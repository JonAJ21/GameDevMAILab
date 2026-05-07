using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Чувствительность мыши")]
    public float mouseSensitivity = 1f;

    [Header("Скорость движения")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector2 mouseDelta;
    private Vector2 moveInput;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        mouseDelta = Mouse.current.delta.ReadValue();
        yRotation += mouseDelta.x * mouseSensitivity;
        xRotation -= mouseDelta.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        Keyboard keyboard = Keyboard.current;
        moveInput = Vector2.zero;
        if (keyboard.wKey.isPressed) moveInput.y += 1;
        if (keyboard.sKey.isPressed) moveInput.y -= 1;
        if (keyboard.dKey.isPressed) moveInput.x += 1;
        if (keyboard.aKey.isPressed) moveInput.x -= 1;

        float speed = keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * speed * Time.deltaTime;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}