using UnityEngine;
using System.Collections.Generic;

public class SmartMirror : MonoBehaviour
{
    public enum MirrorType { Normal, Red, Green, Blue }
    public MirrorType type = MirrorType.Normal;

    public enum MirrorBehavior { Transmissive, Absorptive }
    public MirrorBehavior behavior = MirrorBehavior.Transmissive;
    public void ProcessMirror(Vector3 direction, Vector3 normal, Vector3 color, Vector3 hitPoint, int depth, ref List<RayPacket> outputRays)
    {
        Vector3 reflectedColor = color;
        Vector3 transmittedColor = Vector3.zero;
        bool hasReflection = true;
        bool hasTransmission = false;

        if (type == MirrorType.Normal)
        {
            hasReflection = true;
        }
        else
        {
            Vector3 mask = type == MirrorType.Red ? new Vector3(1, 0, 0) :
                           type == MirrorType.Green ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1);

            reflectedColor = Vector3.Scale(color, mask);
            transmittedColor = Vector3.Scale(color, Vector3.one - mask);

            if (reflectedColor.sqrMagnitude < 0.001f) hasReflection = false;

            if (behavior == MirrorBehavior.Transmissive && transmittedColor.sqrMagnitude > 0.001f)
                hasTransmission = true;
        }

        if (hasReflection)
        {
            Vector3 reflectDir = Vector3.Reflect(direction, normal);
            outputRays.Add(new RayPacket
            {
                origin = hitPoint + reflectDir * 0.05f,
                direction = reflectDir,
                color = reflectedColor,
                depth = depth + 1
            });
        }

        if (hasTransmission)
        {
            outputRays.Add(new RayPacket
            {
                origin = hitPoint + direction * 0.05f,
                direction = direction,
                color = transmittedColor,
                depth = depth + 1
            });
        }
    }
}