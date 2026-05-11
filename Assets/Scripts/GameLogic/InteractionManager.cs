using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    [Header("Взаимодействие")]
    public float grabRange = 3f;
    public float holdDistance = 6f;
    public float minHoldDistance = 1.5f;
    public float maxHoldDistance = 6f;
    public float moveSmoothness = 8f;

    [Header("Вращение")]
    public float rotateSpeedFree = 100f;
    public float rotateSpeedAxis = 45f;
    public float rotateSpeedFine = 10f;

    [Header("Визуал")]
    public Color hoverColor = new Color(1f, 1f, 0.5f);
    public Color holdColor = new Color(1f, 0.5f, 0.5f);

    private Camera cam;
    private GameObject hoveredObject;
    private GameObject heldObject;
    private Rigidbody heldRb;
    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, RigidbodySettings> originalRigidbodySettings = new Dictionary<GameObject, RigidbodySettings>();
    private bool isFineRotation = false;

    private struct RigidbodySettings
    {
        public bool isKinematic;
        public bool useGravity;
    }

    void Start() => cam = GetComponent<Camera>();

    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Physics.Raycast(ray, out RaycastHit hit, grabRange);

        GameObject currentTarget = (hit.collider != null && hit.collider.CompareTag("Interactable"))
                                   ? hit.collider.gameObject : null;

        if (heldObject == null)
        {
            if (currentTarget != hoveredObject)
            {
                RevertHighlight(hoveredObject);
                hoveredObject = currentTarget;
                ApplyHighlight(hoveredObject, hoverColor);
            }

            if (mouse.leftButton.wasPressedThisFrame && hoveredObject != null)
            {
                Pickup(hoveredObject);
                hoveredObject = null;
            }
        }
        else
        {
            HandleHolding(mouse, keyboard);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Drop();
            }
        }
    }

    void ApplyHighlight(GameObject obj, Color targetColor)
    {
        if (obj == null) return;
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        if (!originalColors.ContainsKey(obj))
            originalColors[obj] = rend.material.color;

        rend.material.color = targetColor;
    }

    void RevertHighlight(GameObject obj)
    {
        if (obj == null || !originalColors.ContainsKey(obj)) return;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = originalColors[obj];

        originalColors.Remove(obj);
    }

    void Pickup(GameObject obj)
    {
        heldObject = obj;
        RevertHighlight(obj);
        ApplyHighlight(obj, holdColor);

        Vector3 worldScale = obj.transform.lossyScale;
        if (!originalScales.ContainsKey(obj))
            originalScales[obj] = worldScale;

        heldRb = obj.GetComponent<Rigidbody>();
        if (heldRb != null)
        {
            if (!originalRigidbodySettings.ContainsKey(obj))
            {
                originalRigidbodySettings[obj] = new RigidbodySettings
                {
                    isKinematic = heldRb.isKinematic,
                    useGravity = heldRb.useGravity
                };
            }

            heldRb.isKinematic = true;
            heldRb.useGravity = false;
        }

        Vector3 targetPos = GetCenterScreenPosition();
        obj.transform.position = targetPos;

        obj.transform.rotation = transform.rotation;

        obj.transform.localScale = worldScale;
    }

    Vector3 GetCenterScreenPosition()
    {
   
        Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return centerRay.origin + centerRay.direction * holdDistance;
    }

    void HandleHolding(Mouse mouse, Keyboard keyboard)
    {
        if (heldObject == null) return;

        // Регулировка расстояния (Колесико мыши)
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0)
        {
            holdDistance -= scroll * 0.5f;
            holdDistance = Mathf.Clamp(holdDistance, minHoldDistance, maxHoldDistance);
        }

        // Плавное движение точно к центру экрана
        Vector3 targetPos = GetCenterScreenPosition();
        heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPos, Time.deltaTime * moveSmoothness);

        // Проверка точного режима (Ctrl)
        isFineRotation = keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
        float currentRotateSpeed = isFineRotation ? rotateSpeedFine : rotateSpeedAxis;

        // ВРАЩЕНИЕ КЛАВИШАМИ
        if (keyboard != null)
        {
            // Ось Y (Yaw) - поворот влево/вправо
            float yaw = 0f;
            if (keyboard.qKey.isPressed) yaw = 1f;      // Q - влево
            if (keyboard.eKey.isPressed) yaw = -1f;     // E - вправо
            heldObject.transform.Rotate(Vector3.up * yaw * currentRotateSpeed * Time.deltaTime, Space.World);

            // Ось X (Pitch) - наклон вверх/вниз
            float pitch = 0f;
            if (keyboard.rKey.isPressed) pitch = 1f;    // R - вверх
            if (keyboard.fKey.isPressed) pitch = -1f;   // F - вниз
            heldObject.transform.Rotate(transform.right * pitch * currentRotateSpeed * Time.deltaTime, Space.World);

            // Ось Z (Roll) - вращение вокруг своей оси
            float roll = 0f;
            if (keyboard.tKey.isPressed) roll = 1f;     // T - по часовой
            if (keyboard.gKey.isPressed) roll = -1f;    // G - против часовой
            heldObject.transform.Rotate(transform.forward * roll * currentRotateSpeed * Time.deltaTime, Space.World);

            // Сброс вращения (V)
            if (keyboard.vKey.wasPressedThisFrame)
            {
                heldObject.transform.rotation = transform.rotation;
            }
        }
    }

    void Drop()
    {
        if (heldObject == null) return;

        if (heldRb != null && originalRigidbodySettings.ContainsKey(heldObject))
        {
            RigidbodySettings settings = originalRigidbodySettings[heldObject];
            heldRb.isKinematic = settings.isKinematic;
            heldRb.useGravity = settings.useGravity;
            originalRigidbodySettings.Remove(heldObject);
        }

        RevertHighlight(heldObject);

        if (originalScales.ContainsKey(heldObject))
            originalScales.Remove(heldObject);

        heldObject = null;
        heldRb = null;
    }

    void OnDrawGizmos()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        Gizmos.color = Color.yellow;
        Vector3 centerPos = GetCenterScreenPosition();
        Gizmos.DrawWireSphere(centerPos, 0.15f);
        Gizmos.DrawRay(transform.position, transform.forward * holdDistance);
    }
}