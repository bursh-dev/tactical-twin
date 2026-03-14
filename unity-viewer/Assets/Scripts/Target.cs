using UnityEngine;

/// <summary>
/// Shooting range target with concentric rings scoring 1-10.
/// Center hit = 10 points, outermost = 1 point.
/// Visual: flat disc with painted rings facing the player.
/// </summary>
public class Target : MonoBehaviour
{
    public bool isHit { get; private set; }
    public int lastHitScore { get; private set; }
    public float TimeSinceHit => isHit ? Time.time - hitTime : 0f;

    private float hitTime;
    private Transform playerTransform;
    private GameObject visualDisc;

    void Start()
    {
        playerTransform = GameObject.Find("Player")?.transform;

        // Remove default sphere renderer/collider
        var defaultRenderer = GetComponent<Renderer>();
        if (defaultRenderer != null) defaultRenderer.enabled = false;

        // Build the target visual: concentric rings on a flat disc
        BuildTargetVisual();

        // Face the player
        if (playerTransform != null)
            transform.LookAt(playerTransform);
    }

    void BuildTargetVisual()
    {
        float baseRadius = transform.localScale.x * 0.5f;

        // Ring colors: alternating white and red, center is yellow (bullseye)
        Color[] ringColors = new Color[]
        {
            new Color(1f, 1f, 1f, 0.9f),   // ring 1 (outer) - white
            new Color(1f, 1f, 1f, 0.9f),   // ring 2 - white
            new Color(0f, 0f, 0f, 0.9f),   // ring 3 - black
            new Color(0f, 0f, 0f, 0.9f),   // ring 4 - black
            new Color(0f, 0.5f, 1f, 0.9f), // ring 5 - blue
            new Color(0f, 0.5f, 1f, 0.9f), // ring 6 - blue
            new Color(1f, 0f, 0f, 0.9f),   // ring 7 - red
            new Color(1f, 0f, 0f, 0.9f),   // ring 8 - red
            new Color(1f, 0.9f, 0f, 0.9f), // ring 9 - gold
            new Color(1f, 0.9f, 0f, 0.9f), // ring 10 (center) - gold bullseye
        };

        visualDisc = new GameObject("TargetVisual");
        visualDisc.transform.SetParent(transform, false);
        visualDisc.transform.localPosition = Vector3.zero;
        visualDisc.transform.localRotation = Quaternion.identity;

        // Create rings from outer to inner (so inner renders on top)
        for (int i = 0; i < 10; i++)
        {
            float ringFraction = (10 - i) / 10f; // 1.0 down to 0.1
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(visualDisc.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0f, i * 0.001f); // slight offset to prevent z-fighting
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            float ringScale = ringFraction;
            ring.transform.localScale = new Vector3(ringScale, 0.005f, ringScale);

            var renderer = ring.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = ringColors[i];
            renderer.material = mat;

            // Remove cylinder collider — we use the parent sphere collider
            Destroy(ring.GetComponent<Collider>());
        }
    }

    /// <summary>
    /// Calculate score based on where the hit landed relative to center.
    /// Returns 1-10: 10 = bullseye, 1 = outer edge.
    /// </summary>
    public int CalculateScore(Vector3 hitPoint)
    {
        float radius = transform.localScale.x * 0.5f;

        // Project hit point onto the target's face plane (perpendicular to forward)
        // This gives the 2D distance from center on the target face
        Vector3 toHit = hitPoint - transform.position;
        Vector3 forward = transform.forward;
        // Remove the forward component — keep only the lateral offset on the disc face
        Vector3 projected = toHit - Vector3.Dot(toHit, forward) * forward;
        float distFromCenter = projected.magnitude;

        float normalized = Mathf.Clamp01(distFromCenter / radius);

        // 10 rings: center (0-0.1) = 10pts, outer (0.9-1.0) = 1pt
        int score = Mathf.CeilToInt((1f - normalized) * 10f);
        return Mathf.Clamp(score, 1, 10);
    }

    public int OnHitWithScore(Vector3 hitPoint)
    {
        if (isHit) return 0;
        isHit = true;
        hitTime = Time.time;
        lastHitScore = CalculateScore(hitPoint);

        // Show hit marker — briefly flash the visual
        if (visualDisc != null)
        {
            // Tint all rings slightly green
            foreach (var renderer in visualDisc.GetComponentsInChildren<Renderer>())
            {
                var c = renderer.material.color;
                renderer.material.color = Color.Lerp(c, Color.green, 0.5f);
            }
        }

        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        return lastHitScore;
    }

    public void OnHit()
    {
        OnHitWithScore(transform.position); // default to center if called without hit point
    }

    public void Reset()
    {
        isHit = false;
        lastHitScore = 0;
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;
    }
}
