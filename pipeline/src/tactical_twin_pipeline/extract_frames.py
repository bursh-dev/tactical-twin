"""Extract frames from a video file using ffmpeg."""

import subprocess
from pathlib import Path

import click
from rich.console import Console

console = Console()

# Look for ffmpeg in deps/bin/ first
DEPS_BIN = Path(__file__).resolve().parent.parent.parent.parent / "deps" / "bin"


def _find_ffmpeg() -> str:
    """Find ffmpeg binary: deps/bin/ first, then system PATH."""
    local = DEPS_BIN / "ffmpeg.exe"
    if local.exists():
        return str(local)
    return "ffmpeg"


def extract_frames(video_path: Path, output_dir: Path, fps: int = 3, quality: int = 1) -> int:
    """Extract frames from video at given fps.

    Args:
        video_path: Path to input video file.
        output_dir: Directory to write JPEG frames.
        fps: Frames per second to extract.
        quality: JPEG quality (1=best, 31=worst).

    Returns:
        Number of frames extracted.
    """
    output_dir.mkdir(parents=True, exist_ok=True)
    pattern = str(output_dir / "%05d.jpg")

    cmd = [
        _find_ffmpeg(), "-i", str(video_path),
        "-qscale:v", str(quality),
        "-r", str(fps),
        pattern,
        "-y",
    ]

    console.print(f"[bold]Extracting frames at {fps} fps...[/bold]")
    console.print(f"  Input:  {video_path}")
    console.print(f"  Output: {output_dir}/")

    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        console.print(f"[red]ffmpeg error:[/red]\n{result.stderr}")
        raise RuntimeError("Frame extraction failed")

    frame_count = len(list(output_dir.glob("*.jpg")))
    console.print(f"[green]Extracted {frame_count} frames[/green]")
    return frame_count


@click.command()
@click.argument("video", type=click.Path(exists=True, path_type=Path))
@click.option("--output", "-o", type=click.Path(path_type=Path), default=None,
              help="Output directory for frames. Defaults to ./frames/<video_name>/")
@click.option("--fps", default=3, help="Frames per second to extract (default: 3)")
def main(video: Path, output: Path | None, fps: int):
    """Extract frames from VIDEO file for 3DGS reconstruction."""
    if output is None:
        output = Path("frames") / video.stem
    extract_frames(video, output, fps=fps)


if __name__ == "__main__":
    main()
