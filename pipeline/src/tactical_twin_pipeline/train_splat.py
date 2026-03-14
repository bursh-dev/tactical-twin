"""Train a 3D Gaussian Splat using Nerfstudio's splatfacto on COLMAP output."""

import subprocess
import sys
from pathlib import Path

import click
from rich.console import Console

console = Console()


def train_splat(
    colmap_dir: Path,
    output_dir: Path,
    max_iterations: int = 15000,
    downscale: int = 2,
) -> Path:
    """Train Gaussian splats using ns-train splatfacto.

    Args:
        colmap_dir: COLMAP project dir (with sparse/ and images/).
        output_dir: Where to save the trained model.
        max_iterations: Training iterations.
        downscale: Downscale images by this factor for faster training.

    Returns:
        Path to the Nerfstudio output config (used for export).
    """
    console.print(f"[bold]Training splatfacto on {colmap_dir}[/bold]")
    console.print(f"  Iterations: {max_iterations}, Downscale: {downscale}x")

    cmd = [
        sys.executable, "-m", "nerfstudio.scripts.train",
        "splatfacto",
        "--data", str(colmap_dir),
        "--max-num-iterations", str(max_iterations),
        "--output-dir", str(output_dir),
        "--viewer.quit-on-train-completion", "True",
        "colmap",
        "--colmap-path", "sparse/0",
    ]

    if downscale > 1:
        # Insert before the "colmap" dataparser arg
        colmap_idx = cmd.index("colmap")
        cmd.insert(colmap_idx, str(downscale))
        cmd.insert(colmap_idx, "--downscale-factor")

    console.print(f"  Running: ns-train splatfacto ...")

    result = subprocess.run(cmd, text=True)
    if result.returncode != 0:
        raise RuntimeError("ns-train splatfacto failed")

    # Find the config.yml in the output — Nerfstudio saves to
    # <output_dir>/splatfacto/<timestamp>/config.yml
    config_files = sorted(output_dir.rglob("config.yml"), key=lambda p: p.stat().st_mtime)
    if not config_files:
        raise RuntimeError(f"No config.yml found in {output_dir}")

    config_path = config_files[-1]  # most recent
    console.print(f"[green]Training complete! Config: {config_path}[/green]")
    return config_path


@click.command()
@click.argument("colmap_dir", type=click.Path(exists=True, path_type=Path))
@click.option("--output", "-o", type=click.Path(path_type=Path), default=None,
              help="Output directory for trained model.")
@click.option("--iterations", "-n", default=15000, help="Max training iterations (default: 15000)")
@click.option("--downscale", "-d", default=2, help="Downscale images by factor (default: 2)")
def main(colmap_dir: Path, output: Path | None, iterations: int, downscale: int):
    """Train 3DGS on COLMAP_DIR (must contain sparse/ and images/)."""
    if output is None:
        output = Path("trained") / colmap_dir.name
    train_splat(colmap_dir, output, max_iterations=iterations, downscale=downscale)


if __name__ == "__main__":
    main()
