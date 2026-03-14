# Tactical Twin — Phase 1 POC Design

## 1. What Is Tactical Twin

A site-specific training simulation platform that turns real indoor environments into navigable, photorealistic 3D scenes. Security personnel can walk through digital copies of places they actually protect, practice spatial awareness, and rehearse scenarios — including target shooting.

**No existing product combines photorealistic real-location capture with tactical training.** Competitors (Operator XR, V-Armed, MILO VR, VirTra) use manually-built or generic environments.

## 2. Phase 1 Goal

Prove the core loop on a single laptop:

1. Record a room on a phone
2. System converts it to a photorealistic 3D scene (Gaussian splat)
3. Navigate the scene with FPS controls (WASD + mouse)
4. Shoot targets in a round-based shooting range game
5. Path to VR with minimal rework

## 3. System Architecture

```
┌─────────────┐     ┌──────────────────────────────────────────┐     ┌──────────────────┐
│   CAPTURE   │     │         RECONSTRUCTION PIPELINE          │     │   UNITY VIEWER   │
│             │     │              (Python / uv)               │     │                  │
│  Phone      │     │                                          │     │  Load .ply       │
│  Video      │────>│  ffmpeg ──> COLMAP ──> Nerfstudio ──> .ply ──>│  FPS Navigation  │
│  (2-4 min)  │     │  (frames)   (poses)   splatfacto        │     │  Shooting Range  │
│             │     │                                          │     │  Scoring / HUD   │
└─────────────┘     └──────────────────────────────────────────┘     │  VR (future)     │
                                                                     └──────────────────┘
```

### Two Independent Sub-Projects

| Sub-project | Language | Purpose |
|-------------|----------|---------|
| `pipeline/` | Python 3.11 (uv-managed) | Video → 3D Gaussian splat (.ply) |
| `unity-viewer/` | C# (Unity 6 LTS) | Load splat, navigate, shoot, score |

## 4. Reconstruction Pipeline

### What Is a Gaussian Splat

A 3D scene represented as hundreds of thousands of tiny colored, transparent blobs (3D Gaussians) rather than traditional triangle meshes. Each blob has position, size, orientation, color, and opacity. When rendered together, they produce photorealistic views of the original scene from any angle.

### Pipeline Steps

```
Step 1: Extract Frames
  Input:  video.mp4 (phone recording, 2-4 minutes)
  Tool:   ffmpeg at 2 fps
  Output: 200-500 JPEG frames

Step 2: Estimate Camera Poses (COLMAP 3.13)
  Input:  JPEG frames
  Tool:   COLMAP 3.13.0 (pre-built Windows CUDA binary in deps/bin/)
  Via:    process_data.py (runs COLMAP directly, NOT ns-process-data)
  Output: sparse/0/ (cameras.bin, images.bin, points3D.bin)

  COLMAP 3.13 Settings (critical for reliable reconstruction):
    Feature extraction:
      --FeatureExtraction.use_gpu 1          (NOT --SiftExtraction.use_gpu — renamed in 3.13)
      --SiftExtraction.max_num_features 8192 (default 2048 is too few)
      --SiftExtraction.estimate_affine_shape 1
      --SiftExtraction.domain_size_pooling 1
      --ImageReader.single_camera 1
      --ImageReader.camera_model OPENCV
    Matching:
      sequential_matcher (NOT exhaustive — sequential is far better for video walkthroughs)
      --FeatureMatching.use_gpu 1            (NOT --SiftMatching.use_gpu — renamed in 3.13)
      --SequentialMatching.overlap 15        (match each frame against 15 neighbors)
    Mapper:
      --Mapper.ba_global_max_num_iterations 50
      --Mapper.ba_global_max_refinements 3

  Auto-selects best reconstruction when COLMAP produces multiple (sparse/0, sparse/1, etc.)
  by picking the one with the largest images.bin (most registered images).

Step 3: Train Gaussian Splat
  Input:  COLMAP output directory
  Tool:   Nerfstudio splatfacto (wraps gsplat library)
  Flag:   --colmap-path sparse/0  (required for COLMAP 3.13 output structure)
  Output: config.yml + model checkpoint
  Time:   ~15-30 minutes on RTX A4500 (15k iterations)

Step 4: Export
  Input:  Nerfstudio config.yml
  Tool:   ns-export gaussian-splat
  Output: .ply file (~60-100 MB, ~300-435k splats)
```

### CUDA Build Environment

The gsplat library requires JIT compilation with specific version matching:

