"""Run COLMAP to estimate camera poses from extracted frames.

Compatible with COLMAP 3.13+ (uses --FeatureExtraction/--FeatureMatching flags).
Uses sequential matching (better for video walkthroughs than exhaustive).
Enhanced feature extraction settings for reliable reconstruction.
Automatically selects the best reconstruction when COLMAP produces multiple.
"""

import subprocess
import shutil
from pathlib import Path

import click
from rich.console import Console

console = Console()

# Default COLMAP binary location (downloaded to deps/)
DEFAULT_COLMAP = Path(__file__).resolve().parent.parent.parent.parent / "deps" / "bin" / "colmap.exe"


def run_colmap(images_dir: Path, project_dir: Path, colmap_bin: Path | None = None) -> Path:
    """Run COLMAP sparse reconstruction on a folder of images.

    Produces the sparse/0/ directory with cameras.bin, images.bin, points3D.bin
    that Nerfstudio splatfacto expects.

    Uses COLMAP 3.13 flag names (--FeatureExtraction, --FeatureMatching)
    and sequential matching optimized for video frame sequences.

    Args:
        images_dir: Directory containing JPEG frames.
        project_dir: Project directory for COLMAP outputs.
        colmap_bin: Path to colmap.exe. Defaults to deps/bin/colmap.exe.

    Returns:
        Path to the project directory (containing sparse/0/ and images/).
    """
    if colmap_bin is None:
        colmap_bin = DEFAULT_COLMAP
    if not colmap_bin.exists():
        # Fall back to system PATH
        colmap_bin = Path("colmap")

    colmap = str(colmap_bin)

    project_dir.mkdir(parents=True, exist_ok=True)
    db_path = project_dir / "database.db"
    sparse_dir = project_dir / "sparse"
    sparse_dir.mkdir(exist_ok=True)

    # Copy images into project dir if not already there
    project_images = project_dir / "images"
    if not project_images.exists():
        shutil.copytree(images_dir, project_images)

    # Step 1: Feature extraction (COLMAP 3.13 flags)
    # Enhanced settings: 8192 max features, affine shape, domain size pooling
    console.print("[bold]Step 1/3: Extracting features...[/bold]")
    _run_colmap_cmd([
        colmap, "feature_extractor",
        "--database_path", str(db_path),
        "--image_path", str(project_images),
        "--ImageReader.single_camera", "1",
        "--ImageReader.camera_model", "OPENCV",
        "--FeatureExtraction.use_gpu", "1",
        "--SiftExtraction.max_num_features", "8192",
        "--SiftExtraction.estimate_affine_shape", "1",
        "--SiftExtraction.domain_size_pooling", "1",
    ])

    # Step 2: Sequential matching (better for video walkthroughs than exhaustive)
    # overlap=15 matches each frame against 15 neighbors
    console.print("[bold]Step 2/3: Sequential matching...[/bold]")
    _run_colmap_cmd([
        colmap, "sequential_matcher",
        "--database_path", str(db_path),
        "--FeatureMatching.use_gpu", "1",
        "--SequentialMatching.overlap", "15",
    ])

    # Step 3: Sparse reconstruction (mapper)
    console.print("[bold]Step 3/3: Sparse reconstruction...[/bold]")
    _run_colmap_cmd([
        colmap, "mapper",
        "--database_path", str(db_path),
        "--image_path", str(project_images),
        "--output_path", str(sparse_dir),
        "--Mapper.ba_global_max_num_iterations", "50",
        "--Mapper.ba_global_max_refinements", "3",
    ])

    # COLMAP may produce multiple reconstructions (sparse/0, sparse/1, ...).
    # Pick the one with the most registered images (largest images.bin).
    _select_best_reconstruction(sparse_dir)

    # Verify output
    sparse_0 = sparse_dir / "0"
    if sparse_0.exists() and (sparse_0 / "cameras.bin").exists():
        console.print(f"[green]COLMAP reconstruction complete: {sparse_0}[/green]")
    else:
        console.print("[red]COLMAP reconstruction failed — no sparse/0/ output[/red]")
        raise RuntimeError("COLMAP mapper produced no output")

    return project_dir


def _select_best_reconstruction(sparse_dir: Path):
    """If COLMAP produced multiple reconstructions, move the best one to sparse/0/.

    The best reconstruction is the one with the largest images.bin
    (more registered images = better).
    """
    recon_dirs = sorted(
        [d for d in sparse_dir.iterdir() if d.is_dir() and d.name.isdigit()],
        key=lambda d: int(d.name),
    )

    if len(recon_dirs) <= 1:
        return

    # Find the reconstruction with the largest images.bin
    best = max(recon_dirs, key=lambda d: (d / "images.bin").stat().st_size if (d / "images.bin").exists() else 0)

    if best.name == "0":
        console.print(f"[dim]Best reconstruction is already sparse/0[/dim]")
        return

    console.print(f"[yellow]Multiple reconstructions found. Best: sparse/{best.name} "
                  f"(images.bin: {(best / 'images.bin').stat().st_size / 1024:.0f} KB). "
                  f"Moving to sparse/0.[/yellow]")

    # Swap: rename 0 → 0_old, best → 0
    old_0 = sparse_dir / "0_replaced"
    if (sparse_dir / "0").exists():
        (sparse_dir / "0").rename(old_0)
    best.rename(sparse_dir / "0")


def _run_colmap_cmd(cmd: list[str]):
    """Run a COLMAP command, stream output, and handle errors."""
    console.print(f"[dim]  {' '.join(cmd[:3])}...[/dim]")
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        console.print(f"[red]COLMAP error:[/red]\n{result.stderr[-2000:]}")
        raise RuntimeError(f"COLMAP command failed: {' '.join(cmd[:2])}")


@click.command()
@click.argument("images_dir", type=click.Path(exists=True, path_type=Path))
@click.option("--output", "-o", type=click.Path(path_type=Path), default=None,
              help="Project directory for COLMAP output. Defaults to ./colmap/<dir_name>/")
def main(images_dir: Path, output: Path | None):
    """Run COLMAP on IMAGES_DIR to estimate camera poses."""
    if output is None:
        output = Path("colmap") / images_dir.name
    run_colmap(images_dir, output)


if __name__ == "__main__":
    main()
