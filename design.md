# Tactical Twin — Phase 1 POC Design

## 1. What Is Tactical Twin

A site-specific training simulation platform that turns real indoor environments into navigable, photorealistic 3D scenes. Security personnel can walk through digital copies of places they actually protect, practice spatial awareness, and rehearse scenarios — including target shooting.

**No existing product combines photorealistic real-location capture with tactical training.** Competitors (Operator XR, V-Armed, MILO VR, VirTra) use manually-built or generic environments.

## 2. Phase 1 Goal

Prove the core loop on a single laptop:

1. Record a room on a phone
2. System converts it to a photorealistic 3D scene (Gaussian splat)
3. Navigate the scene with FPS controls (WASD + mouse)
4. Shoot targets placed in the scene
5. Path to VR with minimal rework

## 3. System Architecture

```
┌─────────────┐     ┌──────────────────────────────────────────┐     ┌──────────────────┐
│   CAPTURE   │     │         RECONSTRUCTION PIPELINE          │     │   UNITY VIEWER   │
│             │     │              (Python / uv)               │     │                  │
│  Phone      │     │                                          │     │  Load .ply       │
│  Video      │────>│  ffmpeg ──> COLMAP ──> INRIA 3DGS ──> .ply ──>│  FPS Navigation  │
│  (60-120s)  │     │  (frames)   (poses)   (train.py)         │     │  Shooting        │
│             │     │                                          │     │  HUD / Scoring   │
└─────────────┘     └──────────────────────────────────────────┘     │  VR (future)     │
                                                                     └──────────────────┘
```

### Two Independent Sub-Projects

| Sub-project | Language | Purpose |
|-------------|----------|---------|
| `pipeline/` | Python 3.11 (uv-managed) | Video → 3D Gaussian splat (.ply) |
| `unity-viewer/` | C# (Unity) | Load splat, navigate, shoot, train |

## 4. Reconstruction Pipeline

### What Is a Gaussian Splat

A 3D scene represented as millions of tiny colored, transparent blobs (3D Gaussians) rather than traditional triangle meshes. Each blob has position, size, orientation, color, and opacity. When rendered together, they produce photorealistic views of the original scene from any angle.

### Pipeline Steps

```
Step 1: Extract Frames
  Input:  video.mp4 (phone recording, 60-120 seconds)
  Tool:   ffmpeg at 2 fps
  Output: 100-200 JPEG frames

Step 2: Estimate Camera Poses
  Input:  JPEG frames
  Tool:   COLMAP 3.13.0 (pre-built Windows CUDA binary in deps/bin/)
  Output: sparse/0/ (cameras.bin, images.bin, points3D.bin)

Step 3: Train Gaussian Splat
  Input:  COLMAP project (sparse/ + images/)
  Tool:   INRIA 3DGS train.py (original Gaussian splatting repo)
  Output: point_cloud/iteration_30000/point_cloud.ply
  Time:   ~15-30 minutes on RTX A4500

Step 4: Export
  Input:  trained point_cloud.ply
  Tool:   copy to assets/splats/
  Output: .ply file (100-500 MB)
```

### Fallback Options

| Scenario | Fallback |
|----------|----------|
| COLMAP fails on Windows | AnySplat (feed-forward, no COLMAP needed) |
| 3DGS training issues | OpenSplat (Windows-native C++ binary, paid) or Nerfstudio |
| Poor splat quality | SuGaR/MILo for mesh extraction + cleanup |

### Capture Guidelines

- Walk slowly and steadily through the room
- Overlap coverage — revisit areas from different angles
- Consistent lighting (avoid mixed sun/artificial)
- Avoid fast rotation or shaky movement
- 60-120 seconds is sufficient for a single room
- For connected rooms, walk through doorways with extra overlap

## 5. Unity Viewer

### Why Unity (Not Web-Based)

| Requirement | Web (Three.js) | Unity |
|-------------|----------------|-------|
| Render 6M+ Gaussians | No (WebGL limit ~2M) | Yes (Aras-P: 147fps) |
| Shooting with physics | Weeks of custom code | Built-in PhysX |
| Collision detection | Manual, no geometry | Built-in |
| VR headset support | Experimental WebXR | Production OpenXR |
| Multi-room scenes | Possible but complex | Scene graph, native |
| Desktop + VR from same codebase | No (rewrite) | Yes |

### Core Components

#### FPSController.cs
- Mouse look: cursor locked, pitch clamped ±85°
- WASD movement relative to camera facing
- Sprint (Shift), configurable speed
- Gravity + ground check (CharacterController)

#### SplatSceneLoader.cs
- Loads .ply files via Aras-P UnityGaussianSplatting plugin
- Supports multiple splats per scene (multi-room)
- Positions splats based on scene config
- Sets player spawn point

#### ShootingSystem.cs
- Mouse click → Physics.Raycast from camera center
- Hit detection against target colliders
- Visual feedback: target color change, particle burst
- Audio feedback: shot sound, hit confirmation
- Cooldown between shots