| Component | Version | Notes |
|-----------|---------|-------|
| PyTorch | 2.4.1+cu124 | From pytorch-cu124 index |
| CUDA Toolkit | 12.4 | Must match PyTorch cu124 |
| MSVC | 14.44.35207 | VS 2025, 2022-compatible toolset |
| gsplat | JIT compiled | Patched with `--allow-unsupported-compiler` |
| setuptools | <70 | Newer versions removed `distutils._msvccompiler` |
| ninja | latest | Required for JIT build |

### Capture Guidelines

- **Duration**: 2-4 minutes per room/area
- **Orientation**: Horizontal (landscape) — wider FOV, better overlap
- Walk **slowly** and steadily — fast movement = motion blur
- Every spot should be visible from at least 3 different angles
- Stand in each corner and slowly pan
- Walk through doorways slowly, looking left and right
- Hold phone at chest/eye level, keep steady
- Turn on all lights, open curtains — bright and even lighting
- Avoid mirrors, glass, shiny surfaces, moving objects
- Lock exposure/focus if possible; 1080p is fine, 4K is better

## 5. Unity Viewer

### Why Unity (Not Web-Based)

| Requirement | Web (Three.js) | Unity |
|-------------|----------------|-------|
| Render 300k+ Gaussians | Possible but limited | Yes (Aras-P plugin, DX12) |
| Shooting with physics | Weeks of custom code | Built-in PhysX |
| Collision detection | Manual, no geometry | Built-in |
| VR headset support | Experimental WebXR | Production OpenXR |
| Desktop + VR from same codebase | No (rewrite) | Yes |

### Setup Requirements

