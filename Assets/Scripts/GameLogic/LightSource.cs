using UnityEngine;
using System.Collections.Generic;

public class LightSource : MonoBehaviour
{
    [Header("Настройки луча")]
    public Color rayColor = Color.white;
    public float maxDistance = 30f;
    public float lineWidth = 0.08f;
    public int maxBounces = 8;
    public int maxTotalRays = 64;

    private List<GameObject> visualSegments = new List<GameObject>();
    private Shader segmentShader;

    void Awake()
    {
        segmentShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (segmentShader == null) segmentShader = Shader.Find("Unlit/Color");
    }

    void Update()
    {
        foreach (var seg in visualSegments) Destroy(seg);
        visualSegments.Clear();
        TraceBranchingRays();
    }

    struct RayPacket
    {
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 color;
        public int depth;
    }

    void TraceBranchingRays()
    {
        Queue<RayPacket> queue = new Queue<RayPacket>();
        queue.Enqueue(new RayPacket
        {
            origin = transform.position + transform.forward * 0.15f,
            direction = transform.forward.normalized,
            color = new Vector3(rayColor.r, rayColor.g, rayColor.b),
            depth = 0
        });

        int raysSpawned = 0;
        float offset = 0.05f;

        while (queue.Count > 0 && raysSpawned < maxTotalRays)
        {
            RayPacket ray = queue.Dequeue();
            raysSpawned++;
            if (ray.depth > maxBounces) continue;

            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, maxDistance))
            {
                CreateSegment(ray.origin, hit.point, new Color(ray.color.x, ray.color.y, ray.color.z));

                Prism prism = hit.collider.GetComponent<Prism>();
                if (prism != null)
                {
                    Vector3 normal = hit.normal;
                    bool entering = Vector3.Dot(ray.direction, normal) < 0;
                    if (!entering) normal = -normal;
                    float[] n_glass = { 1.513f, 1.517f, 1.523f };
                    for (int i = 0; i < 3; i++)
                    {
                        if (ray.color[i] > 0.01f)
                        {
                            float n1 = entering ? 1.0f : n_glass[i];
                            float n2 = entering ? n_glass[i] : 1.0f;
                            Vector3 refracted;
                            if (TryRefract(ray.direction, normal, n1, n2, out refracted))
                            {
                                Vector3 newColor = Vector3.zero; newColor[i] = ray.color[i];
                                queue.Enqueue(new RayPacket { origin = hit.point + refracted * offset, direction = refracted, color = newColor, depth = ray.depth + 1 });
                            }
                        }
                    }
                    continue;
                }

                SmartMirror mirror = hit.collider.GetComponent<SmartMirror>();
                if (mirror != null)
                {
                    if (mirror.type == SmartMirror.MirrorType.Normal)
                    {
                        Vector3 reflectDir = Vector3.Reflect(ray.direction, hit.normal);
                        queue.Enqueue(new RayPacket { origin = hit.point + reflectDir * offset, direction = reflectDir, color = ray.color, depth = ray.depth + 1 });
                    }
                    else
                    {
                        Vector3 mask = mirror.type == SmartMirror.MirrorType.Red ? new Vector3(1, 0, 0) :
                                       mirror.type == SmartMirror.MirrorType.Green ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1);

                        Vector3 reflectedColor = Vector3.Scale(ray.color, mask);

                        if (reflectedColor.sqrMagnitude > 0.001f)
                        {
                            Vector3 reflectDir = Vector3.Reflect(ray.direction, hit.normal);
                            queue.Enqueue(new RayPacket { origin = hit.point + reflectDir * offset, direction = reflectDir, color = reflectedColor, depth = ray.depth + 1 });
                        }

                        if (mirror.behavior == SmartMirror.MirrorBehavior.Transmissive)
                        {
                            Vector3 transmittedColor = Vector3.Scale(ray.color, Vector3.one - mask);
                            if (transmittedColor.sqrMagnitude > 0.001f)
                            {
                                queue.Enqueue(new RayPacket { origin = hit.point + ray.direction * offset, direction = ray.direction, color = transmittedColor, depth = ray.depth + 1 });
                            }
                        }
                    }
                    continue;
                }

                ColorFilter filter = hit.collider.GetComponent<ColorFilter>();
                if (filter != null)
                {
                    Vector3 trans = Vector3.one;
                    if (filter.type == ColorFilter.FilterType.Cyan) trans = new Vector3(0f, 1f, 1f);
                    else if (filter.type == ColorFilter.FilterType.Magenta) trans = new Vector3(1f, 0f, 1f);
                    else if (filter.type == ColorFilter.FilterType.Yellow) trans = new Vector3(1f, 1f, 0f);

                    Vector3 newColor = Vector3.Scale(ray.color, trans);
                    queue.Enqueue(new RayPacket { origin = hit.point + ray.direction * offset, direction = ray.direction, color = newColor, depth = ray.depth + 1 });
                    continue;
                }

                LightReceiver receiver = hit.collider.GetComponent<LightReceiver>();
                if (receiver != null)
                {
                    receiver.ReceiveLight(new Color(ray.color.x, ray.color.y, ray.color.z));
                }
            }
            else
            {
                CreateSegment(ray.origin, ray.origin + ray.direction * maxDistance, new Color(ray.color.x, ray.color.y, ray.color.z));
            }
        }
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

    void CreateSegment(Vector3 start, Vector3 end, Color col)
    {
        GameObject obj = new GameObject($"Seg_{visualSegments.Count}");
        obj.transform.SetParent(transform, false);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.startColor = col;
        lr.endColor = col;
        lr.material = new Material(segmentShader) { color = col, hideFlags = HideFlags.DontSave };
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        visualSegments.Add(obj);
    }
}