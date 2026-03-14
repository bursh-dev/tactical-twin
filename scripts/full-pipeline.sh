#!/usr/bin/env bash
# End-to-end: video -> Gaussian splat .ply -> ready for Unity
#
# Usage: bash scripts/full-pipeline.sh <video_file> [scene_name]
#
# Example: bash scripts/full-pipeline.sh assets/videos/my-room.mp4

set -euo pipefail

VIDEO="${1:?Usage: bash $0 <video_file> [scene_name]}"
SCENE_NAME="${2:-$(basename "${VIDEO%.*}")}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Build environment — CUDA, MSVC, and local tools
CUDA_HOME="/c/Program Files/NVIDIA GPU Computing Toolkit/CUDA/v12.4"
MSVC_ROOT="/c/Program Files/Microsoft Visual Studio/18/Community/VC/Tools/MSVC/14.44.35207"
export PATH="$PROJECT_DIR/deps/bin:$MSVC_ROOT/bin/Hostx64/x64:$PROJECT_DIR/pipeline/.venv/Scripts:$CUDA_HOME/bin:$PATH"
export CUDA_HOME
export PYTHONIOENCODING=utf-8
export PYTHONUTF8=1

echo "=== Tactical Twin Pipeline ==="
echo "Video:  $VIDEO"
echo "Scene:  $SCENE_NAME"
echo ""

cd "$PROJECT_DIR/pipeline"

# Run the full pipeline
.venv/Scripts/python.exe -m tactical_twin_pipeline.pipeline "$PROJECT_DIR/$VIDEO" \
    --output "$PROJECT_DIR/assets/splats/$SCENE_NAME" \
    --work-dir "$PROJECT_DIR/pipeline/work"

echo ""
echo "=== Done ==="
echo "Splat file: assets/splats/$SCENE_NAME/$SCENE_NAME.ply"
echo "Import this .ply into Unity via the Aras-P plugin."
