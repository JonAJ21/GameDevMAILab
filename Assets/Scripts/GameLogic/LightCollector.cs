using UnityEngine;
using System.Collections.Generic;

public class LightCollector : MonoBehaviour
{
    [Header("Настройки коллектора")]
    public Transform outputPoint;
    public float exitOffset = 0.05f;

    [Header("Режимы работы")]
    public bool collectAllRays = true;
    public bool clampTo01 = false;
    public bool normalizeByCount = false;

    private Vector3 accumulatedColor = Vector3.zero;
    private int raysCollected = 0;
    private int localResetFrame = -1;

    public void StartAccumulation()
    {
        if (localResetFrame != Time.frameCount)
        {
            localResetFrame = Time.frameCount;
            accumulatedColor = Vector3.zero;
            raysCollected = 0;
        }
    }
    public void Absorb(Vector3 color)
    {
        accumulatedColor += color;
        raysCollected++;

        //Debug.Log($"[LightCollector] '{name}' поглотил луч #{raysCollected}: RGB({color.x:F2}, {color.y:F2}, {color.z:F2}), " +
        //          $"Сумма: ({accumulatedColor.x:F2}, {accumulatedColor.y:F2}, {accumulatedColor.z:F2})");
    }

    public void FinishAccumulation(ref List<RayPacket> outputRays)
    {
        if (raysCollected == 0) return;
        if (outputPoint == null)
        {
            //Debug.LogWarning($"[LightCollector] '{name}': outputPoint не назначен!");
            return;
        }

        Vector3 finalColor = accumulatedColor;

        if (normalizeByCount && raysCollected > 0)
            finalColor /= raysCollected;

        if (clampTo01)
            finalColor = new Vector3(
                Mathf.Clamp01(finalColor.x),
                Mathf.Clamp01(finalColor.y),
                Mathf.Clamp01(finalColor.z)
            );

        Vector3 outputOrigin = outputPoint.position + outputPoint.forward * exitOffset;

        outputRays.Add(new RayPacket
        {
            origin = outputOrigin,
            direction = outputPoint.forward,
            color = finalColor,
            depth = 0,
            isInternal = false,
            endPoint = Vector3.zero
        });

        raysCollected = 0;
        accumulatedColor = Vector3.zero;
    }

    public Vector3 GetAccumulatedColor() => accumulatedColor;
    public int GetRaysCollected() => raysCollected;

    void OnDrawGizmosSelected()
    {
        if (outputPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(outputPoint.position, 0.1f);
            Gizmos.DrawRay(outputPoint.position, outputPoint.forward * 0.5f);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}