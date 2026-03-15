using UnityEngine;

/// <summary>
/// First-person controller with WASD movement and mouse look.
/// Attach to a GameObject with a CharacterController component.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float sprintSpeed = 3f;
    public float gravity = 20f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxPitchAngle = 85f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;
    private bool cursorLocked;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;

        LockCursor();
    }

    void Update()
    {
        HandleCursorLock();

        if (cursorLocked)
        {
            HandleMouseLook();
            HandleMovement();
        }
    }

    void HandleCursorLock()
    {
        if (Input.GetMouseButtonDown(0) && !cursorLocked)
            LockCursor();

        if (Input.GetKeyDown(KeyCode.Escape) && cursorLocked)
            UnlockCursor();
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Yaw — rotate the player body
        transform.Rotate(Vector3.up, mouseX);

        // Pitch — rotate the camera, clamped
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxPitchAngle, maxPitchAngle);
        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        float moveX = Input.GetAxisRaw("Horizontal"); // A/D
        float moveZ = Input.GetAxisRaw("Vertical");    // W/S

        float moveY = 0f;

        // Keep movement on horizontal plane regardless of camera pitch
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();
        Vector3 flatRight = transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        Vector3 move = flatRight * moveX + flatForward * moveZ;
        move = move.normalized * speed;

        // Store Y — no gravity, no vertical movement
        float fixedY = transform.position.y;

        // Stop dead on wall hit — no sliding
        Vector3 posBefore = transform.position;
        controller.Move(new Vector3(move.x, 0f, move.z) * Time.deltaTime);
        if ((controller.collisionFlags & CollisionFlags.Sides) != 0)
        {
            transform.position = posBefore;
        }

        // Lock Y position
        Vector3 pos = transform.position;
        pos.y = fixedY;
        transform.position = pos;
    }
}
