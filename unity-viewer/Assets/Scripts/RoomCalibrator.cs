using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Room calibration by shooting at walls.
/// Aim at each corner of a wall and press F to mark it (4 points = 1 wall).
/// Scroll wheel adjusts placement distance. A preview dot follows the crosshair.
/// Press C on start/end screen to recalibrate.
/// </summary>
public class RoomCalibrator : MonoBehaviour
{
    [Header("Settings")]
    public string roomName = "livingroom";
    public float defaultDistance = 3f;
    public float minDistance = 0.5f;
    public float maxDistance = 15f;

    [Header("References")]
    public Camera playerCamera;
    public FPSController fpsController;

    [Header("Auto-Mesh")]
    [Tooltip("If true, look for <roomName>_collision.obj in Assets/Splats/ and load it automatically")]
    public bool useAutoMesh = true;

    public bool IsCalibrating { get; private set; }
    private bool autoMeshLoaded;

    private float placementDistance;
    private List<Vector3> currentPoints = new List<Vector3>(); // 0-3 points for current wall
    private List<WallQuad> walls = new List<WallQuad>();
    private List<GameObject> markerObjects = new List<GameObject>();
    private List<GameObject> wallVisuals = new List<GameObject>();
    private List<GameObject> colliderObjects = new List<GameObject>();
    private GameObject previewDot;

    // Materials
    private Material markerMaterial;
    private Material wallMaterial;
    private Material previewMaterial;
    private Material lineMaterial;

    // GUI
    private GUIStyle labelStyle;
    private GUIStyle instructionStyle;
    private GUIStyle countStyle;
    private Texture2D bgTexture;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();

        if (fpsController == null)
            fpsController = FindFirstObjectByType<FPSController>();

        placementDistance = defaultDistance;
        CreateMaterials();

        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
        bgTexture.Apply();

        // Try auto-loading collision mesh from pipeline first
        if (TryLoadCollisionMesh())
        {
            Debug.Log($"[RoomCalibrator] Auto-loaded collision mesh for {roomName}");
            return;
        }

