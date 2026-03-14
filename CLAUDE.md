# Tactical Twin

Site-specific training simulation platform. Captures real indoor environments via phone video, reconstructs them as photorealistic 3D Gaussian splats, and renders them in Unity for FPS navigation and target shooting. Future VR support via OpenXR.

## Documentation

- `design/design.md` — Architecture, components, milestones, competitive landscape
- `design/spec.md` — **Load this first** — all tool paths, env setup, pipeline commands, per-room settings, gotchas

## Sub-Projects

- `pipeline/` — Python 3.11 (uv-managed). Video → 3D Gaussian splat (.ply). Uses COLMAP + Nerfstudio splatfacto.
- `unity-viewer/` — Unity 6 (C#). Loads .ply splats via Aras-P plugin. FPS controls, shooting, HUD.

## Dependencies (in deps/)

- `deps/bin/colmap.exe` — COLMAP 3.13.0 with CUDA (pre-built Windows binary)
- `deps/bin/ffmpeg.exe` — ffmpeg 8.0.1
- `deps/bin/ffprobe.exe` — ffprobe 8.0.1

## Tech Stack

- **Python env**: Always use `uv`, never raw pip. Venv lives in `pipeline/.venv`.
- **Reconstruction**: COLMAP 3.13 (camera poses) → Nerfstudio splatfacto (Gaussian splat training)
- **Unity**: 6.3 LTS (6000.3.11f1), URP template, DX12, Aras-P UnityGaussianSplatting plugin.
- **GPU**: NVIDIA RTX A4500, 16GB VRAM, CUDA 12.4. All training runs locally.
- **OS**: Windows 11. Don't use chmod or Unix-only commands.

## Common Commands

```bash
# Full pipeline: video → splat (sets up env automatically)
bash scripts/full-pipeline.sh assets/videos/my-room.mp4 my-room

# Manual steps — see design/spec.md for full details
```

## Critical Rules

- **Never use pip** — always `uv pip install`
- **Never use ns-process-data** — broken with COLMAP 3.13, use `process_data.py` instead
- **Never use exhaustive_matcher** — use `sequential_matcher` for video input
- **MSVC must be on PATH** before splatfacto training (gsplat needs cl.exe)
- Don't commit .ply, video, or deps/ files to git (large binaries)
- Don't use chmod or other Unix-only commands (this is Windows)
- Don't use WSL unless native Windows CUDA setup fails

## Conventions

- Splat .ply files go in `assets/splats/` (gitignored)
- Source videos go in `assets/videos/` (gitignored)
- External tools go in `deps/` (gitignored)
- Pipeline working files go in `pipeline/work/` (gitignored)
- Unity scripts go in `unity-viewer/Assets/Scripts/`
- Runtime-loaded assets go in `unity-viewer/Assets/Resources/`
- Pipeline scripts use `click` for CLI, `rich` for progress output
