using UnityEngine;

/// <summary>
/// Individual target that can be hit by the shooting system.
/// </summary>
public class Target : MonoBehaviour
{
    public Color normalColor = Color.red;
    public Color hitColor = Color.green;
    public bool isHit { get; private set; }

    private Renderer targetRenderer;

    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
            targetRenderer.material.color = normalColor;
    }

    public void OnHit()
    {
        if (isHit) return;
        isHit = true;

        if (targetRenderer != null)
            targetRenderer.material.color = hitColor;

        // Optional: disable collider so it can't be hit again
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }

    public void Reset()
    {
        isHit = false;
        if (targetRenderer != null)
            targetRenderer.material.color = normalColor;

        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;
    }
}
