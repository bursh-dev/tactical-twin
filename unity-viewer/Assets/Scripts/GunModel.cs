using UnityEngine;

/// <summary>
/// Creates a simple gun model from primitives, attached to the camera.
/// Also handles muzzle flash effect on shot.
/// </summary>
public class GunModel : MonoBehaviour
{
    private GameObject muzzleFlash;
    private float flashTimer;

    void Start()
    {
        // Gun body (dark gray box)
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(transform, false);
        body.transform.localPosition = new Vector3(0.25f, -0.25f, 0.6f);
        body.transform.localScale = new Vector3(0.06f, 0.12f, 0.4f);
        body.transform.localRotation = Quaternion.identity;
        SetColor(body, new Color(0.15f, 0.15f, 0.15f));
        Destroy(body.GetComponent<Collider>());
        body.layer = 0; // Default layer so raycast ignores it

        // Barrel (slightly lighter, extends forward)
        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.transform.SetParent(transform, false);
        barrel.transform.localPosition = new Vector3(0.25f, -0.2f, 0.85f);
        barrel.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SetColor(barrel, new Color(0.1f, 0.1f, 0.1f));
        Destroy(barrel.GetComponent<Collider>());
        barrel.layer = 0;

        // Grip (handle, angled slightly)
        var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.transform.SetParent(transform, false);
        grip.transform.localPosition = new Vector3(0.25f, -0.38f, 0.52f);
        grip.transform.localScale = new Vector3(0.055f, 0.18f, 0.08f);
        grip.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        SetColor(grip, new Color(0.2f, 0.18f, 0.12f));
        Destroy(grip.GetComponent<Collider>());
        grip.layer = 0;

        // Trigger guard
        var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.transform.SetParent(transform, false);
        guard.transform.localPosition = new Vector3(0.25f, -0.3f, 0.55f);
        guard.transform.localScale = new Vector3(0.02f, 0.06f, 0.1f);
        SetColor(guard, new Color(0.12f, 0.12f, 0.12f));
        Destroy(guard.GetComponent<Collider>());
        guard.layer = 0;

        // Sight (small nub on top)
        var sight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sight.transform.SetParent(transform, false);
        sight.transform.localPosition = new Vector3(0.25f, -0.17f, 0.7f);
        sight.transform.localScale = new Vector3(0.02f, 0.03f, 0.02f);
        SetColor(sight, Color.black);
        Destroy(sight.GetComponent<Collider>());
        sight.layer = 0;

        // Muzzle flash (hidden by default)
        muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleFlash.transform.SetParent(transform, false);
        muzzleFlash.transform.localPosition = new Vector3(0.25f, -0.2f, 0.98f);
        muzzleFlash.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        SetColor(muzzleFlash, new Color(1f, 0.9f, 0.3f));
        SetEmission(muzzleFlash, new Color(1f, 0.8f, 0.2f) * 3f);
        Destroy(muzzleFlash.GetComponent<Collider>());
        muzzleFlash.layer = 0;
        muzzleFlash.SetActive(false);
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
                muzzleFlash.SetActive(false);
        }
    }

    public void ShowMuzzleFlash()
    {
        muzzleFlash.SetActive(true);
        flashTimer = 0.05f;
    }

    void SetColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;
    }

    void SetEmission(GameObject obj, Color emissionColor)
    {
        var renderer = obj.GetComponent<Renderer>();
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", emissionColor);
    }
}