        // Fall back to saved manual wall calibration
        string path = GetSavePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var data = RoomBlockData.FromJson(json);
            if (data != null && data.blocks.Count > 0)
            {
                foreach (var b in data.blocks)
                {
                    walls.Add(new WallQuad
                    {
                        p0 = new Vector3(b.posX, b.posY, b.posZ),
                        p1 = new Vector3(b.scaleX, b.scaleY, b.scaleZ),
                        p2 = new Vector3(b.rotY, b.extra1, b.extra2),
                        p3 = new Vector3(b.extra3, b.extra4, b.extra5),
                    });
                }
                GenerateColliders();
            }
            else
            {
                EnterCalibration();
            }
        }
        else
        {
            EnterCalibration();
        }
    }

    void Update()
    {
        if (!IsCalibrating) return;

        // Scroll — adjust distance
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            placementDistance += scroll * 2f;
            placementDistance = Mathf.Clamp(placementDistance, minDistance, maxDistance);
        }

        // Update preview dot position
        UpdatePreviewDot();

        // F — place point
        if (Input.GetKeyDown(KeyCode.F))
        {
            PlacePoint();
        }

        // Z — undo last point (or last wall if no current points)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
        }

        // Escape — done
        if (Input.GetKeyDown(KeyCode.Escape) && currentPoints.Count == 0 && walls.Count > 0)
        {
            ConfirmCalibration();
        }
    }

    void UpdatePreviewDot()
    {
        if (playerCamera == null) return;

        Vector3 pos = playerCamera.transform.position + playerCamera.transform.forward * placementDistance;

        if (previewDot == null)
        {
            previewDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            previewDot.name = "PreviewDot";
            Destroy(previewDot.GetComponent<Collider>());
            previewDot.GetComponent<Renderer>().material = previewMaterial;
            previewDot.transform.localScale = Vector3.one * 0.15f;
        }

        previewDot.transform.position = pos;

        // Draw lines from placed points to preview
        // (handled in OnRenderObject)
    }

    void PlacePoint()
    {
        Vector3 pos = playerCamera.transform.position + playerCamera.transform.forward * placementDistance;
        currentPoints.Add(pos);
        SpawnMarker(pos, currentPoints.Count);

        // Every 4 points = one wall
        if (currentPoints.Count == 4)
        {
            CreateWall(currentPoints[0], currentPoints[1], currentPoints[2], currentPoints[3]);
            currentPoints.Clear();
        }
    }

    void CreateWall(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        var quad = new WallQuad { p0 = p0, p1 = p1, p2 = p2, p3 = p3 };
        walls.Add(quad);

        // Create visible quad
        CreateWallVisual(quad);

        // Remove point markers for this wall
        int startIdx = markerObjects.Count - 4;
        if (startIdx >= 0)
        {
            for (int i = startIdx; i < startIdx + 4; i++)
            {
                if (i < markerObjects.Count && markerObjects[i] != null)
                    Destroy(markerObjects[i]);
            }
            markerObjects.RemoveRange(startIdx, 4);
        }
    }

    void CreateWallVisual(WallQuad quad)
    {
        var go = new GameObject($"WallVisual_{wallVisuals.Count}");

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = wallMaterial;

        var mesh = new Mesh();
        mesh.vertices = new Vector3[] { quad.p0, quad.p1, quad.p2, quad.p3 };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 }; // Double-sided
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        wallVisuals.Add(go);
    }

    void Undo()
    {
        if (currentPoints.Count > 0)
        {
            // Remove last point
            currentPoints.RemoveAt(currentPoints.Count - 1);
            if (markerObjects.Count > 0)
            {
                Destroy(markerObjects[markerObjects.Count - 1]);
                markerObjects.RemoveAt(markerObjects.Count - 1);
            }
        }
        else if (walls.Count > 0)
        {
            // Remove last wall
            walls.RemoveAt(walls.Count - 1);
            if (wallVisuals.Count > 0)
            {
                Destroy(wallVisuals[wallVisuals.Count - 1]);
                wallVisuals.RemoveAt(wallVisuals.Count - 1);
            }
        }
    }

    void SpawnMarker(Vector3 position, int pointNumber)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = $"WallPoint_{pointNumber}";
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.15f;
        Destroy(marker.GetComponent<Collider>());

        var rend = marker.GetComponent<Renderer>();
        rend.material = markerMaterial;

        markerObjects.Add(marker);
    }

    public void EnterCalibration()
    {
        IsCalibrating = true;
        autoMeshLoaded = false;
        ClearColliders();
        currentPoints.Clear();

        // Recreate visuals for existing walls
        foreach (var v in wallVisuals)
        {
            if (v != null) Destroy(v);
        }
        wallVisuals.Clear();

        foreach (var wall in walls)
            CreateWallVisual(wall);
    }

    void ConfirmCalibration()
    {
        // Save
        var data = new RoomBlockData { roomName = roomName };
        foreach (var w in walls)
        {
            data.blocks.Add(new RoomBlockData.BlockEntry
            {
                posX = w.p0.x, posY = w.p0.y, posZ = w.p0.z,
                scaleX = w.p1.x, scaleY = w.p1.y, scaleZ = w.p1.z,
                rotY = w.p2.x, extra1 = w.p2.y, extra2 = w.p2.z,
                extra3 = w.p3.x, extra4 = w.p3.y, extra5 = w.p3.z,
            });
        }

        string path = GetSavePath();
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, data.ToJson());

        // Cleanup visuals
        foreach (var v in wallVisuals)
        {
            if (v != null) Destroy(v);
        }
        wallVisuals.Clear();
        ClearMarkers();
        if (previewDot != null) { Destroy(previewDot); previewDot = null; }

        // Generate colliders
        ClearColliders();
        GenerateColliders();

        IsCalibrating = false;
    }

    void GenerateColliders()
    {
        ClearColliders();
        float thickness = 0.3f;

        foreach (var wall in walls)
        {
            var go = new GameObject($"WallCollider_{colliderObjects.Count}");
            go.transform.SetParent(transform, false);

            // Compute wall center, orientation, and size from 4 points
            Vector3 center = (wall.p0 + wall.p1 + wall.p2 + wall.p3) / 4f;
            Vector3 bottomEdge = wall.p1 - wall.p0;
            Vector3 leftEdge = wall.p3 - wall.p0;
            float width = (bottomEdge.magnitude + (wall.p2 - wall.p3).magnitude) / 2f;
            float height = (leftEdge.magnitude + (wall.p2 - wall.p1).magnitude) / 2f;

            Vector3 right = bottomEdge.normalized;
            Vector3 up = leftEdge.normalized;
            Vector3 forward = Vector3.Cross(right, up).normalized;

            go.transform.position = center;
            go.transform.rotation = Quaternion.LookRotation(forward, up);

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(width, height, thickness);

            colliderObjects.Add(go);
        }

        // Auto-generate floor and ceiling from wall corner Y values
        GenerateFloorCeiling();
    }

    void GenerateFloorCeiling()
    {
        if (walls.Count == 0) return;

        // Find Y extents and XZ bounds from all wall corners
        float minY = float.MaxValue, maxY = float.MinValue;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var wall in walls)
        {
            foreach (var p in new[] { wall.p0, wall.p1, wall.p2, wall.p3 })
            {
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }
        }

        float sizeX = maxX - minX + 2f; // slight margin
        float sizeZ = maxZ - minZ + 2f;
        float centerX = (minX + maxX) / 2f;
        float centerZ = (minZ + maxZ) / 2f;
        float slabThickness = 0.3f;

        // Floor collider
        var floor = new GameObject("FloorCollider");
        floor.transform.SetParent(transform, false);
        floor.transform.position = new Vector3(centerX, minY - slabThickness / 2f, centerZ);
        var floorCol = floor.AddComponent<BoxCollider>();
        floorCol.size = new Vector3(sizeX, slabThickness, sizeZ);
        colliderObjects.Add(floor);

        // Ceiling collider
        var ceiling = new GameObject("CeilingCollider");
        ceiling.transform.SetParent(transform, false);
        ceiling.transform.position = new Vector3(centerX, maxY + slabThickness / 2f, centerZ);
        var ceilingCol = ceiling.AddComponent<BoxCollider>();
        ceilingCol.size = new Vector3(sizeX, slabThickness, sizeZ);
        colliderObjects.Add(ceiling);
    }

    void ClearColliders()
    {
        foreach (var obj in colliderObjects)
        {
            if (obj != null) Destroy(obj);
        }
        colliderObjects.Clear();
    }

    void ClearMarkers()
    {
        foreach (var m in markerObjects)
        {
            if (m != null) Destroy(m);
        }
        markerObjects.Clear();
    }

    bool TryLoadCollisionMesh()
    {
        if (!useAutoMesh) return false;

        // Look for collision .obj in Assets/Splats/
        string objPath = Path.Combine(Application.dataPath, "Splats", $"{roomName}_collision.obj");
        if (!File.Exists(objPath))
            return false;

        Debug.Log($"[RoomCalibrator] Loading collision mesh: {objPath}");
        Mesh mesh = LoadObjMesh(objPath);
        if (mesh == null || mesh.vertexCount == 0)
            return false;

        // Create a collision GameObject with MeshCollider
        var go = new GameObject("AutoCollisionMesh");
        go.transform.SetParent(transform, false);

        // Apply same rotation as splat (X = -90 for COLMAP Z-up to Unity Y-up)
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = false; // concave mesh for room collision

        colliderObjects.Add(go);
        autoMeshLoaded = true;

        Debug.Log($"[RoomCalibrator] Collision mesh loaded: {mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles");
        return true;
    }

    static Mesh LoadObjMesh(string path)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.StartsWith("v "))
            {
                string[] parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            else if (line.StartsWith("f "))
            {
                string[] parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                // Parse face indices (OBJ is 1-indexed, may have v/vt/vn format)
                var faceIndices = new List<int>();
                for (int i = 1; i < parts.Length; i++)
                {
                    string idx = parts[i].Split('/')[0];
                    faceIndices.Add(int.Parse(idx) - 1); // convert to 0-indexed
                }
                // Triangulate quads and n-gons via fan
                for (int i = 1; i < faceIndices.Count - 1; i++)
                {
                    triangles.Add(faceIndices[0]);
                    triangles.Add(faceIndices[i]);
                    triangles.Add(faceIndices[i + 1]);
                }
            }
        }

        if (vertices.Count == 0) return null;

        var mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    string GetSavePath()
    {
        return Path.Combine(Application.dataPath, "Configs", $"{roomName}_walls.json");
    }

    void CreateMaterials()
    {
        // Marker — bright yellow emissive
        markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        markerMaterial.color = Color.yellow;
        markerMaterial.EnableKeyword("_EMISSION");
        markerMaterial.SetColor("_EmissionColor", Color.yellow * 3f);

        // Wall visual — green semi-transparent
        wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wallMaterial.SetFloat("_Surface", 1);
        wallMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wallMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        wallMaterial.SetInt("_ZWrite", 0);
        wallMaterial.renderQueue = 3000;
        wallMaterial.color = new Color(0f, 1f, 0f, 0.25f);
        wallMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        // Preview dot — white emissive
        previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        previewMaterial.color = Color.white;
        previewMaterial.EnableKeyword("_EMISSION");
        previewMaterial.SetColor("_EmissionColor", Color.white * 2f);

        // Line material for connecting points
        lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    void OnRenderObject()
    {
        if (!IsCalibrating) return;
        if (currentPoints.Count == 0) return;

        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(new Color(1f, 1f, 0f, 0.8f));

        // Draw lines between placed points
        for (int i = 0; i < currentPoints.Count - 1; i++)
        {
            GL.Vertex(currentPoints[i]);
            GL.Vertex(currentPoints[i + 1]);
        }

        // Draw line from last point to preview dot
        if (previewDot != null && currentPoints.Count > 0)
        {
            GL.Color(new Color(1f, 1f, 1f, 0.4f));
            GL.Vertex(currentPoints[currentPoints.Count - 1]);
            GL.Vertex(previewDot.transform.position);
        }

        GL.End();
    }

    void OnGUI()
    {
        if (!IsCalibrating) return;

        InitStyles();

        // Top bar
        GUI.DrawTexture(new Rect(0, 0, Screen.width, 80), bgTexture);

        GUI.Label(new Rect(0, 8, Screen.width, 30),
            "ROOM CALIBRATION — Aim at wall corners and shoot", labelStyle);

        string pointInfo = currentPoints.Count > 0
            ? $"Point {currentPoints.Count}/4 — {4 - currentPoints.Count} more for this wall"
            : "Aim at a wall corner and press F";

        GUI.Label(new Rect(0, 38, Screen.width, 25),
            $"F: mark point | Z: undo | Scroll: distance ({placementDistance:F1}m) | Walls: {walls.Count}",
            instructionStyle);

        GUI.Label(new Rect(0, 58, Screen.width, 20), pointInfo, instructionStyle);

        // Bottom bar when can finish
        if (currentPoints.Count == 0 && walls.Count > 0)
        {
            GUI.DrawTexture(new Rect(0, Screen.height - 40, Screen.width, 40), bgTexture);
            GUI.Label(new Rect(0, Screen.height - 35, Screen.width, 30),
                "Press Esc to finish and save", instructionStyle);
        }
    }

    void InitStyles()
    {
        if (labelStyle != null) return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        labelStyle.normal.textColor = Color.white;

        instructionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
        };
        instructionStyle.normal.textColor = new Color(0.9f, 0.9f, 0.6f);
    }

    void OnDestroy()
    {
        if (previewDot != null) Destroy(previewDot);
    }

    public class WallQuad
    {
        public Vector3 p0, p1, p2, p3;
    }
}
