# Tactical Twin

Site-specific training simulation platform. Captures real indoor environments via phone video, reconstructs them as photorealistic 3D Gaussian splats, and renders them in Unity for FPS navigation and target shooting. Future VR support via OpenXR.

## Sub-Projects

- `pipeline/` — Python 3.11 (uv-managed). Video → 3D Gaussian splat (.ply). Uses COLMAP + INRIA 3DGS.
- `unity-viewer/` — Unity (C#). Loads .ply splats via Aras-P plugin. FPS controls, shooting, HUD.

## Dependencies (in deps/)

- `deps/bin/colmap.exe` — COLMAP 3.13.0 with CUDA (pre-built Windows binary)
- `deps/gaussian-splatting/` — INRIA 3DGS repo (clone manually, see setup)

## Tech Stack

- **Python env**: Always use `uv`, never raw pip. Venv lives in `pipeline/.venv`.
- **Reconstruction**: COLMAP (camera poses) → INRIA 3DGS train.py (Gaussian splat training)
- **Unity**: 2022.3 LTS or 6000.x, URP template, Aras-P UnityGaussianSplatting plugin.
- **GPU**: NVIDIA RTX A4500, 16GB VRAM, CUDA 12.8. All training runs locally.
- **OS**: Windows 11. Don't use chmod or Unix-only commands.

## Setup

```bash
# 1. Clone 3DGS repo into deps/
git clone https://github.com/graphdeco-inria/gaussian-splatting.git deps/gaussian-splatting

# 2. Install 3DGS Python dependencies (from their repo)
cd deps/gaussian-splatting && pip install -r requirements.txt

# 3. Pipeline deps
cd pipeline && uv sync
```

## Common Commands

```bash
# Full pipeline: video → splat (from pipeline/ directory)
uv run tt-pipeline path/to/video.mp4

# Individual steps
uv run tt-extract path/to/video.mp4 --fps 2
uv run python -m tactical_twin_pipeline.process_data ./frames/my-room
uv run python -m tactical_twin_pipeline.train_splat ./colmap/my-room
uv run python -m tactical_twin_pipeline.export_splat ./trained/my-room
```

## Conventions

- Splat .ply files go in `assets/splats/` (gitignored)
- Source videos go in `assets/videos/` (gitignored)
- COLMAP + 3DGS binaries/repos go in `deps/` (gitignored)
- Pipeline working files go in `pipeline/work/` (gitignored)
- Target configs are JSON in `unity-viewer/Assets/Configs/`
- Pipeline scripts use `click` for CLI, `rich` for progress output
- Unity scripts go in `unity-viewer/Assets/Scripts/`

## Do Not

- Commit .ply, video, or deps/ files to git (large binaries)
- Use pip directly — always uv (for pipeline)
- Install Python packages globally — use the pipeline venv
- Use chmod or other Unix-only commands (this is Windows)
- Use WSL unless native Windows CUDA setup fails
