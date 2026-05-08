using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
public class Prism : MonoBehaviour
{
    [Header("Оптические свойства (Показатели преломления n)")]
    [Range(1.0f, 3.0f)] public float n_Red = 1.50f;
    [Range(1.0f, 3.0f)] public float n_Yellow = 1.55f;
    [Range(1.0f, 3.0f)] public float n_Green = 1.60f;
    [Range(1.0f, 3.0f)] public float n_Cyan = 1.65f;
    [Range(1.0f, 3.0f)] public float n_Blue = 1.70f;
    [Range(1.0f, 3.0f)] public float n_Magenta = 1.75f;

    void Awake()
    {
        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc == null) mc = gameObject.AddComponent<MeshCollider>();

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }
        else
        {
            Mesh mesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
            if (GetComponent<MeshFilter>() == null) gameObject.AddComponent<MeshFilter>().mesh = mesh;
            mc.sharedMesh = mesh;
        }

        var mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null || mr.sharedMaterial.name == "Default-Material")
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");
            Material mat = new Material(unlit);
            mat.color = new Color(0.8f, 0.9f, 1f, 0.3f);
            mr.sharedMaterial = mat;
        }
    }

    public void ProcessRefraction(Vector3 direction, Vector3 hitNormal, Vector3 color,
                                  bool isEntering, Vector3 hitPoint, ref List<RayPacket> outputRays)
    {
        bool isWhite = color.x > 0.8f && color.y > 0.8f && color.z > 0.8f;
        Vector3 normal = isEntering ? hitNormal : -hitNormal;

        if (isWhite && isEntering)
        {
            CreateRefractedRay(direction, normal, hitPoint, new Vector3(1, 0, 0), n_Red, ref outputRays);
            CreateRefractedRay(direction, normal, hitPoint, new Vector3(1, 1, 0), n_Yellow, ref outputRays);
            CreateRefractedRay(direction, normal, hitPoint, new Vector3(0, 1, 0), n_Green, ref outputRays);
            CreateRefractedRay(direction, normal, hitPoint, new Vector3(0, 1, 1), n_Cyan, ref outputRays);
            CreateRefractedRay(direction, normal, hitPoint, new Vector3(0, 0, 1), n_Blue, ref outputRays);
            CreateRefractedRay(direction, normal, hitPoint, new Vector3(1, 0, 1), n_Magenta, ref outputRays);
        }
        else
        {
            float nGlass = GetNForColor(color);
            float n1 = isEntering ? 1.0f : nGlass;
            float n2 = isEntering ? nGlass : 1.0f;

            Vector3 refractedDir;
            if (TryRefract(direction, normal, n1, n2, out refractedDir))
            {
                outputRays.Add(new RayPacket
                {
                    origin = hitPoint + refractedDir * 0.05f,
                    direction = refractedDir,
                    color = color,
                    depth = 1
                });
            }
        }
    }

    void CreateRefractedRay(Vector3 direction, Vector3 normal, Vector3 hitPoint,
                           Vector3 color, float nGlass, ref List<RayPacket> outputRays)
    {
        Vector3 refractedDir;
        if (TryRefract(direction, normal, 1.0f, nGlass, out refractedDir))
        {
            outputRays.Add(new RayPacket
            {
                origin = hitPoint + refractedDir * 0.05f,
                direction = refractedDir,
                color = color,
                depth = 1
            });
        }
    }

    float GetNForColor(Vector3 color)
    {
        if (color.x > 0.5f && color.y < 0.5f && color.z < 0.5f) return n_Red;
        if (color.x > 0.5f && color.y > 0.5f && color.z < 0.5f) return n_Yellow;
        if (color.x < 0.5f && color.y > 0.5f && color.z < 0.5f) return n_Green;
        if (color.x < 0.5f && color.y > 0.5f && color.z > 0.5f) return n_Cyan;
        if (color.x < 0.5f && color.y < 0.5f && color.z > 0.5f) return n_Blue;
        if (color.x > 0.5f && color.y < 0.5f && color.z > 0.5f) return n_Magenta;
        return (n_Red + n_Green + n_Blue) / 3f;
    }

    bool TryRefract(Vector3 I, Vector3 N, float n1, float n2, out Vector3 T)
    {
        T = Vector3.zero;
        float eta = n1 / n2;
        float cosI = -Vector3.Dot(N, I);
        float sinT2 = eta * eta * (1.0f - cosI * cosI);
        if (sinT2 > 1.0f) return false;
        float cosT = Mathf.Sqrt(1.0f - sinT2);
        T = (eta * I + (eta * cosI - cosT) * N).normalized;
        return true;
    }
}