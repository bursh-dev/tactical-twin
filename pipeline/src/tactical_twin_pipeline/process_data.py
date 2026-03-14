"""Run COLMAP to estimate camera poses from extracted frames."""

import subprocess
from pathlib import Path

import click
from rich.console import Console

console = Console()

# Default COLMAP binary location (downloaded to deps/)
DEFAULT_COLMAP = Path(__file__).resolve().parent.parent.parent.parent / "deps" / "bin" / "colmap.exe"


def run_colmap(images_dir: Path, project_dir: Path, colmap_bin: Path | None = None) -> Path:
    """Run COLMAP sparse reconstruction on a folder of images.

    Produces the sparse/ directory with cameras.bin, images.bin, points3D.bin
    that the INRIA 3DGS training script expects.

    Args:
        images_dir: Directory containing JPEG frames.
        project_dir: Project directory for COLMAP outputs.

    Returns:
        Path to the project directory (containing sparse/ and images/).
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
        import shutil
        shutil.copytree(images_dir, project_images)

    # Step 1: Feature extraction
    console.print("[bold]Step 1/3: Extracting features...[/bold]")
    _run_colmap_cmd([
        colmap, "feature_extractor",
        "--database_path", str(db_path),
        "--image_path", str(project_images),
        "--ImageReader.single_camera", "1",
    ])

    # Step 2: Feature matching
    console.print("[bold]Step 2/3: Matching features...[/bold]")
    _run_colmap_cmd([
        colmap, "exhaustive_matcher",
        "--database_path", str(db_path),
    ])

    # Step 3: Sparse reconstruction (mapper)
    console.print("[bold]Step 3/3: Sparse reconstruction...[/bold]")
    _run_colmap_cmd([
        colmap, "mapper",
        "--database_path", str(db_path),
        "--image_path", str(project_images),
        "--output_path", str(sparse_dir),
    ])

    # Verify output
    sparse_0 = sparse_dir / "0"
    if sparse_0.exists() and (sparse_0 / "cameras.bin").exists():
        console.print(f"[green]COLMAP reconstruction complete: {sparse_0}[/green]")
    else:
        console.print("[red]COLMAP reconstruction failed — no sparse/0/ output[/red]")
        raise RuntimeError("COLMAP mapper produced no output")

    return project_dir


def _run_colmap_cmd(cmd: list[str]):
    """Run a COLMAP command and handle errors."""
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        console.print(f"[red]COLMAP error:[/red]\n{result.stderr}")
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
