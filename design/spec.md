# Tactical Twin — Operational Spec

Quick-reference for all tool paths, environment setup, and operational details.
Load this when working on the project to have everything at hand.

## 1. Tool Paths

### External Binaries (deps/bin/)

| Tool | Absolute Path | Version |
|------|--------------|---------|
| COLMAP | `C:\projects\vs_code\tactical-twin\deps\bin\colmap.exe` | 3.13.0 (CUDA) |
| ffmpeg | `C:\projects\vs_code\tactical-twin\deps\bin\ffmpeg.exe` | 8.0.1 |
| ffprobe | `C:\projects\vs_code\tactical-twin\deps\bin\ffprobe.exe` | 8.0.1 |

### System Tools

| Tool | Absolute Path | Version |
|------|--------------|---------|
| CUDA Toolkit | `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.4` | 12.4 |
| MSVC (cl.exe) | `C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.44.35207\bin\Hostx64\x64\cl.exe` | 14.44.35207 |
| Python | `C:\python\Python311\python.exe` | 3.11 |
| uv | system PATH | 0.9.21 |

### Python Venv

| Item | Path |
|------|------|
| Venv root | `C:\projects\vs_code\tactical-twin\pipeline\.venv\` |
| Python exe | `C:\projects\vs_code\tactical-twin\pipeline\.venv\Scripts\python.exe` |
| pip (via uv) | `uv pip install` (never raw pip) |

### Unity Project

| Item | Path |
|------|------|
| Unity project | `C:\projects\vs_code\tactical-twin\unity-viewer\` |
| Scripts | `unity-viewer\Assets\Scripts\` |
| Resources | `unity-viewer\Assets\Resources\` (runtime-loadable assets) |
| Audio | `unity-viewer\Assets\Audio\` |
| Splats | `unity-viewer\Assets\Splats\` (gitignored) |
| Aras-P plugin | `unity-viewer\Packages\UnityGaussianSplatting\` |

## 2. Pipeline — Fully Automated

The pipeline sets up CUDA/MSVC/tool environment automatically. Just run:

```bash
cd pipeline/
uv run tt-pipeline ../assets/videos/VIDEO.mp4
```

Options:
```bash
uv run tt-pipeline ../assets/videos/VIDEO.mp4 \
  --scene my-room \          # Scene name (default: video filename)
  --fps 3 \                  # Frame extraction rate (default: 3)
  --iterations 30000 \       # Training iterations (default: 30000)
  --downscale 2 \            # Image downscale factor (default: 2)
  --copy-to-unity             # Copies .ply to unity-viewer/Assets/Splats/ (default: yes)
```

The pipeline:
1. Sets up PATH (COLMAP, MSVC cl.exe, CUDA) automatically
2. Verifies all tools exist
3. Extracts frames (3fps, JPEG quality 1)
4. Runs COLMAP (16384 features, overlap 20, sequential matching)
5. Trains splatfacto (30K iterations)
6. Exports .ply
7. Extracts collision mesh via RANSAC plane detection (walls, floor, ceiling)
8. Copies .ply + collision .obj to `assets/splats/` AND `unity-viewer/Assets/Splats/`
9. Prints Unity import instructions

Steps 1-2, 7 are skipped if already completed (delete work dir to re-run).

### Pipeline Defaults (improved)

| Setting | Old | New | Why |
|---------|-----|-----|-----|
| FPS extraction | 2 | 3 | More frames = better COLMAP overlap |
| JPEG quality | 2 | 1 | Best quality, negligible size impact |
| COLMAP features | 8192 | 16384 | Denser point cloud |
| COLMAP overlap | 15 | 20 | More frame pairs matched |
| Training iterations | 15000 | 30000 | Sharper splat details |

### Individual steps (if needed)
```bash
cd pipeline/

# Step 1: Extract frames
uv run tt-extract ../assets/videos/VIDEO.mp4 -o work/SCENE/frames --fps 3

# Step 2: COLMAP
uv run python -m tactical_twin_pipeline.process_data work/SCENE/frames -o work/SCENE/colmap

# Step 3: Train splatfacto
uv run tt-train work/SCENE/colmap -n 30000

# Step 4: Export .ply
uv run tt-export work/SCENE/trained/SCENE/splatfacto/TIMESTAMP/config.yml

