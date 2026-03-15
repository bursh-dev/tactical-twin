"""End-to-end pipeline: video → Gaussian splat .ply file.

Uses: ffmpeg → COLMAP → Nerfstudio splatfacto → .ply export

Automatically sets up CUDA/MSVC/tool environment before running.
Run from the pipeline/ directory:
    uv run tt-pipeline ../assets/videos/VIDEO.mp4
"""

import os
import shutil
from pathlib import Path

import click
from rich.console import Console
from rich.panel import Panel

from tactical_twin_pipeline.extract_frames import extract_frames
from tactical_twin_pipeline.process_data import run_colmap
from tactical_twin_pipeline.train_splat import train_splat
from tactical_twin_pipeline.export_splat import export_splat

console = Console()

# Absolute paths from spec.md — baked in so the pipeline is fully autonomous
PROJECT_DIR = Path(__file__).resolve().parent.parent.parent.parent  # tactical-twin/
DEPS_BIN = PROJECT_DIR / "deps" / "bin"
CUDA_HOME = Path(r"C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.4")
MSVC_BIN = Path(r"C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.44.35207\bin\Hostx64\x64")
UNITY_SPLATS = PROJECT_DIR / "unity-viewer" / "Assets" / "Splats"


def setup_environment():
    """Add COLMAP, MSVC, CUDA to PATH so all subprocesses find them."""
    paths_to_add = []

    # Venv Scripts (for ninja, etc.)
    venv_scripts = PROJECT_DIR / "pipeline" / ".venv" / "Scripts"
    if venv_scripts.exists():
        paths_to_add.append(str(venv_scripts))
    if DEPS_BIN.exists():
        paths_to_add.append(str(DEPS_BIN))
    if MSVC_BIN.exists():
        paths_to_add.append(str(MSVC_BIN))
    if (CUDA_HOME / "bin").exists():
        paths_to_add.append(str(CUDA_HOME / "bin"))

    current_path = os.environ.get("PATH", "")
    for p in paths_to_add:
        if p not in current_path:
            current_path = p + os.pathsep + current_path

    os.environ["PATH"] = current_path
    os.environ["CUDA_HOME"] = str(CUDA_HOME)
    os.environ["PYTHONIOENCODING"] = "utf-8"
    os.environ["PYTHONUTF8"] = "1"


def verify_tools():
    """Check that all required tools are accessible."""
    issues = []

    colmap = DEPS_BIN / "colmap.exe"
    if not colmap.exists():
        issues.append(f"COLMAP not found: {colmap}")

    ffmpeg = DEPS_BIN / "ffmpeg.exe"
    if not ffmpeg.exists():
        issues.append(f"ffmpeg not found: {ffmpeg}")

    cl_exe = MSVC_BIN / "cl.exe"
    if not cl_exe.exists():
        issues.append(f"MSVC cl.exe not found: {cl_exe}")

    if not (CUDA_HOME / "bin" / "nvcc.exe").exists():
        issues.append(f"CUDA nvcc not found in: {CUDA_HOME}")

    if issues:
        for issue in issues:
            console.print(f"[red]  {issue}[/red]")
        raise RuntimeError("Missing required tools — see above")

    console.print("[green]All tools verified[/green]")


@click.command()
@click.argument("video", type=click.Path(exists=True, path_type=Path))
@click.option("--scene", "-s", default=None, help="Scene name (default: video filename)")
@click.option("--output", "-o", type=click.Path(path_type=Path), default=None,
              help="Final output directory for .ply file. Defaults to ../assets/splats/<scene>/")
@click.option("--fps", default=3, help="Frame extraction rate (default: 3)")
@click.option("--iterations", "-n", default=30000, help="Training iterations (default: 30000)")
@click.option("--downscale", "-d", default=2, help="Downscale images for training (default: 2)")
@click.option("--work-dir", type=click.Path(path_type=Path), default=Path("work"),
              help="Working directory for intermediate files (default: ./work/)")
@click.option("--copy-to-unity/--no-copy-to-unity", default=True,
              help="Copy final .ply to Unity Assets/Splats/ (default: yes)")
