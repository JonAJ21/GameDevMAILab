using UnityEngine;
using System.Collections.Generic;

public class LightSource : MonoBehaviour
{
    [Header("Настройки луча")]
    public Color rayColor = Color.white;
    public float maxDistance = 30f;
    public float lineWidth = 0.08f;
    public int maxBounces = 8;
    public int maxTotalRays = 100;

    private List<GameObject> segmentPool = new List<GameObject>();
    private List<Material> materialPool = new List<Material>();
    private Shader segmentShader;

    void Awake()
    {
        segmentShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (segmentShader == null) segmentShader = Shader.Find("Unlit/Color");

        int poolSize = Mathf.Max(200, maxTotalRays * 2);
        for (int i = 0; i < poolSize; i++)
        {
            Material mat = new Material(segmentShader);
            mat.color = Color.white;
            mat.enableInstancing = false;
            mat.hideFlags = HideFlags.DontSave;
            materialPool.Add(mat);

            GameObject obj = new GameObject($"Seg_{i}");
            obj.transform.SetParent(transform, false);
            obj.SetActive(false);

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.material = mat;

            segmentPool.Add(obj);
        }
    }

    void Update()
    {
        for (int i = 0; i < segmentPool.Count; i++) segmentPool[i].SetActive(false);
        TraceBranchingRays();
    }

    int activeSegmentIndex = 0;

    void CreateSegment(Vector3 start, Vector3 end, Color col)
    {
        if (activeSegmentIndex >= segmentPool.Count) return;

        GameObject obj = segmentPool[activeSegmentIndex];
        obj.SetActive(true);

        LineRenderer lr = obj.GetComponent<LineRenderer>();
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.material.color = col;

        activeSegmentIndex++;
    }

    void TraceBranchingRays()
    {
        activeSegmentIndex = 0;

        Queue<RayPacket> queue = new Queue<RayPacket>();
        queue.Enqueue(new RayPacket
        {
            origin = transform.position + transform.forward * 0.15f,
            direction = transform.forward.normalized,
            color = new Vector3(rayColor.r, rayColor.g, rayColor.b),
            depth = 0
        });

        int raysSpawned = 0;

        while (queue.Count > 0 && raysSpawned < maxTotalRays)
        {
            RayPacket ray = queue.Dequeue();
            raysSpawned++;
            if (ray.depth > maxBounces) continue;

            Color drawColor = new Color(ray.color.x, ray.color.y, ray.color.z, 1f);

            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, maxDistance))
            {
                CreateSegment(ray.origin, hit.point, drawColor);

                Prism prism = hit.collider.GetComponent<Prism>();
                if (prism != null)
                {
                    List<RayPacket> newRays = new List<RayPacket>();
                    bool entering = Vector3.Dot(ray.direction, hit.normal) < 0;
                    prism.ProcessRefraction(ray.direction, hit.normal, ray.color, entering, hit.point, ref newRays);

                    foreach (var newRay in newRays) queue.Enqueue(newRay);
                    continue;
                }

                SmartMirror mirror = hit.collider.GetComponent<SmartMirror>();
                if (mirror != null)
                {
                    List<RayPacket> newRays = new List<RayPacket>();
                    mirror.ProcessMirror(ray.direction, hit.normal, ray.color, hit.point, ray.depth, ref newRays);

                    foreach (var newRay in newRays) queue.Enqueue(newRay);
                    continue;
                }

                ColorFilter filter = hit.collider.GetComponent<ColorFilter>();
                if (filter != null)
                {
                    List<RayPacket> newRays = new List<RayPacket>();
                    filter.ProcessFilter(ray.direction, ray.color, hit.point, ray.depth, ref newRays);

                    foreach (var newRay in newRays) queue.Enqueue(newRay);
                    continue;
                }

                LightReceiver receiver = hit.collider.GetComponent<LightReceiver>();
                if (receiver != null) receiver.ReceiveLight(drawColor);
            }
            else
            {
                CreateSegment(ray.origin, ray.origin + ray.direction * maxDistance, drawColor);
            }
        }
    }
}