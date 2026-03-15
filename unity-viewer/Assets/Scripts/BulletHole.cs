using UnityEngine;

/// <summary>
/// Spawns a bullet hole decal at a hit point. Fades and destroys after lifetime.
/// </summary>
public class BulletHole : MonoBehaviour
{
    private float lifetime;
    private float elapsed;
    private new Renderer renderer;
    private Color baseColor;

    public static GameObject Spawn(Vector3 position, Vector3 normal, float lifetime = 5f, float size = 0.06f)
    {
        var hole = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hole.name = "BulletHole";
        Destroy(hole.GetComponent<Collider>());

        // Position slightly off the surface to avoid z-fighting
        hole.transform.position = position + normal * 0.005f;
        hole.transform.rotation = Quaternion.LookRotation(-normal);
        hole.transform.localScale = Vector3.one * size;

        // Dark material with slight transparency
        var rend = hole.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        mat.color = color;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3001;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        rend.material = mat;

        // Add a darker center dot for realism
        var center = GameObject.CreatePrimitive(PrimitiveType.Quad);
        center.name = "BulletCenter";
        Destroy(center.GetComponent<Collider>());
        center.transform.SetParent(hole.transform, false);
        center.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        center.transform.localScale = Vector3.one * 0.4f;
        var centerRend = center.GetComponent<Renderer>();
        var centerMat = new Material(mat);
        centerMat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f, 1f));
        centerMat.color = new Color(0.02f, 0.02f, 0.02f, 1f);
        centerRend.material = centerMat;

        // Add scorch ring (slightly larger, lighter)
        var scorch = GameObject.CreatePrimitive(PrimitiveType.Quad);
        scorch.name = "ScorchRing";
        Destroy(scorch.GetComponent<Collider>());
        scorch.transform.SetParent(hole.transform, false);
        scorch.transform.localPosition = new Vector3(0f, 0f, 0.001f);
        scorch.transform.localScale = Vector3.one * 1.6f;
        var scorchRend = scorch.GetComponent<Renderer>();
        var scorchMat = new Material(mat);
        scorchMat.SetColor("_BaseColor", new Color(0.15f, 0.12f, 0.08f, 0.5f));
        scorchMat.color = new Color(0.15f, 0.12f, 0.08f, 0.5f);
        scorchRend.material = scorchMat;

        var bh = hole.AddComponent<BulletHole>();
        bh.lifetime = lifetime;
        bh.renderer = rend;
        bh.baseColor = color;

        return hole;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Fade out in last 30% of lifetime
        float fadeStart = lifetime * 0.7f;
        if (elapsed > fadeStart)
        {
            float alpha = 1f - (elapsed - fadeStart) / (lifetime - fadeStart);
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                var c = rend.material.color;
                rend.material.color = new Color(c.r, c.g, c.b, c.a * alpha);
                rend.material.SetColor("_BaseColor", new Color(c.r, c.g, c.b, c.a * alpha));
            }
        }
    }
}
