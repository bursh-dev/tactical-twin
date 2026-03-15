"""Extract collision geometry from a Gaussian splat .ply file.

Uses RANSAC plane detection on the point cloud to find walls, floor, and ceiling.
Exports collision planes as a simple .obj mesh for Unity import.
"""

import struct
from pathlib import Path
from typing import NamedTuple

import click
import numpy as np
from rich.console import Console

console = Console()


class DetectedPlane(NamedTuple):
    """A plane detected via RANSAC."""
    normal: np.ndarray       # unit normal (3,)
    offset: float            # signed distance from origin
    inliers: np.ndarray      # indices of inlier points
    center: np.ndarray       # centroid of inliers (3,)
    extents: tuple           # (width, height) in plane-local coords
    corners: np.ndarray      # 4 corner points (4, 3)


def load_ply_positions(ply_path: Path) -> np.ndarray:
    """Load XYZ positions from a binary .ply file (Gaussian splat format).

    Reads only the x, y, z properties — ignores all other splat data.
    Handles both little-endian and big-endian binary PLY.
    """
    console.print(f"[bold]Loading point cloud: {ply_path}[/bold]")

    with open(ply_path, "rb") as f:
        # Parse header
        header_lines = []
        while True:
            line = f.readline().decode("ascii").strip()
            header_lines.append(line)
            if line == "end_header":
                break

        # Extract vertex count and property layout
        vertex_count = 0
        properties = []
        in_vertex = False

        for line in header_lines:
            if line.startswith("element vertex"):
                vertex_count = int(line.split()[-1])
                in_vertex = True
            elif line.startswith("element") and in_vertex:
                in_vertex = False
            elif line.startswith("property") and in_vertex:
                parts = line.split()
                prop_type = parts[1]
                prop_name = parts[2]
                properties.append((prop_name, prop_type))

        if vertex_count == 0:
            raise RuntimeError("No vertices found in PLY file")

        # Build struct format for one vertex
        type_map = {
            "float": "f", "double": "d",
            "uchar": "B", "uint8": "B",
            "char": "b", "int8": "b",
            "ushort": "H", "uint16": "H",
            "short": "h", "int16": "h",
            "uint": "I", "uint32": "I",
            "int": "i", "int32": "i",
        }

        # Check endianness from header
        fmt_prefix = "<"  # default little-endian
        for line in header_lines:
            if "binary_big_endian" in line:
                fmt_prefix = ">"
                break

        fmt = fmt_prefix
        xyz_offsets = []
        offset = 0
        for prop_name, prop_type in properties:
            code = type_map.get(prop_type, "f")
            if prop_name in ("x", "y", "z"):
                xyz_offsets.append((prop_name, offset, code))
            fmt += code
            offset += struct.calcsize(code)

        vertex_size = struct.calcsize(fmt)
        console.print(f"  Vertices: {vertex_count:,}, {vertex_size} bytes each")

        # Read all vertex data
        data = f.read(vertex_count * vertex_size)

    # Extract XYZ
    positions = np.zeros((vertex_count, 3), dtype=np.float32)
    for i in range(vertex_count):
        vertex = struct.unpack_from(fmt, data, i * vertex_size)
        # x, y, z are typically the first 3 properties
        positions[i, 0] = vertex[0]  # x
        positions[i, 1] = vertex[1]  # y
        positions[i, 2] = vertex[2]  # z

    console.print(f"  Loaded {vertex_count:,} positions")
    return positions


def ransac_plane(points: np.ndarray, threshold: float = 0.05,
                 iterations: int = 1000, min_inliers: int = 100) -> DetectedPlane | None:
    """Detect the dominant plane in a point cloud using RANSAC.

    Args:
        points: (N, 3) point cloud.
        threshold: Max distance from plane to count as inlier.
        iterations: Number of RANSAC iterations.
        min_inliers: Minimum inliers to accept a plane.

    Returns:
        DetectedPlane or None if no plane found.
    """
    n = len(points)
    if n < 3:
        return None

    best_inliers = None
    best_normal = None
    best_offset = None
    best_count = 0

    rng = np.random.default_rng(42)

    for _ in range(iterations):
        # Sample 3 random points
        idx = rng.choice(n, 3, replace=False)
        p0, p1, p2 = points[idx]

        # Compute plane normal
        v1 = p1 - p0
        v2 = p2 - p0
        normal = np.cross(v1, v2)
        norm_len = np.linalg.norm(normal)
        if norm_len < 1e-8:
            continue
        normal /= norm_len

        # Plane equation: normal . x = offset
        offset = np.dot(normal, p0)

        # Count inliers
        distances = np.abs(points @ normal - offset)
        inlier_mask = distances < threshold
        count = np.sum(inlier_mask)

        if count > best_count:
            best_count = count
            best_normal = normal
            best_offset = offset
            best_inliers = np.where(inlier_mask)[0]

    if best_count < min_inliers:
        return None

    # Compute plane extents from inlier points
    inlier_pts = points[best_inliers]
    center = inlier_pts.mean(axis=0)
    corners, extents = _compute_plane_rect(inlier_pts, best_normal, center)

    return DetectedPlane(
        normal=best_normal,
        offset=best_offset,
        inliers=best_inliers,
        center=center,
        extents=extents,
        corners=corners,
    )


