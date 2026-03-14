"""End-to-end pipeline: video → Gaussian splat .ply file.

Uses: ffmpeg → COLMAP → Nerfstudio splatfacto → .ply export
"""

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


@click.command()
@click.argument("video", type=click.Path(exists=True, path_type=Path))
@click.option("--output", "-o", type=click.Path(path_type=Path), default=None,
              help="Final output directory for .ply file. Defaults to ../assets/splats/<video_name>/")
@click.option("--fps", default=2, help="Frame extraction rate (default: 2)")
@click.option("--iterations", "-n", default=15000, help="Training iterations (default: 15000)")
@click.option("--downscale", "-d", default=2, help="Downscale images for training (default: 2)")
@click.option("--work-dir", type=click.Path(path_type=Path), default=Path("work"),
              help="Working directory for intermediate files (default: ./work/)")
def main(video: Path, output: Path | None, fps: int, iterations: int, downscale: int, work_dir: Path):
    """Run full pipeline: VIDEO → 3D Gaussian Splat (.ply).

    Extracts frames, runs COLMAP for camera poses, trains Gaussians with
    Nerfstudio splatfacto, and exports the result as a .ply file ready for Unity.
    """
    scene_name = video.stem
    if output is None:
        output = Path("..") / "assets" / "splats" / scene_name

    frames_dir = work_dir / scene_name / "frames"
    colmap_dir = work_dir / scene_name / "colmap"
    training_dir = work_dir / scene_name / "trained"
    export_dir = work_dir / scene_name / "export"

    console.print(Panel(
        f"[bold]Tactical Twin Pipeline[/bold]\n\n"
        f"Video:      {video}\n"
        f"Scene:      {scene_name}\n"
        f"FPS:        {fps}\n"
        f"Iterations: {iterations}\n"
        f"Downscale:  {downscale}x\n"
        f"Output:     {output}",
        title="Configuration",
    ))

    # Step 1: Extract frames
    console.rule("[bold blue]Step 1/4: Extract Frames")
    frame_count = extract_frames(video, frames_dir, fps=fps)
    if frame_count < 20:
        console.print("[yellow]Warning: fewer than 20 frames — consider a longer video or higher fps[/yellow]")

    # Step 2: COLMAP camera poses
    console.rule("[bold blue]Step 2/4: Camera Pose Estimation (COLMAP)")
    run_colmap(frames_dir, colmap_dir)

    # Step 3: Train Gaussian splat with Nerfstudio
    console.rule("[bold blue]Step 3/4: Train Gaussian Splat (splatfacto)")
    config_path = train_splat(colmap_dir, training_dir, max_iterations=iterations, downscale=downscale)

    # Step 4: Export .ply
    console.rule("[bold blue]Step 4/4: Export")
    ply_path = export_splat(config_path, export_dir)

    # Copy to final output
    output.mkdir(parents=True, exist_ok=True)
    final_ply = output / f"{scene_name}.ply"
    shutil.copy2(ply_path, final_ply)

    size_mb = final_ply.stat().st_size / (1024 * 1024)
    console.print()
    console.print(Panel(
        f"[bold green]Pipeline complete![/bold green]\n\n"
        f"Splat file: {final_ply}\n"
        f"Size:       {size_mb:.1f} MB\n\n"
        f"Next: import {final_ply} into Unity via the Aras-P plugin.",
        title="Done",
    ))


if __name__ == "__main__":
    main()