- **Unity 6.3 LTS** (6000.3.11f1) with Universal 3D (URP) template
- **Graphics API**: Direct3D12 (DX11 won't work with splat plugin)
- **Render Graph Compatibility Mode**: OFF
- **Active Input Handling**: Both (old + new)
- **Aras-P UnityGaussianSplatting**: Added as local package in `Packages/`
- **GaussianSplatURPFeature**: Added to PC_Renderer asset

### Core Components

#### FPSController.cs
- Mouse look: cursor locked, pitch clamped ±85°
- WASD movement on horizontal plane (independent of camera pitch)
- No fly mode — Q/E disabled, simulates ground-level walking
- Sprint (Shift), configurable speed
- Click to lock cursor, Escape to release

#### GunModel.cs
- Loads real gun photo (gun_hand.png) from Resources as transparent quad overlay
- AI background removal (rembg/u2net) produces clean PNG with alpha channel
- Positioned centered, scaled 1.4x, 7° Z rotation so barrel points at crosshair
- Falls back to procedural gun model (cubes/cylinders) if no texture found
- Muzzle flash effect on shot (yellow emissive sphere, 50ms)

#### ShootingSystem.cs
- Mouse click → Physics.Raycast from camera center
- Hit detection against target colliders
- Ring-based scoring (1-10 based on hit distance from center)
- Speed multiplier calculation (2.0x instant → 1.0x at timeout)
- Bullet hole decals on targets (small, 1.5s), walls (larger, 8s), and misses (at 15 units, 8s)
- Muzzle flash trigger on gun model
- Audio: Glock 19 sound (Resources/glock19.mp3), fallback procedural; hit/miss procedural
- Cooldown between shots (0.3s)

#### BulletHole.cs
- Spawns bullet hole decals at hit points (Quad primitives with transparent material)
- Dark center dot + outer ring + scorch ring for realism
- Positioned slightly off surface to avoid z-fighting
- Fades out in last 30% of lifetime, then self-destructs
- On splat walls (no colliders): placed at fixed distance along ray direction

#### Target.cs
- Shooting range target disc with 10 concentric colored rings
- Ring colors: white → black → blue → red → gold (bullseye)
- Score calculation: projects hit point onto target face plane
- Center = 10 points, edge = 1 point
- Faces player on spawn
- Configurable timeout (8s default) — disappears if not hit
- Visual feedback: rings tint green on hit

#### TargetManager.cs (Event-Based Game)
- **Round = N events** (default 5), each event spawns 1-3 targets simultaneously
- Surprise factor: 3-7s random pause between events ("NAVIGATE..." shown)
- Targets spawn in ±20° arc in front of player (configurable)
- Distance-based sizing: small close (radius 0.03), bigger far (radius 0.08)
- Spawn distance configurable per room (Inspector: spawnDistanceMin/Max)
- Minimal height variation (±0.2) to keep targets at eye level
- Targets timeout after 6s (configurable)
- States: WaitingToStart → Playing (between events) → EventActive (targets live) → RoundOver
- Press R to start/restart round
- **Score = accuracy (1-10) × speed multiplier**: 2.0x for instant shots → 1.0x at timeout
- Tracks: hits, misses, total score, avg/best score, avg/best reaction time
- Grades: S (90%+), A (80%+), B (65%+), C (50%+), D (35%+), F

#### HUDManager.cs
- **Start screen**: "TACTICAL TWIN — Shooting Range", event/target/timeout config, "Score = Accuracy x Speed"
- **Playing HUD**: score, time, hits, avg/best/reaction time, event counter
- **Between events**: "NAVIGATE..." prompt
- **Event active**: "2 TARGETS!" flash on spawn, remaining target count
- **Summary screen**: grade, score/max, hits, missed, avg score, best shot, avg reaction, fastest shot, round time
- Gun-sight crosshair (4 lines with gap + center dot)
- Floating hit score popup with speed labels: "+18 LIGHTNING!", "+15 Fast!", "+10 BULLSEYE!"

#### RoomColliders.cs
- Generates 6 invisible box colliders (floor, ceiling, 4 walls) matching room bounds
- Configurable roomCenter, roomSize, wallThickness in Inspector
- Press G to toggle green wireframe visibility for debugging
- Works with CharacterController on Player for wall collision

#### ProceduralSFX.cs
- Runtime-generated audio clips (no external files needed for basic sounds)
- Gunshot: noise burst + sine crack with exponential decay
- Hit: ping + thud combination
- Miss: short whoosh

#### AudioManager.cs
- Singleton for centralized audio playback
- Supports custom wav/mp3 clips for spawn and hit sounds

## 6. Multi-Room Support

### Single Continuous Capture
Record one video walking through all connected rooms. Pipeline produces one splat. Seamless navigation.

**Best for:** 2-5 connected rooms with doorways.

### Multi-Splat Scenes
Capture each room separately. Load multiple splat assets into the same Unity scene.

**Best for:** Large buildings, independently updatable rooms.

## 7. VR Path (Milestone 4)

The Unity architecture makes VR a configuration change, not a rewrite:

| Desktop | VR Equivalent |
|---------|---------------|
| Mouse look | Headset tracking |
| WASD keys | Controller thumbstick |
| Mouse click to shoot | Controller trigger |
| Screen crosshair | Controller laser pointer |
| Monitor rendering | Stereo rendering (OpenXR) |

## 8. Tech Stack Summary

### Reconstruction Pipeline (Python)

| Tool | Version/Source | Role |
|------|---------------|------|
| Python | 3.11 | Runtime |
| uv | 0.9.21 | Package/venv manager |
| ffmpeg | 8.0.1 (downloaded binary) | Video → frames |
| COLMAP | 3.13.0 CUDA (deps/bin/colmap.exe) | Camera pose estimation |
| Nerfstudio | latest (pip) | Wraps splatfacto training + export |
| gsplat | JIT compiled (cu124) | Gaussian splatting rasterizer |
| PyTorch | 2.4.1+cu124 | ML framework |
| CUDA Toolkit | 12.4 | GPU compilation |

### Unity Viewer (C#)

| Tool | Version/Source | Role |
|------|---------------|------|
| Unity | 6.3 LTS (6000.3.11f1) | Engine |
| URP | 17.3.0 (bundled) | Render pipeline |
| Aras-P UnityGaussianSplatting | 1.1.1 (local package) | Splat rendering |
| Direct3D12 | (Windows) | Required graphics API |
| Unity OpenXR | (package, future) | VR support |

## 9. Tool Locations

All external tools live in `deps/bin/` (gitignored). They must be on PATH when running the pipeline.

| Tool | Location | Purpose |
|------|----------|---------|
| COLMAP | `deps/bin/colmap.exe` | Camera pose estimation |
| ffmpeg | `deps/bin/ffmpeg.exe` | Video → frames, audio trimming |
| ffprobe | `deps/bin/ffprobe.exe` | Video metadata inspection |
| CUDA Toolkit 12.4 | `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.4` | GPU compilation |
| MSVC 14.44 | `C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.44.35207` | C++ compiler for gsplat JIT |
| Python venv | `pipeline/.venv/` | Nerfstudio, PyTorch, gsplat |

### Running the pipeline

**Automated (recommended):**
```bash
bash scripts/full-pipeline.sh assets/videos/my-room.mp4 my-room
```

**Manual steps (if needed):**
```bash
cd pipeline/
CUDA_HOME="/c/Program Files/NVIDIA GPU Computing Toolkit/CUDA/v12.4"
MSVC_ROOT="/c/Program Files/Microsoft Visual Studio/18/Community/VC/Tools/MSVC/14.44.35207"
export PATH="../deps/bin:$MSVC_ROOT/bin/Hostx64/x64:.venv/Scripts:$CUDA_HOME/bin:$PATH"
export CUDA_HOME PYTHONIOENCODING=utf-8 PYTHONUTF8=1

# Step 1: Extract frames
ffmpeg -i ../assets/videos/VIDEO.mp4 -vf "fps=2" -q:v 2 work/SCENE/frames/frame_%05d.jpg

# Step 2: COLMAP (uses process_data.py, NOT ns-process-data)
# process_data.py handles COLMAP 3.13 flags, sequential matching, and best reconstruction selection
.venv/Scripts/python.exe -m tactical_twin_pipeline.process_data work/SCENE/frames -o work/SCENE/colmap

# Step 3: Train splatfacto (--colmap-path sparse/0 required for COLMAP 3.13)
.venv/Scripts/python.exe -m nerfstudio.scripts.train splatfacto \
  --data work/SCENE/colmap --output-dir work/SCENE/trained \
  --max-num-iterations 15000 --viewer.quit-on-train-completion True \
  colmap --colmap-path sparse/0

# Step 4: Export .ply
.venv/Scripts/python.exe -m nerfstudio.scripts.exporter gaussian-splat \
  --load-config work/SCENE/trained/.../config.yml \
  --output-dir ../assets/splats/SCENE/
```

### COLMAP 3.13 Compatibility Notes

COLMAP 3.13 renamed several CLI flags. The old names cause errors:
- `--SiftExtraction.use_gpu` → `--FeatureExtraction.use_gpu`
- `--SiftMatching.use_gpu` → `--FeatureMatching.use_gpu`
- `ns-process-data` uses the old flag names and will fail with COLMAP 3.13

**Do NOT use `ns-process-data`** — use `process_data.py` which runs COLMAP directly
with the correct 3.13 flags.

**Sequential matching** is critical for video walkthroughs — exhaustive matching
produces very poor reconstructions (few registered images, few 3D points).
Sequential matching with overlap=15 reliably registers 100% of frames.

## 10. Project Structure

```
tactical-twin/
├── project-brief.md
├── design/
│   ├── design.md                          # Architecture, components, milestones
│   └── spec.md                            # Tool paths, env setup, operational reference
├── CLAUDE.md
├── .gitignore
│
├── pipeline/                              # Python reconstruction pipeline
│   ├── pyproject.toml                     # uv project with cu124 indexes
│   ├── .python-version                    # 3.11
│   ├── .venv/                             # (gitignored)
│   ├── work/                              # (gitignored) per-scene working dirs
│   └── src/tactical_twin_pipeline/
│       ├── __init__.py
│       ├── extract_frames.py              # ffmpeg: video → JPEGs
│       ├── process_data.py                # COLMAP wrapper via ns-process-data
│       ├── train_splat.py                 # ns-train splatfacto wrapper
│       ├── export_splat.py                # ns-export gaussian-splat wrapper
│       └── pipeline.py                    # End-to-end CLI orchestrator
│
├── unity-viewer/                          # Unity 6 project (URP + DX12)
│   ├── Assets/
│   │   ├── Scenes/SampleScene.unity
│   │   ├── Scripts/
│   │   │   ├── FPSController.cs           # WASD + mouse + Q/E fly
│   │   │   ├── GunModel.cs                # Real gun photo overlay + muzzle flash
│   │   │   ├── ShootingSystem.cs          # Raycast shooting + ring scoring + bullet holes
│   │   │   ├── BulletHole.cs              # Decal spawning with fade-out
│   │   │   ├── Target.cs                  # Ring target with 1-10 scoring
│   │   │   ├── TargetManager.cs           # Event-based game (5 events, 1-3 targets each)
│   │   │   ├── HUDManager.cs              # Score, speed, reaction time, summary
│   │   │   ├── RoomColliders.cs           # Invisible wall colliders for room bounds
│   │   │   ├── ProceduralSFX.cs           # Runtime-generated audio
│   │   │   └── AudioManager.cs            # Audio singleton
│   │   ├── Resources/
│   │   │   ├── gun_hand.png               # Real gun photo with AI background removal
│   │   │   └── glock19.mp3               # Real Glock 19 gunshot sound
│   │   ├── Audio/
│   │   │   ├── hit.wav                    # Custom hit sound (1-2s)
│   │   │   └── spawn.wav                  # Custom spawn sound (0-1s)
│   │   ├── Configs/targets.json
│   │   ├── Splats/                        # (gitignored) imported splat assets
│   │   └── Settings/                      # URP renderer + pipeline assets
│   ├── Packages/
│   │   ├── manifest.json                  # Includes gaussian-splatting package
│   │   └── UnityGaussianSplatting/        # Aras-P plugin (gitignored)
│   └── ProjectSettings/
│
├── deps/                                  # External tools (gitignored)
│   └── bin/colmap.exe
│
├── assets/
│   ├── splats/                            # Exported .ply files (gitignored)
│   └── videos/                            # Source videos (gitignored)
│
└── scripts/
    └── full-pipeline.sh
```

## 10. Implementation Status

### M1: "See my room in Unity" — DONE
- Full pipeline working: video → ffmpeg → COLMAP → splatfacto → .ply → Unity
- Two rooms processed: office (278K splats) and living room (435K splats)
- Aras-P plugin with GaussianSplatURPFeature on DX12
- Pipeline automated: `bash scripts/full-pipeline.sh video.mp4 scene-name`
- COLMAP 3.13 compatibility fixed (flag renames, sequential matching)

### M2: "Walk through my room" — DONE
- Horizontal-plane WASD movement (no fly mode — simulates real walking)
- Movement independent of camera pitch (look up/down without drifting)
- Splat rotated -90° X to match Unity coordinate system
- Cursor lock/unlock with click/Escape
- RoomColliders.cs for invisible wall collision (needs per-room tuning)

### M3: "Shoot targets in my room" — DONE
- Event-based shooting game: 5 events per round, 1-3 targets per event
- Surprise factor: random 3-7s pauses between events
- Score = accuracy (1-10) × speed multiplier (2.0x instant → 1.0x at timeout)
- Real gun photo overlay (AI background removal with rembg/u2net)
- Bullet hole decals on targets, walls, and misses with fade-out
- Shooting range target discs with 10 concentric rings (1-10 scoring)
- Full stats: hits, misses, avg/best score, avg/best reaction time, grade (S-F)
- Summary screen with grade after each round
- Custom audio for spawn/hit, procedural audio for shot/miss

### M4: "VR ready" — PLANNED
- Unity OpenXR package
- XR Rig replaces FPS camera
- Controller input mapping
- Laser pointer for aiming

## 11. Hardware (Confirmed Working)

| Component | Spec |
|-----------|------|
| GPU | NVIDIA RTX A4500, 16GB VRAM |
| RAM | 32 GB |
| CPU | Intel i7-12800H (14 cores) |
| CUDA Toolkit | 12.4 |
| MSVC | 14.44.35207 (VS 2025) |
| OS | Windows 11 Enterprise |

## 12. Key Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| COLMAP fails on video | Blocks pipeline | Sequential matching + 8192 features; re-capture with better overlap; AnySplat fallback |
| COLMAP 3.13 flag changes | Pipeline errors | process_data.py uses correct flags directly; do NOT use ns-process-data |
| Exhaustive matching on video | Bad reconstruction | Always use sequential_matcher with overlap=15 for video walkthroughs |
| CUDA version mismatch | Blocks JIT compilation | Documented version matrix; gsplat patched; MSVC must be on PATH |
| Poor splat quality | Low realism | Longer video, better lighting, more overlap |
| No collision in splat | Walk through walls | RoomColliders.cs adds invisible box colliders; needs per-room tuning |
| Large .ply files | Slow load, git bloat | gitignored; Medium quality compression (~13MB asset) |

## 13. Competitive Landscape

| Product | Real-Location Capture | Photorealistic | VR Training | Shooting | Open Gap |
|---------|----------------------|----------------|-------------|----------|----------|
| Operator XR | Import scans (manual) | No | Yes | Yes | Not automated capture |
| V-Armed | Manual VR builds | No | Yes | Yes | Not real locations |
| MILO VR | No | No | Yes | Yes | Generic environments |
| VirTra | Processed images | Partial | No (screens) | Yes | No VR, no walkthrough |
| Matterport | Yes (excellent) | Yes | No | No | No training features |
| **Tactical Twin** | **Yes (automated)** | **Yes (splats)** | **Yes (planned)** | **Yes** | **—** |
