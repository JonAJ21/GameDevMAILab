using UnityEngine;

public class LightReceiver : MonoBehaviour
{
    [Header("Настройки")]
    public Color requiredColor = Color.white;
    [Range(0f, 1f)]
    public float colorTolerance = 0.1f;

    [Header("Дверь")]
    public DoorController targetDoor;

    private bool isReceivingRequiredColor = false;
    private bool checkThisFrame = false;

    void Update()
    {
        if (!checkThisFrame && isReceivingRequiredColor)
        {
            isReceivingRequiredColor = false;
            if (targetDoor != null)
            {
                //Debug.Log($"[LightReceiver] '{name}' - луч перестал попадать, закрываю дверь");
                targetDoor.Close();
            }
        }

        checkThisFrame = false;
    }

    public void ReceiveLight(Color lightColor)
    {
        checkThisFrame = true;

        float colorDifference = Vector3.Distance(
            new Vector3(lightColor.r, lightColor.g, lightColor.b),
            new Vector3(requiredColor.r, requiredColor.g, requiredColor.b)
        );

        bool colorMatches = colorDifference <= colorTolerance;

        //Debug.Log($"[LightReceiver] '{name}' - получен цвет: ({lightColor.r:F2}, {lightColor.g:F2}, {lightColor.b:F2}), " +
        //          $"требуется: ({requiredColor.r:F2}, {requiredColor.g:F2}, {requiredColor.b:F2}), " +
        //          $"разница: {colorDifference:F3}, совпадение: {colorMatches}");

        if (colorMatches && !isReceivingRequiredColor)
        {
            isReceivingRequiredColor = true;
            if (targetDoor != null)
            {
                //Debug.Log($"[LightReceiver] '{name}' - цвет совпал, открываю дверь!");
                targetDoor.Open();
            }
        }
        else if (!colorMatches && isReceivingRequiredColor)
        {
            isReceivingRequiredColor = false;
            if (targetDoor != null)
            {
                //Debug.Log($"[LightReceiver] '{name}' - цвет НЕ совпал, закрываю дверь");
                targetDoor.Close();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = requiredColor;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f);
    }
}