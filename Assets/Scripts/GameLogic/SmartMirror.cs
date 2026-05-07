using UnityEngine;

public class SmartMirror : MonoBehaviour
{
    public enum MirrorType { Normal, Red, Green, Blue }
    public enum MirrorBehavior { Transmissive, Absorptive }

    [Header("Тип зеркала")]
    public MirrorType type = MirrorType.Normal;

    [Header("Режим работы")]
    [Tooltip("Transmissive: отражает целевой цвет, пропускает остальные\nAbsorptive: отражает целевой цвет, поглощает остальные")]
    public MirrorBehavior behavior = MirrorBehavior.Transmissive;
}