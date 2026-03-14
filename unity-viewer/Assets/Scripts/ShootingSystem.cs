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

        // Load real gunshot sound, fallback to procedural
        if (shotSound == null)
        {
            var clip = Resources.Load<AudioClip>("glock19");
            if (clip != null) shotSound = clip;
            else shotSound = ProceduralSFX.GenerateGunshot();
        }
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

                // Bullet hole on target — short lifetime
                BulletHole.Spawn(hit.point, hit.normal, 1.5f, 0.04f);

                // Calculate speed multiplier for HUD display
                float reactionTime = targetManager != null
                    ? Time.time - targetManager.CurrentEventStartTime : 0f;
                float speedMul = targetManager != null
                    ? Mathf.Lerp(2f, 1f, Mathf.Clamp01(reactionTime / (targetManager != null ? targetManager.targetTimeout : 6f)))
                    : 1f;

                if (targetManager != null)
                    targetManager.RecordHit(points);

                if (hud != null)
                {
                    hud.ShowHitScore(points, hit.point, speedMul);
                }
            }
            else
            {
                // Bullet hole on wall/surface — longer lifetime
                BulletHole.Spawn(hit.point, hit.normal, 8f, 0.06f);

                if (hud != null)
                    hud.RecordShot(false, 0);
            }
        }
        else
        {
            audioSource.PlayOneShot(missSound, 0.5f);

            // No collider hit — place bullet hole at fixed distance (splat walls have no colliders)
            Vector3 impactPoint = ray.origin + ray.direction * 15f;
            BulletHole.Spawn(impactPoint, -ray.direction, 8f, 0.2f);

            if (hud != null)
                hud.RecordShot(false, 0);
        }
    }
}
