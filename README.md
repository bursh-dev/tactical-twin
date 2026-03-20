# Tactical Twin

**Site-specific tactical training simulation.** Capture any real indoor environment with a phone camera, reconstruct it as a photorealistic 3D scene, and train inside it — navigating rooms, engaging targets, and building spatial awareness before ever stepping on site.

<p align="center">
  <img src="https://img.shields.io/badge/Python-3.11-blue" alt="Python">
  <img src="https://img.shields.io/badge/Unity-6.3_LTS-black" alt="Unity">
  <img src="https://img.shields.io/badge/CUDA-12.4-green" alt="CUDA">
  <img src="https://img.shields.io/badge/GPU-RTX_A4500-76b900" alt="GPU">
  <img src="https://img.shields.io/badge/Platform-Windows_11-0078d4" alt="Platform">
</p>

---

## How It Works

```
Phone Video (2 min walk-through)
        |
        v
  +-----------+     +-----------+     +-------------+     +------------+
  |  Extract   | --> |  COLMAP   | --> |  Splatfacto | --> |   Export   |
  |  Frames    |     |  Poses    |     |  Training   |     |   .ply     |
  |  (ffmpeg)  |     |  (3D AI)  |     |  (30K iter) |     |  + mesh   |
  +-----------+     +-----------+     +-------------+     +------------+
     ~1 min            ~3 min            ~20 min              ~1 min
        |
        +---> Fully automated, one command, ~25 min total
                                                               |
                                                               v
                                                    +-------------------+
                                                    |   Unity Viewer    |
                                                    |  FPS navigation   |
                                                    |  Target shooting  |
                                                    |  Room collision   |
                                                    +-------------------+
```

## What You Get

- **Photorealistic 3D environment** — 300K+ Gaussian splats from a 2-minute phone video
- **First-person navigation** — walk through the reconstructed space at realistic speed
- **Room collision** — auto-generated walls from point cloud analysis, or walk-to-mark calibration
- **Target shooting** — ring targets (1-10 scoring), speed multipliers, event-based rounds
- **Real weapon overlay** — Glock 19 photo + authentic gunshot audio
- **One-command pipeline** — video in, training simulation out

---

## Quick Start

### Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| Python | 3.11 | Managed via [uv](https://github.com/astral-sh/uv) |
| CUDA | 12.4 | NVIDIA GPU Computing Toolkit |
| COLMAP | 3.13 | Pre-built with CUDA, in `deps/bin/` |
| ffmpeg | 8.0+ | In `deps/bin/` |
| Unity | 6.3 LTS (6000.3.11f1) | URP template, DX12 |
| MSVC | 14.44+ | For gsplat JIT compilation |
| GPU | NVIDIA with 8GB+ VRAM | Tested on RTX A4500 (16GB) |

### 1. Setup Pipeline

```bash
cd pipeline/
uv sync
```

### 2. Record Your Space

Film a 2-4 minute walk-through with your phone:
- **Landscape orientation**, lights on
- Walk slowly — every surface visible from 3+ angles
- Avoid mirrors, glass, and shiny surfaces

### 3. Run the Pipeline

```bash
cd pipeline/
uv run tt-pipeline ../assets/videos/my-room.mp4 --scene my-room
```

That's it. The pipeline:
1. Extracts frames (3 fps, best JPEG quality)
2. Runs COLMAP (16K features, sequential matching)
3. Trains splatfacto (30K iterations, ~20 min on RTX A4500)
4. Exports `.ply` splat file
5. Generates collision mesh from point cloud
6. Copies everything to `unity-viewer/Assets/Splats/`

### 4. Load in Unity

1. Open `unity-viewer/` project in Unity 6
2. **Tools > Gaussian Splats > Create GaussianSplatAsset** — select the `.ply`
3. Assign to a GaussianSplatRenderer, set **Rotation X = -90**
4. Hit Play — walk around your space and shoot targets

---

## Project Structure

