using UnityEngine;

/// <summary>
/// Raycast-based shooting system. Fires from camera center on mouse click.
/// </summary>
public class ShootingSystem : MonoBehaviour
{
    [Header("Shooting")]
    public float maxRange = 100f;
    public float cooldown = 0.3f;
    public LayerMask targetLayer;

    [Header("Audio")]
    public AudioClip shotSound;
    public AudioClip hitSound;
    public AudioClip missSound;

    [Header("References")]
    public Camera playerCamera;
    public HUDManager hud;
    public TargetManager targetManager;

    private AudioSource audioSource;
    private float lastShotTime;
    private GunModel gunModel;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (hud == null)
            hud = GetComponent<HUDManager>();

        if (targetManager == null)
            targetManager = FindFirstObjectByType<TargetManager>();

        // Create gun model on camera
        if (playerCamera != null)
        {
            gunModel = playerCamera.gameObject.AddComponent<GunModel>();
        }

        // Generate procedural sounds if none assigned
        if (shotSound == null) shotSound = ProceduralSFX.GenerateGunshot();
        if (hitSound == null) hitSound = ProceduralSFX.GenerateHit();
        if (missSound == null) missSound = ProceduralSFX.GenerateMiss();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked)
        {
            if (Time.time - lastShotTime >= cooldown)
                Shoot();
        }
    }

    void Shoot()
    {
        lastShotTime = Time.time;

        // Play shot sound
        audioSource.PlayOneShot(shotSound);

        // Muzzle flash
        if (gunModel != null)
            gunModel.ShowMuzzleFlash();

        // Raycast from screen center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
        {
            Target target = hit.collider.GetComponent<Target>();
            if (target != null && !target.isHit)
            {
                int points = target.OnHitWithScore(hit.point);
                audioSource.PlayOneShot(hitSound);

                if (targetManager != null)
                    targetManager.RecordHit(points);

                if (hud != null)
                {
                    hud.ShowHitScore(points, hit.point);
                }
            }
            else
            {
                if (hud != null)
                    hud.RecordShot(false, 0);
            }
        }
        else
        {
            audioSource.PlayOneShot(missSound, 0.5f);
            if (hud != null)
                hud.RecordShot(false, 0);
        }
    }
}
