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

## 2. Environment Setup (bash)

Required before running any pipeline command:

```bash
export CUDA_HOME="/c/Program Files/NVIDIA GPU Computing Toolkit/CUDA/v12.4"
MSVC_ROOT="/c/Program Files/Microsoft Visual Studio/18/Community/VC/Tools/MSVC/14.44.35207"
PROJECT_DIR="/c/projects/vs_code/tactical-twin"
export PATH="$PROJECT_DIR/deps/bin:$MSVC_ROOT/bin/Hostx64/x64:$PROJECT_DIR/pipeline/.venv/Scripts:$CUDA_HOME/bin:$PATH"
export PYTHONIOENCODING=utf-8
export PYTHONUTF8=1
```

## 3. Pipeline Commands

### Full automated pipeline
```bash
bash scripts/full-pipeline.sh assets/videos/VIDEO.mp4 SCENE_NAME
```

### Individual steps
```bash
cd pipeline/

# Step 1: Extract frames (2 fps)
.venv/Scripts/python.exe -m tactical_twin_pipeline.extract_frames ../assets/videos/VIDEO.mp4 -o work/SCENE/frames --fps 2

# Step 2: COLMAP (process_data.py — NOT ns-process-data)
.venv/Scripts/python.exe -m tactical_twin_pipeline.process_data work/SCENE/frames -o work/SCENE/colmap

# Step 3: Train splatfacto
.venv/Scripts/python.exe -m nerfstudio.scripts.train splatfacto \
  --data work/SCENE/colmap \
  --output-dir work/SCENE/trained \
  --max-num-iterations 15000 \
  --viewer.quit-on-train-completion True \
  colmap --colmap-path sparse/0

# Step 4: Export .ply
.venv/Scripts/python.exe -m nerfstudio.scripts.exporter gaussian-splat \
  --load-config work/SCENE/trained/colmap/splatfacto/TIMESTAMP/config.yml \
  --output-dir ../assets/splats/SCENE/
```

## 4. COLMAP 3.13 Critical Settings

**NEVER use `ns-process-data`** — it uses old COLMAP flag names that fail with 3.13.

### Flag renames (3.13 breaking changes)
| Old (broken) | New (correct) |
|-------------|--------------|
| `--SiftExtraction.use_gpu` | `--FeatureExtraction.use_gpu` |
| `--SiftMatching.use_gpu` | `--FeatureMatching.use_gpu` |

### Feature extraction
```
--FeatureExtraction.use_gpu 1
--SiftExtraction.max_num_features 8192
--SiftExtraction.estimate_affine_shape 1
--SiftExtraction.domain_size_pooling 1
--ImageReader.single_camera 1
--ImageReader.camera_model OPENCV
```

### Matching
```
sequential_matcher  (NEVER exhaustive for video input)
--FeatureMatching.use_gpu 1
--SequentialMatching.overlap 15
```

### Mapper
```
--Mapper.ba_global_max_num_iterations 50
--Mapper.ba_global_max_refinements 3
```

### Multiple reconstructions
COLMAP may produce sparse/0, sparse/1, etc. The `process_data.py` auto-selects the best one
(largest images.bin = most registered images) and moves it to sparse/0.

## 5. Unity Import Steps

### Import a new .ply splat into Unity

1. Copy .ply to `unity-viewer/Assets/Splats/`
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

## 6. Processed Rooms

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

## 7. CUDA Build Environment

Required for gsplat JIT compilation during splatfacto training:

| Component | Version | Notes |
|-----------|---------|-------|
| PyTorch | 2.4.1+cu124 | From pytorch-cu124 index |
| CUDA Toolkit | 12.4 | Must match PyTorch cu124 |
| MSVC | 14.44.35207 | cl.exe must be on PATH |
| gsplat | JIT compiled | Patched with `--allow-unsupported-compiler` |
| setuptools | <70 | Newer versions break distutils |
| ninja | latest | Required for JIT build |

## 8. Hardware

| Component | Spec |
|-----------|------|
| GPU | NVIDIA RTX A4500, 16GB VRAM |
| RAM | 32 GB |
| CPU | Intel i7-12800H (14 cores) |
| OS | Windows 11 Enterprise |

## 9. Key Gotchas

- **Never use pip directly** — always `uv pip install`
- **Never use chmod** or Unix-only commands — this is Windows
- **Never use ns-process-data** — broken with COLMAP 3.13
- **Always use sequential_matcher** for video input (exhaustive produces garbage)
- **MSVC must be on PATH** before splatfacto training (gsplat needs cl.exe)
- **Splat Rotation X = -90** in Unity (COLMAP Z-up → Unity Y-up)
- **Splat .ply import is manual**: Tools → Gaussian Splats → Create GaussianSplatAsset
- **Target spawn distance** must be tuned per room — splat rooms vary in scale
- **Q/E fly mode disabled** — horizontal walking only (simulates real movement)
