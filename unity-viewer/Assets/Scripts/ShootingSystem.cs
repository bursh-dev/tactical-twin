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

    private AudioSource audioSource;
    private float lastShotTime;

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
        if (shotSound != null)
            audioSource.PlayOneShot(shotSound);

        // Raycast from screen center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, targetLayer))
        {
            Target target = hit.collider.GetComponent<Target>();
            if (target != null && !target.isHit)
            {
                target.OnHit();

                if (hitSound != null)
                    audioSource.PlayOneShot(hitSound);

                if (hud != null)
                    hud.AddScore(1);
            }
        }
        else
        {
            if (missSound != null)
                audioSource.PlayOneShot(missSound, 0.5f);
        }
    }
}