def _compute_plane_rect(points: np.ndarray, normal: np.ndarray,
                        center: np.ndarray) -> tuple[np.ndarray, tuple]:
    """Compute an oriented bounding rectangle for points on a plane.

    Returns (corners (4,3), (width, height)).
    """
    # Project points onto plane's local 2D coordinate system
    # Choose basis vectors on the plane
    if abs(normal[1]) < 0.9:
        up = np.array([0, 1, 0], dtype=np.float64)
    else:
        up = np.array([1, 0, 0], dtype=np.float64)

    u = np.cross(normal, up)
    u /= np.linalg.norm(u)
    v = np.cross(normal, u)
    v /= np.linalg.norm(v)

    # Project inlier points to 2D
    relative = points - center
    coords_u = relative @ u
    coords_v = relative @ v

    # Axis-aligned bounding box in plane space
    u_min, u_max = coords_u.min(), coords_u.max()
    v_min, v_max = coords_v.min(), coords_v.max()

    width = u_max - u_min
    height = v_max - v_min

    # 4 corners in 3D
    corners = np.array([
        center + u_min * u + v_min * v,
        center + u_max * u + v_min * v,
        center + u_max * u + v_max * v,
        center + u_min * u + v_max * v,
    ])

    return corners, (float(width), float(height))


