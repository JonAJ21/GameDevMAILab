using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Обзор")]
    public float mouseSensitivity = 2f;

    [Header("Границы")]
    public float fallLimit = -10f;             
    public Vector3 respawnPosition;             
    public bool useStartPosition = true;        

    private CharacterController controller;
    private Camera playerCam;
    private Vector3 velocity;
    private float xRotation;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCam = GetComponentInChildren<Camera>();

        if (useStartPosition)
            respawnPosition = transform.position;

        moveAction = new InputAction("Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", type: InputActionType.Value, binding: "<Mouse>/delta");
        jumpAction = new InputAction("Jump", type: InputActionType.Button, binding: "<Keyboard>/space");
        sprintAction = new InputAction("Sprint", type: InputActionType.Button, binding: "<Keyboard>/leftShift");

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (useStartPosition)
            respawnPosition = transform.position;
    }

    void Update()
    {
  
        CheckFallLimit();

        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        bool isSprinting = sprintAction.IsPressed();
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        controller.Move((move * currentSpeed + velocity) * Time.deltaTime);

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void CheckFallLimit()
    {
        if (transform.position.y < fallLimit)
        {
            TeleportToRespawn();
        }
    }

    void TeleportToRespawn()
    {
        controller.enabled = false;

        transform.position = respawnPosition;

        velocity.y = 0f;

        controller.enabled = true;

       // Debug.Log($"[PlayerController] Игрок упал за карту! Телепортация на {respawnPosition}");
    }

    public void SetRespawnPoint(Vector3 newRespawnPosition)
    {
        respawnPosition = newRespawnPosition;
        Debug.Log($"[PlayerController] Точка респавна обновлена: {respawnPosition}");
    }

    public void ForceRespawn()
    {
        TeleportToRespawn();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 fallPosition = transform.position;
        fallPosition.y = fallLimit;
        Gizmos.DrawWireCube(fallPosition, new Vector3(50f, 0.1f, 50f));

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? respawnPosition :
            (useStartPosition ? transform.position : respawnPosition), 0.5f);
        Gizmos.DrawRay(Application.isPlaying ? respawnPosition :
            (useStartPosition ? transform.position : respawnPosition), Vector3.up * 2f);
    }
}