# Step 5: Extract collision mesh from .ply
uv run tt-collision work/SCENE/export/splat.ply -o work/SCENE/collision/SCENE_collision.obj
```

### Auto-Collision Mesh

The pipeline automatically extracts collision geometry from the Gaussian splat point cloud using RANSAC plane detection:

- Detects walls (vertical planes), floor, and ceiling (horizontal planes)
- Exports as .obj mesh with thin slabs (0.15m thickness)
- Auto-copied to `unity-viewer/Assets/Splats/<scene>_collision.obj`
- Unity loads it at runtime as a MeshCollider (rotation X=-90 applied automatically)
- Falls back to manual wall calibration (C key) if no collision mesh found
- No external dependencies — pure numpy RANSAC

## 3. COLMAP 3.13 Critical Settings

**NEVER use `ns-process-data`** — it uses old COLMAP flag names that fail with 3.13.

### Flag renames (3.13 breaking changes)
| Old (broken) | New (correct) |
|-------------|--------------|
| `--SiftExtraction.use_gpu` | `--FeatureExtraction.use_gpu` |
| `--SiftMatching.use_gpu` | `--FeatureMatching.use_gpu` |

### Feature extraction
```
--FeatureExtraction.use_gpu 1
--SiftExtraction.max_num_features 16384
--SiftExtraction.estimate_affine_shape 1
--SiftExtraction.domain_size_pooling 1
--ImageReader.single_camera 1
--ImageReader.camera_model OPENCV
```

### Matching
```
sequential_matcher  (NEVER exhaustive for video input)
--FeatureMatching.use_gpu 1
--SequentialMatching.overlap 20
```

### Mapper
```
--Mapper.ba_global_max_num_iterations 50
--Mapper.ba_global_max_refinements 3
```

### Multiple reconstructions
COLMAP may produce sparse/0, sparse/1, etc. The `process_data.py` auto-selects the best one
(largest images.bin = most registered images) and moves it to sparse/0.

## 4. Unity Import Steps (manual — after pipeline)

### Import a new .ply splat into Unity

1. Pipeline copies .ply to `unity-viewer/Assets/Splats/` automatically
2. In Unity: **Tools → Gaussian Splats → Create GaussianSplatAsset**
3. Browse to the .ply file, set Quality to Medium, click **Create Asset**
4. Create empty GameObject in Hierarchy, add **Gaussian Splat Renderer** component
5. Drag the created asset (blue cube icon) into the Splat Asset field
6. Set Transform **Rotation X = -90** (COLMAP → Unity coordinate fix)
7. Disable old splat GameObjects if switching rooms
8. Reposition Player inside the room (set Position, hit Play, adjust)

### Per-room tuning (TargetManager Inspector)
- **Spawn Distance Min/Max**: depends on room size (living room: 1-4)
- **Forward Cone Angle**: 40° (±20° from forward)
- **Target Timeout**: 6s default
- **Events Per Round**: 5
- **Min/Max Targets Per Event**: 1-3

## 5. Processed Rooms

### Office (first room)
| Setting | Value |
|---------|-------|
| Splat asset | `room.asset` (278K splats) |
| Player start | X=15.19, Y=0, Z=6.73 |
| Rotation Y | -162.5 |

### Living Room (second room)
| Setting | Value |
|---------|-------|
| Splat asset | `livingroom.asset` (435K splats) |
| Player start | X=15.24, Y=0, Z=7.01 |
| Rotation Y | -166.5 |
| Spawn distance | 1-4 (room is small in splat coords) |

## 6. CUDA Build Environment

Required for gsplat JIT compilation during splatfacto training:

| Component | Version | Notes |
|-----------|---------|-------|
| PyTorch | 2.4.1+cu124 | From pytorch-cu124 index |
| CUDA Toolkit | 12.4 | Must match PyTorch cu124 |
| MSVC | 14.44.35207 | cl.exe must be on PATH |
| gsplat | JIT compiled | Patched with `--allow-unsupported-compiler` |
| setuptools | <70 | Newer versions break distutils |
| ninja | latest | Required for JIT build |

## 7. Hardware

| Component | Spec |
|-----------|------|
| GPU | NVIDIA RTX A4500, 16GB VRAM |
| RAM | 32 GB |
| CPU | Intel i7-12800H (14 cores) |
| OS | Windows 11 Enterprise |

## 8. Key Gotchas

- **Never use pip directly** — always `uv pip install`
- **Never use chmod** or Unix-only commands — this is Windows
- **Never use ns-process-data** — broken with COLMAP 3.13
- **Always use sequential_matcher** for video input (exhaustive produces garbage)
- **MSVC must be on PATH** before splatfacto training (gsplat needs cl.exe) — pipeline does this automatically
- **Splat Rotation X = -90** in Unity (COLMAP Z-up → Unity Y-up)
- **Splat .ply import is manual**: Tools → Gaussian Splats → Create GaussianSplatAsset
- **Target spawn distance** must be tuned per room — splat rooms vary in scale
- **Q/E fly mode disabled** — horizontal walking only (simulates real movement)
- **Room calibration**: in-game wall marking (aim at 4 corners per wall, press F)
