using UnityEngine;

public class ColorFilter : MonoBehaviour
{
    public enum FilterType { Cyan, Magenta, Yellow }
    public FilterType type = FilterType.Cyan;

    private Vector3 transmission;

    void Awake()
    {
        switch (type)
        {
            case FilterType.Cyan: transmission = new Vector3(0f, 1f, 1f); break;
            case FilterType.Magenta: transmission = new Vector3(1f, 0f, 1f); break; 
            case FilterType.Yellow: transmission = new Vector3(1f, 1f, 0f); break; 
        }
    }

    public Vector3 GetTransmission() => transmission;
}