#### TargetManager.cs
- Reads `targets.json` config:
  ```json
  {
    "rooms": [
      {
        "name": "living-room",
        "splatFile": "living-room.ply",
        "targets": [
          { "position": [2.1, 1.5, -3.0], "radius": 0.3 },
          { "position": [-1.0, 1.2, 0.5], "radius": 0.25 }
        ]
      }
    ]
  }
  ```
- Instantiates target meshes (cylinders/spheres) at specified positions
- Tracks active/hit state per target

#### HUDManager.cs
- Crosshair (centered dot/cross)
- Score counter
- Targets remaining
- Timer (optional)
- Instructions overlay ("Click to start, WASD to move, Esc to release")

#### AudioManager.cs
- Shot sound on fire
- Hit confirmation sound on target hit
- Uses Unity AudioSource, preloaded clips

## 6. Multi-Room Support

### Single Continuous Capture
Record one video walking through all connected rooms. Pipeline produces one splat. Seamless navigation.

**Best for:** 2-5 connected rooms with doorways.

### Multi-Splat Stitching
Capture each room separately. Load multiple .ply files into the same Unity scene, positioned to match the real floor plan.

**Best for:** Large buildings, independently updatable rooms.

```
Unity Scene
├── Room_LivingRoom.ply    (position: 0, 0, 0)
├── Room_Kitchen.ply       (position: 5, 0, 0)
├── Room_Corridor.ply      (position: 2.5, 0, -3)
├── Collision_Walls         (invisible box colliders)
├── Trigger_Doorways        (trigger zones between rooms)
├── Targets                 (per-room target sets)
└── Player                  (FPS controller + camera)
```

### What This Enables
- Full building walk-throughs (lobby → corridor → offices)
- Entry/exit route practice
- Multi-room scenario scripting
- Independent room updates without re-scanning the whole site

## 7. VR Path (Milestone 4)

The Unity architecture makes VR a configuration change, not a rewrite:

| Desktop | VR Equivalent |
|---------|---------------|
| Mouse look | Headset tracking |
| WASD keys | Controller thumbstick |
| Mouse click to shoot | Controller trigger |
| Screen crosshair | Controller laser pointer |
| Monitor rendering | Stereo rendering (OpenXR) |

**Steps:**
1. Add Unity OpenXR package
2. Replace FPS camera with XR Rig
3. Map controller inputs
4. Add laser pointer for aiming
5. Test on Quest 3 / Pico 4

## 8. Tech Stack Summary

### Reconstruction Pipeline (Python)

| Tool | Version/Source | Role |
|------|---------------|------|
| Python | 3.11 | Runtime |
| uv | 0.9.21 | Package/venv manager |
| ffmpeg | latest (winget) | Video → frames |
| COLMAP | 3.13.0 CUDA (deps/bin/colmap.exe) | Camera pose estimation |
| INRIA 3DGS | github clone (deps/gaussian-splatting/) | Gaussian splat training |
| PyTorch | 2.x + CUDA 12.x | ML framework (required by 3DGS) |

### Unity Viewer (C#)

| Tool | Version/Source | Role |
|------|---------------|------|
| Unity | 2022.3 LTS or 6000.x | Engine |
| URP | (bundled) | Render pipeline |
| Aras-P UnityGaussianSplatting | github | Splat rendering |
| Unity OpenXR | (package) | VR support (future) |
| Unity PhysX | (built-in) | Raycasting, collision |

### Supporting Tools

| Tool | Role |
|------|------|
| SuperSplat (superspl.at/editor) | Browser-based splat editor/cleanup |
| SuGaR / MILo | Mesh extraction from splats (for collision geometry) |
| AnySplat | COLMAP-free fallback (feed-forward reconstruction) |

## 9. Project Structure

```
tactical-twin/
├── project-brief.md                       # Product vision
├── design.md                              # This document
├── .gitignore
│
├── pipeline/                              # Python reconstruction pipeline
│   ├── pyproject.toml                     # uv project: click, rich, ffmpeg-python
│   ├── .python-version                    # 3.11
│   └── src/tactical_twin_pipeline/
│       ├── __init__.py
│       ├── extract_frames.py              # ffmpeg: video → JPEGs
│       ├── process_data.py                # COLMAP: feature extraction + matching + mapping
│       ├── train_splat.py                 # INRIA 3DGS train.py wrapper
│       ├── export_splat.py                # Copy .ply to assets/
│       └── pipeline.py                    # End-to-end CLI orchestrator
│
├── unity-viewer/                          # Unity project
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   └── MainScene.unity
│   │   ├── Scripts/
│   │   │   ├── FPSController.cs
│   │   │   ├── SplatSceneLoader.cs
│   │   │   ├── ShootingSystem.cs
│   │   │   ├── TargetManager.cs
│   │   │   ├── HUDManager.cs
│   │   │   └── AudioManager.cs
│   │   ├── Configs/
│   │   │   └── targets.json
│   │   ├── Audio/
│   │   │   ├── shot.wav
│   │   │   └── hit.wav
│   │   └── Plugins/
│   │       └── UnityGaussianSplatting/    # Aras-P (git submodule)
│   ├── Packages/
│   └── ProjectSettings/
│
├── deps/                                  # External tools (gitignored)
│   ├── bin/colmap.exe                     # COLMAP 3.13.0 CUDA binary
│   └── gaussian-splatting/                # INRIA 3DGS repo clone
│
├── assets/
│   ├── splats/                            # Exported .ply files (gitignored)
│   └── videos/                            # Source videos (gitignored)
│
└── scripts/
    └── full-pipeline.sh                   # video → splat → copy to Unity
```