```
tactical-twin/
├── pipeline/                          # Python reconstruction pipeline
│   └── src/tactical_twin_pipeline/
│       ├── pipeline.py                # One-command orchestrator
│       ├── extract_frames.py          # Video → JPEG frames (ffmpeg)
│       ├── process_data.py            # COLMAP 3.13 wrapper
│       ├── train_splat.py             # Nerfstudio splatfacto training
│       ├── export_splat.py            # Model → .ply export
│       └── extract_collision.py       # Point cloud → collision mesh
│
├── unity-viewer/                      # Unity 6 real-time viewer
│   └── Assets/Scripts/
│       ├── FPSController.cs           # WASD + mouse look, wall collision
│       ├── ShootingSystem.cs          # Raycast shooting, ring scoring
│       ├── TargetManager.cs           # Event-based round system
│       ├── RoomCalibrator.cs          # Auto-mesh + walk-to-mark walls
│       ├── HUDManager.cs              # Crosshair, score, grade (S–F)
│       ├── GunModel.cs                # Real gun photo overlay
│       └── Target.cs                  # 10-ring concentric target
│
├── design/
│   ├── design.md                      # Architecture & milestones
│   └── spec.md                        # Tool paths, commands, gotchas
│
├── deps/bin/                          # COLMAP, ffmpeg (gitignored)
├── assets/videos/                     # Source recordings (gitignored)
└── assets/splats/                     # Exported .ply files (gitignored)
```

## Pipeline CLI

```bash
# Full pipeline (recommended)
uv run tt-pipeline ../assets/videos/room.mp4 --scene room-name

# Individual steps
uv run tt-extract ../assets/videos/room.mp4 -o work/room/frames --fps 3
uv run tt-colmap work/room/frames -o work/room/colmap
uv run tt-train work/room/colmap --iterations 30000
uv run tt-export work/room/train -o work/room/export
uv run tt-collision work/room/export/splat.ply
```

| Flag | Default | Description |
|------|---------|-------------|
| `--scene` | from filename | Scene name for output directories |
| `--fps` | 3 | Frame extraction rate |
| `--iterations` | 30000 | Splatfacto training iterations |
| `--downscale` | 2 | Image downscale factor |
| `--copy-to-unity` | yes | Auto-copy .ply to Unity project |

---

## Room Collision

Two modes for defining room boundaries:

### Auto-Mesh (default)
The pipeline analyzes the splat point cloud and generates collision planes automatically. Assign the splat's Transform to RoomCalibrator's **Splat Transform** field — colliders load on Play.

### Walk-to-Mark (manual)
Press **C** in-game to enter calibration mode. Walk to each wall and press **F** — a collision plane is placed perpendicular to your facing direction. Press **Esc** to save. Works for any room shape.

---

## Gameplay

| Control | Action |
|---------|--------|
| WASD | Move |
| Mouse | Look |
| Shift | Sprint |
| Left Click | Shoot |
| Scroll | Adjust placement distance (calibration) |
| C | Enter/exit calibration |
| F | Mark wall (calibration) |
| Esc | Exit calibration / unlock cursor |

**Scoring:** Ring targets score 1 (edge) to 10 (bullseye). Speed multiplier rewards fast reactions (2.0x instant → 1.0x at timeout). Rounds consist of surprise events — 1-3 targets appear, followed by "NAVIGATE" pauses. Final grade: S / A / B / C / D / F.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Capture | Phone camera (any modern smartphone) |
| Frame extraction | ffmpeg 8.0 |
| Camera poses | COLMAP 3.13 + CUDA |
| 3D reconstruction | Nerfstudio splatfacto (Gaussian splatting) |
| Collision mesh | Open3D RANSAC plane detection |
| Real-time rendering | Unity 6.3 LTS, URP, DX12 |
| Splat renderer | [Aras-P UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) |
| GPU compute | PyTorch 2.4.1+cu124, NVIDIA RTX |
| Package management | uv (Python), Unity Package Manager (C#) |

---

## Milestones

- [x] **M1** — See the room: video → splat → Unity rendering
- [x] **M2** — Walk through: FPS controller, room collision
- [x] **M3** — Shoot targets: scoring, events, HUD, gun overlay
- [ ] **M4** — VR support: OpenXR, headset tracking, hand controllers

---

## Important Notes

- **Never use pip** — always `uv pip install` or `uv sync`
- **Never use `ns-process-data`** — broken with COLMAP 3.13, use `process_data.py`
- **MSVC must be on PATH** before splatfacto training (gsplat JIT needs `cl.exe`)
- **Sequential matching only** — exhaustive matcher is too slow for video input
- Large files (`.ply`, videos, `deps/`) are gitignored — not committed

---

## License

Private project. All rights reserved.
