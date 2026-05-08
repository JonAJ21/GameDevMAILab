using UnityEngine;
using System.Collections.Generic;

public class ColorFilter : MonoBehaviour
{
    public enum FilterType { Cyan, Magenta, Yellow }
    public FilterType type = FilterType.Cyan;

    public void ProcessFilter(Vector3 direction, Vector3 color, Vector3 hitPoint, int depth, ref List<RayPacket> outputRays)
    {
        Vector3 trans = Vector3.one;
        switch (type)
        {
            case FilterType.Cyan: trans = new Vector3(0f, 1f, 1f); break;
            case FilterType.Magenta: trans = new Vector3(1f, 0f, 1f); break;
            case FilterType.Yellow: trans = new Vector3(1f, 1f, 0f); break;
        }

        Vector3 newColor = Vector3.Scale(color, trans);

        outputRays.Add(new RayPacket
        {
            origin = hitPoint + direction * 0.05f,
            direction = direction,
            color = newColor,
            depth = depth + 1
        });
    }
}