## 10. Implementation Milestones

### M1: "See my room in Unity"
| Step | Task | Output |
|------|------|--------|
| 1 | Scaffold repo + pipeline + Unity project | Project structure |
| 2 | Set up Python venv, verify CUDA | `torch.cuda.is_available() == True` |
| 3 | Record room video (phone) | video.mp4 |
| 4 | Extract frames (ffmpeg 2fps) | 100-200 JPEGs |
| 5 | Run COLMAP (ns-process-data) | transforms.json |
| 6 | Train splatfacto (~30 min) | Model checkpoint |
| 7 | Export to .ply | Splat file |
| 8 | Load in Unity (Aras-P plugin) | Room visible, orbit camera |

### M2: "Walk through my room"
| Step | Task | Output |
|------|------|--------|
| 1 | FPSController.cs | WASD + mouse-look navigation |
| 2 | SplatSceneLoader.cs | Runtime .ply loading + player spawn |
| 3 | Polish (crosshair, speed, instructions) | Usable experience |
| 4 | Capture + process second room | Pipeline validated as repeatable |

### M3: "Shoot targets in my room"
| Step | Task | Output |
|------|------|--------|
| 1 | TargetManager.cs + targets.json | Colored targets in scene |
| 2 | ShootingSystem.cs | Click-to-shoot with raycasting |
| 3 | Feedback (color, particles, sound) | Satisfying hit response |
| 4 | HUDManager.cs | Score, crosshair, timer |

### M4: "VR ready" (future)
| Step | Task | Output |
|------|------|--------|
| 1 | Unity OpenXR package | VR runtime |
| 2 | XR Rig replaces FPS camera | Head tracking |
| 3 | Controller input mapping | Thumbstick move, trigger shoot |
| 4 | Laser pointer for aiming | Visual aim feedback |

## 11. Hardware Requirements

### Development / Training Machine (confirmed)

| Component | Minimum | This Laptop |
|-----------|---------|-------------|
| GPU | NVIDIA with 8GB+ VRAM, CUDA 11.8+ | RTX A4500, 16GB, CUDA 12.8 |
| RAM | 16 GB | 32 GB |
| CPU | 8+ cores | i7-12800H (14 cores) |
| Storage | 50 GB free (splats + Unity) | OK |
| OS | Windows 10/11 | Windows 11 Enterprise |

### VR Headset (future, for M4)
- Meta Quest 3 (recommended, standalone + PC VR)
- Pico 4
- Any OpenXR-compatible headset via Unity

## 12. Key Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| COLMAP fails on Windows | Blocks pipeline | AnySplat (no COLMAP needed) |
| Nerfstudio CUDA issues | Blocks training | OpenSplat (Windows C++ binary); WSL2 fallback |
| Poor splat quality from video | Low realism | Re-capture with better lighting/overlap; try SuGaR for mesh cleanup |
| Large .ply files (100-500MB) | Slow load, git bloat | gitignore splats; Aras-P handles large scenes |
| No collision in splat | Walk through walls | Invisible Unity colliders for walls; SuGaR mesh extraction for auto-collision |
| Unity learning curve | Slower development | Aras-P has good docs; FPS controller is standard pattern |
| VR frame rate drops | Motion sickness | Foveated rendering; reduce splat count; test on target headset early |

## 13. Effort Estimate

| Milestone | Hands-on Time | GPU/Wait Time |
|-----------|---------------|---------------|
| M1 — See room in Unity | 1-2 days | ~30 min training per room |
| M2 — Walk through it | 1 day | — |
| M3 — Shoot targets | 1 day | — |
| M4 — VR (future) | 1 day | — |
| **Total POC** | **3-5 days** | |

## 14. Competitive Landscape

| Product | Real-Location Capture | Photorealistic | VR Training | Shooting | Open Gap |
|---------|----------------------|----------------|-------------|----------|----------|
| Operator XR | Import scans (manual) | No | Yes | Yes | Not automated capture |
| V-Armed | Manual VR builds | No | Yes | Yes | Not real locations |
| MILO VR | No | No | Yes | Yes | Generic environments |
| VirTra | Processed images | Partial | No (screens) | Yes | No VR, no walkthrough |
| Matterport | Yes (excellent) | Yes | No | No | No training features |
| **Tactical Twin** | **Yes (automated)** | **Yes (splats)** | **Yes (planned)** | **Yes** | **—** |
