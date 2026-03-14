"""Export trained Nerfstudio model to .ply file."""

import subprocess
import sys
from pathlib import Path

import click
from rich.console import Console

console = Console()


def export_splat(config_path: Path, output_dir: Path) -> Path:
    """Export a trained splatfacto model to .ply using ns-export.

    Args:
        config_path: Path to the Nerfstudio config.yml from training.
        output_dir: Where to write the exported .ply file.

    Returns:
        Path to the exported .ply file.
    """
    output_dir.mkdir(parents=True, exist_ok=True)

    cmd = [
        sys.executable, "-m", "nerfstudio.scripts.exporter",
        "gaussian-splat",
        "--load-config", str(config_path),
        "--output-dir", str(output_dir),
    ]

    console.print(f"[bold]Exporting splat from {config_path}[/bold]")
    result = subprocess.run(cmd, text=True)
    if result.returncode != 0:
        raise RuntimeError("ns-export gaussian-splat failed")

    # Find the .ply file in output
    ply_files = sorted(output_dir.rglob("*.ply"), key=lambda p: p.stat().st_mtime)
    if not ply_files:
        raise RuntimeError(f"No .ply file found in {output_dir}")

    ply_path = ply_files[-1]
    size_mb = ply_path.stat().st_size / (1024 * 1024)
    console.print(f"[green]Exported: {ply_path} ({size_mb:.1f} MB)[/green]")
    return ply_path


@click.command()
@click.argument("config_path", type=click.Path(exists=True, path_type=Path))
@click.option("--output", "-o", type=click.Path(path_type=Path), default=Path("export"),
              help="Output directory for .ply file (default: ./export/)")
def main(config_path: Path, output: Path):
    """Export trained model from CONFIG_PATH to .ply."""
    export_splat(config_path, output)


if __name__ == "__main__":
    main()
