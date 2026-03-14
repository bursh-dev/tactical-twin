using UnityEngine;

/// <summary>
/// Generates invisible box colliders to match the splat room boundaries.
/// Attach to an empty GameObject. Adjust bounds in the Inspector to match your room.
/// Press G in play mode to toggle collider visibility (green wireframes).
/// </summary>
public class RoomColliders : MonoBehaviour
{
    [Header("Room Bounds (adjust to match your splat)")]
    public Vector3 roomCenter = new Vector3(0f, 5f, 0f);
    public Vector3 roomSize = new Vector3(10f, 4f, 10f);

    [Header("Wall Thickness")]
    public float wallThickness = 0.3f;

    [Header("Debug")]
    public bool showColliders = false;

    private GameObject[] walls;

    void Start()
    {
        BuildColliders();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            showColliders = !showColliders;
            foreach (var wall in walls)
            {
                if (wall == null) continue;
                var rend = wall.GetComponent<Renderer>();
                if (rend != null) rend.enabled = showColliders;
            }
        }
    }

    void BuildColliders()
    {
        walls = new GameObject[6];

        float hw = roomSize.x / 2f;
        float hh = roomSize.y / 2f;
        float hd = roomSize.z / 2f;

        // Floor
        walls[0] = CreateWall("Floor",
            roomCenter + Vector3.down * hh,
            new Vector3(roomSize.x, wallThickness, roomSize.z));

        // Ceiling
        walls[1] = CreateWall("Ceiling",
            roomCenter + Vector3.up * hh,
            new Vector3(roomSize.x, wallThickness, roomSize.z));

        // Left wall (-X)
        walls[2] = CreateWall("WallLeft",
            roomCenter + Vector3.left * hw,
            new Vector3(wallThickness, roomSize.y, roomSize.z));

        // Right wall (+X)
        walls[3] = CreateWall("WallRight",
            roomCenter + Vector3.right * hw,
            new Vector3(wallThickness, roomSize.y, roomSize.z));

        // Back wall (-Z)
        walls[4] = CreateWall("WallBack",
            roomCenter + Vector3.back * hd,
            new Vector3(roomSize.x, roomSize.y, wallThickness));

        // Front wall (+Z)
        walls[5] = CreateWall("WallFront",
            roomCenter + Vector3.forward * hd,
            new Vector3(roomSize.x, roomSize.y, wallThickness));
    }

    GameObject CreateWall(string name, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(transform, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;

        // Invisible by default — renderer off, collider stays
        var rend = wall.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0f, 1f, 0f, 0.3f);
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        rend.material = mat;
        rend.enabled = showColliders;

        return wall;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireCube(roomCenter, roomSize);
    }
}