def detect_room_planes(points: np.ndarray, max_planes: int = 12,
                       threshold: float = 0.05, min_area: float = 0.5) -> list[DetectedPlane]:
    """Iteratively detect planes in a point cloud.

    Finds up to max_planes planes, removing inliers after each detection.
    Classifies planes as floor, ceiling, or wall based on normal orientation.

    Args:
        points: (N, 3) point cloud.
        max_planes: Maximum planes to detect.
        threshold: RANSAC inlier distance threshold.
        min_area: Minimum plane area (m^2) to keep.

    Returns:
        List of DetectedPlane, sorted by inlier count (largest first).
    """
    console.print(f"[bold]Detecting room planes (RANSAC)...[/bold]")
    console.print(f"  Points: {len(points):,}, threshold: {threshold}m, max planes: {max_planes}")

    remaining = points.copy()
    remaining_idx = np.arange(len(points))
    planes = []

    for i in range(max_planes):
        # Need enough points for meaningful detection
        min_inliers = max(100, len(remaining) // 50)
        plane = ransac_plane(remaining, threshold=threshold,
                             iterations=2000, min_inliers=min_inliers)

        if plane is None:
            console.print(f"  Plane {i+1}: no more planes found")
            break

        # Check minimum area
        area = plane.extents[0] * plane.extents[1]
        if area < min_area:
            console.print(f"  Plane {i+1}: too small ({area:.2f} m^2), skipping")
            # Remove these points and continue
            mask = np.ones(len(remaining), dtype=bool)
            mask[plane.inliers] = False
            remaining = remaining[mask]
            remaining_idx = remaining_idx[mask]
            continue

        # Classify plane
        ny = abs(plane.normal[1])
        if ny > 0.7:
            plane_type = "floor" if plane.center[1] < np.median(points[:, 1]) else "ceiling"
        else:
            plane_type = "wall"

        console.print(
            f"  Plane {i+1}: {plane_type} | "
            f"{len(plane.inliers):,} inliers | "
            f"{plane.extents[0]:.1f} x {plane.extents[1]:.1f} m | "
            f"area: {area:.1f} m^2"
        )

        planes.append(plane)

        # Remove inlier points from remaining
        mask = np.ones(len(remaining), dtype=bool)
        mask[plane.inliers] = False
        remaining = remaining[mask]
        remaining_idx = remaining_idx[mask]

    console.print(f"[green]Detected {len(planes)} planes[/green]")
    return planes


def planes_to_obj(planes: list[DetectedPlane], output_path: Path,
                  thickness: float = 0.15) -> Path:
    """Export detected planes as an .obj mesh with thickness.

    Each plane becomes a thin box (slab) for collision detection.

    Args:
        planes: List of detected planes.
        output_path: Where to write the .obj file.
        thickness: Slab thickness in meters.

    Returns:
        Path to the written .obj file.
    """
    output_path.parent.mkdir(parents=True, exist_ok=True)

    vertices = []
    faces = []
    v_offset = 1  # OBJ is 1-indexed

    for i, plane in enumerate(planes):
        c = plane.corners  # 4 corners on the plane surface
        n = plane.normal * (thickness / 2)

        # Front face (4 vertices) and back face (4 vertices)
        front = c + n
        back = c - n

        for v in front:
            vertices.append(v)
        for v in back:
            vertices.append(v)

        # 8 vertices per slab: front[0-3], back[4-7]
        f = v_offset
        # Front face
        faces.append((f+0, f+1, f+2, f+3))
        # Back face
        faces.append((f+7, f+6, f+5, f+4))
        # Side faces
        faces.append((f+0, f+4, f+5, f+1))
        faces.append((f+1, f+5, f+6, f+2))
        faces.append((f+2, f+6, f+7, f+3))
        faces.append((f+3, f+7, f+4, f+0))

        v_offset += 8

    with open(output_path, "w") as f:
        f.write(f"# Tactical Twin auto-generated collision mesh\n")
        f.write(f"# {len(planes)} planes, {len(vertices)} vertices, {len(faces)} faces\n\n")

        for v in vertices:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")

        f.write("\n")

        for face in faces:
            f.write(f"f {face[0]} {face[1]} {face[2]} {face[3]}\n")

    size_kb = output_path.stat().st_size / 1024
    console.print(f"[green]Collision mesh: {output_path} ({size_kb:.1f} KB, {len(planes)} slabs)[/green]")
    return output_path


def extract_collision(ply_path: Path, output_path: Path | None = None,
                      threshold: float = 0.05, max_planes: int = 12,
                      thickness: float = 0.15, downsample: int = 4) -> Path:
    """Full pipeline: .ply -> detect planes -> export collision .obj.

    Args:
        ply_path: Input Gaussian splat .ply file.
        output_path: Output .obj path. Defaults to same dir as .ply.
        threshold: RANSAC distance threshold.
        max_planes: Max planes to detect.
        thickness: Collision slab thickness (meters).
        downsample: Take every Nth point (speed vs accuracy).

    Returns:
        Path to the collision .obj file.
    """
    if output_path is None:
        output_path = ply_path.with_suffix(".obj")

    # Load point cloud
    positions = load_ply_positions(ply_path)

    # Downsample for speed
    if downsample > 1 and len(positions) > 10000:
        original = len(positions)
        positions = positions[::downsample]
        console.print(f"  Downsampled: {original:,} > {len(positions):,} (every {downsample}th point)")

    # Detect planes
    planes = detect_room_planes(positions, max_planes=max_planes, threshold=threshold)

    if not planes:
        console.print("[yellow]No planes detected — collision mesh not generated[/yellow]")
        return output_path

    # Export as .obj
    return planes_to_obj(planes, output_path, thickness=thickness)


@click.command()
@click.argument("ply_path", type=click.Path(exists=True, path_type=Path))
@click.option("--output", "-o", type=click.Path(path_type=Path), default=None,
              help="Output .obj file path (default: same name as .ply)")
@click.option("--threshold", "-t", default=0.05, help="RANSAC inlier threshold in meters (default: 0.05)")
@click.option("--max-planes", "-m", default=12, help="Maximum planes to detect (default: 12)")
@click.option("--thickness", default=0.15, help="Collision slab thickness in meters (default: 0.15)")
@click.option("--downsample", "-d", default=4, help="Downsample factor (default: 4, every 4th point)")
def main(ply_path: Path, output: Path | None, threshold: float,
         max_planes: int, thickness: float, downsample: int):
    """Extract collision geometry from a Gaussian splat PLY file."""
    extract_collision(ply_path, output, threshold=threshold,
                      max_planes=max_planes, thickness=thickness,
                      downsample=downsample)


if __name__ == "__main__":
    main()