def main(video: Path, scene: str | None, output: Path | None, fps: int,
         iterations: int, downscale: int, work_dir: Path, copy_to_unity: bool):
    """Run full pipeline: VIDEO → 3D Gaussian Splat (.ply).

    Extracts frames, runs COLMAP for camera poses, trains Gaussians with
    Nerfstudio splatfacto, and exports the result as a .ply file ready for Unity.

    Automatically sets up CUDA/MSVC/tool environment.
    """
    scene_name = scene or video.stem
    # Sanitize: remove hyphens (Unity doesn't like them in asset names)
    scene_name = scene_name.replace("-", "")

    if output is None:
        output = Path("..") / "assets" / "splats" / scene_name

    frames_dir = work_dir / scene_name / "frames"
    colmap_dir = work_dir / scene_name / "colmap"
    training_dir = work_dir / scene_name / "trained"
    export_dir = work_dir / scene_name / "export"

    console.print(Panel(
        f"[bold]Tactical Twin Pipeline[/bold]\n\n"
        f"Video:       {video}\n"
        f"Scene:       {scene_name}\n"
        f"FPS:         {fps}\n"
        f"Iterations:  {iterations}\n"
        f"Downscale:   {downscale}x\n"
        f"JPEG quality: 1 (best)\n"
        f"COLMAP features: 16384\n"
        f"COLMAP overlap:  20\n"
        f"Output:      {output}\n"
        f"Unity copy:  {copy_to_unity}",
        title="Configuration",
    ))

    # Step 0: Environment setup
    console.rule("[bold blue]Step 0/5: Environment Setup")
    setup_environment()
    verify_tools()

    # Step 1: Extract frames
    console.rule("[bold blue]Step 1/5: Extract Frames")
    if frames_dir.exists() and len(list(frames_dir.glob("*.jpg"))) > 0:
        frame_count = len(list(frames_dir.glob("*.jpg")))
        console.print(f"[yellow]Frames already extracted ({frame_count}), skipping. Delete {frames_dir} to re-extract.[/yellow]")
    else:
        frame_count = extract_frames(video, frames_dir, fps=fps)
    if frame_count < 20:
        console.print("[yellow]Warning: fewer than 20 frames — consider a longer video or higher fps[/yellow]")

    # Step 2: COLMAP camera poses
    console.rule("[bold blue]Step 2/5: Camera Pose Estimation (COLMAP)")
    sparse_check = colmap_dir / "sparse" / "0" / "cameras.bin"
    if sparse_check.exists():
        console.print(f"[yellow]COLMAP already done, skipping. Delete {colmap_dir} to re-run.[/yellow]")
    else:
        run_colmap(frames_dir, colmap_dir)

    # Step 3: Train Gaussian splat with Nerfstudio
    console.rule("[bold blue]Step 3/5: Train Gaussian Splat (splatfacto)")
    config_path = train_splat(colmap_dir, training_dir, max_iterations=iterations, downscale=downscale)

    # Step 4: Export .ply
    console.rule("[bold blue]Step 4/5: Export")
    ply_path = export_splat(config_path, export_dir)

    # Step 5: Copy to final locations
    console.rule("[bold blue]Step 5/5: Deploy")
    output.mkdir(parents=True, exist_ok=True)
    final_ply = output / f"{scene_name}.ply"
    shutil.copy2(ply_path, final_ply)

    unity_ply = None
    if copy_to_unity:
        UNITY_SPLATS.mkdir(parents=True, exist_ok=True)
        unity_ply = UNITY_SPLATS / f"{scene_name}.ply"
        shutil.copy2(ply_path, unity_ply)
        console.print(f"[green]Copied to Unity: {unity_ply}[/green]")

    size_mb = final_ply.stat().st_size / (1024 * 1024)
    console.print()

    next_steps = (
        f"1. Open Unity project: unity-viewer/\n"
        f"2. Tools > Gaussian Splats > Create GaussianSplatAsset\n"
        f"3. Browse to: {unity_ply or final_ply}\n"
        f"4. Set Quality to Medium, click Create Asset\n"
        f"5. Create empty GameObject, add Gaussian Splat Renderer\n"
        f"6. Drag the asset into Splat Asset field\n"
        f"7. Set Transform Rotation X = -90"
    )

    console.print(Panel(
        f"[bold green]Pipeline complete![/bold green]\n\n"
        f"Splat file: {final_ply}\n"
        f"Size:       {size_mb:.1f} MB\n\n"
        f"[bold]Next steps (manual in Unity):[/bold]\n{next_steps}",
        title="Done",
    ))


if __name__ == "__main__":
    main()
