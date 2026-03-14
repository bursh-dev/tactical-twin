using UnityEngine;

/// <summary>
/// Displays the player's real gun photo as a first-person overlay on the camera.
/// Falls back to a procedural model if no texture is assigned.
/// Also handles muzzle flash effect on shot.
/// </summary>
public class GunModel : MonoBehaviour
{
    [Header("Gun Photo")]
    public Texture2D gunTexture;

    private GameObject gunQuad;
    private GameObject muzzleFlash;
    private float flashTimer;

    void Start()
    {
        if (gunTexture == null)
            gunTexture = Resources.Load<Texture2D>("gun_hand");

        if (gunTexture != null)
            BuildPhotoGun();
        else
            BuildProceduralGun();

        BuildMuzzleFlash();
    }

    void BuildPhotoGun()
    {
        gunQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gunQuad.name = "GunPhoto";
        gunQuad.transform.SetParent(transform, false);
        Destroy(gunQuad.GetComponent<Collider>());
        gunQuad.layer = 0;

        float aspect = (float)gunTexture.width / gunTexture.height;
        float quadHeight = 0.45f;
        float quadWidth = quadHeight * aspect;

        // Position centered, scaled up, rotated so barrel points toward crosshair
        gunQuad.transform.localPosition = new Vector3(0.0f, -0.28f, 0.5f);
        gunQuad.transform.localScale = new Vector3(quadWidth * 1.4f, quadHeight * 1.4f, 1f);
        gunQuad.transform.localRotation = Quaternion.Euler(0f, 0f, 7f);

        // Unlit transparent material — the PNG already has alpha from rembg
        var renderer = gunQuad.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetTexture("_BaseMap", gunTexture);
        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.1f);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        renderer.material = mat;
    }

    void BuildProceduralGun()
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(transform, false);
        body.transform.localPosition = new Vector3(0.25f, -0.25f, 0.6f);
        body.transform.localScale = new Vector3(0.06f, 0.12f, 0.4f);
        SetColor(body, new Color(0.15f, 0.15f, 0.15f));
        Destroy(body.GetComponent<Collider>());
        body.layer = 0;

        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.transform.SetParent(transform, false);
        barrel.transform.localPosition = new Vector3(0.25f, -0.2f, 0.85f);
        barrel.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SetColor(barrel, new Color(0.1f, 0.1f, 0.1f));
        Destroy(barrel.GetComponent<Collider>());
        barrel.layer = 0;

        var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.transform.SetParent(transform, false);
        grip.transform.localPosition = new Vector3(0.25f, -0.38f, 0.52f);
        grip.transform.localScale = new Vector3(0.055f, 0.18f, 0.08f);
        grip.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        SetColor(grip, new Color(0.2f, 0.18f, 0.12f));
        Destroy(grip.GetComponent<Collider>());
        grip.layer = 0;
    }

    void BuildMuzzleFlash()
    {
        muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleFlash.transform.SetParent(transform, false);
        if (gunTexture != null)
            muzzleFlash.transform.localPosition = new Vector3(0.22f, -0.08f, 0.55f);
        else
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
