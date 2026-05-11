using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private static List<LightCollector> allCollectors = new List<LightCollector>();
    private static int lastCollectorUpdateFrame = -1;
    private static int globalPassNumber = 0;

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
        if (lastCollectorUpdateFrame != Time.frameCount)
        {
            lastCollectorUpdateFrame = Time.frameCount;
            allCollectors = new List<LightCollector>(Object.FindObjectsByType<LightCollector>(FindObjectsInactive.Exclude));
            globalPassNumber = 0;
        }

        for (int i = 0; i < segmentPool.Count; i++)
            segmentPool[i].SetActive(false);

        if (IsFirstSourceThisFrame())
        {
            foreach (var col in allCollectors)
            {
                if (col != null)
                    col.StartAccumulation();
            }
        }

        TraceAllRays();
    }

    private bool IsFirstSourceThisFrame()
    {
        var sources = Object.FindObjectsByType<LightSource>(FindObjectsInactive.Exclude);
        return sources.Length > 0 && sources[0] == this;
    }

    private bool IsLastSourceThisFrame()
    {
        var sources = Object.FindObjectsByType<LightSource>(FindObjectsInactive.Exclude);
        return sources.Length > 0 && sources[^1] == this;
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

    void TraceAllRays()
    {
        activeSegmentIndex = 0;
        int totalRaysProcessed = 0;

        // Трассировка входящих лучей от источников
        Queue<RayPacket> incomingQueue = new Queue<RayPacket>();

        // Добавляем начальный луч от этого источника
        incomingQueue.Enqueue(new RayPacket
        {
            origin = transform.position + transform.forward * 0.15f,
            direction = transform.forward.normalized,
            color = new Vector3(rayColor.r, rayColor.g, rayColor.b),
            depth = 0,
            isInternal = false,
            endPoint = Vector3.zero
        });

        // Обрабатываем все входящие лучи (включая преломления/отражения)
        while (incomingQueue.Count > 0 && totalRaysProcessed < maxTotalRays)
        {
            RayPacket ray = incomingQueue.Dequeue();
            totalRaysProcessed++;

            if (ray.isInternal)
            {
                CreateSegment(ray.origin, ray.endPoint, new Color(ray.color.x, ray.color.y, ray.color.z, 1f));
                continue;
            }

            if (ray.depth > maxBounces) continue;

            Color drawColor = new Color(ray.color.x, ray.color.y, ray.color.z, 1f);

            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, maxDistance))
            {
                CreateSegment(ray.origin, hit.point, drawColor);

                // Проверяем коллектор
                LightCollector collector = hit.collider.GetComponent<LightCollector>();
                if (collector != null)
                {
              
                    collector.Absorb(ray.color);
                    continue;
                }

                // Обрабатываем другие компоненты
                ProcessOpticalComponent(ray, hit, ref incomingQueue);
            }
            else
            {
                CreateSegment(ray.origin, ray.origin + ray.direction * maxDistance, drawColor);
            }
        }

        // Генерация выходных лучей от коллекторов
        List<RayPacket> outputRays = new List<RayPacket>();

        if (IsLastSourceThisFrame())
        {
            foreach (var col in allCollectors)
            {
                if (col != null)
                    col.FinishAccumulation(ref outputRays);
            }
        }

        // Фаза 3: Много-проходная трассировка выходных лучей
        // Позволяет цепочки: Collector1 → Collector2 → Collector3 ...
        int maxCollectorPasses = 5; // Максимальное количество проходов через коллекторы
        List<RayPacket> currentOutputRays = outputRays;

        for (int pass = 0; pass < maxCollectorPasses && currentOutputRays.Count > 0; pass++)
        {
            globalPassNumber++;
            List<RayPacket> newOutputRays = new List<RayPacket>();

            foreach (var ray in currentOutputRays)
            {
                if (totalRaysProcessed >= maxTotalRays) break;

                totalRaysProcessed++;

                if (ray.depth > maxBounces) continue;

                Color drawColor = new Color(ray.color.x, ray.color.y, ray.color.z, 1f);

                if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, maxDistance))
                {
                    CreateSegment(ray.origin, hit.point, drawColor);

                    // Проверяем коллектор (разрешаем проход через другой коллектор)
                    LightCollector collector = hit.collider.GetComponent<LightCollector>();
                    if (collector != null)
                    {
                        // Поглощаем луч в новом коллекторе
                        collector.StartAccumulation(); // Сбрасываем для нового прохода
                        collector.Absorb(ray.color);

                        // Генерируем новый выходной луч для следующего прохода
                        List<RayPacket> tempRays = new List<RayPacket>();
                        collector.FinishAccumulation(ref tempRays);
                        newOutputRays.AddRange(tempRays);

                        continue;
                    }

                    // Обрабатываем другие оптические компоненты
                    Queue<RayPacket> tempQueue = new Queue<RayPacket>();
                    if (ProcessOpticalComponent(ray, hit, ref tempQueue))
                    {
                        // Добавляем результаты в новый список
                        while (tempQueue.Count > 0)
                            newOutputRays.Add(tempQueue.Dequeue());
                    }
                }
                else
                {
                    CreateSegment(ray.origin, ray.origin + ray.direction * maxDistance, drawColor);
                   
                }
            }

            currentOutputRays = newOutputRays;

        
        }
    }

    bool ProcessOpticalComponent(RayPacket ray, RaycastHit hit, ref Queue<RayPacket> queue)
    {
        Prism prism = hit.collider.GetComponent<Prism>();
        if (prism != null)
        {
            List<RayPacket> newRays = new List<RayPacket>();
            prism.ProcessRefraction(ray.direction, hit.normal, ray.color, hit.point, ref newRays);
            foreach (var newRay in newRays) queue.Enqueue(newRay);
            return true;
        }

        SmartMirror mirror = hit.collider.GetComponent<SmartMirror>();
        if (mirror != null)
        {
            List<RayPacket> newRays = new List<RayPacket>();
            mirror.ProcessMirror(ray.direction, hit.normal, ray.color, hit.point, ray.depth, ref newRays);
            foreach (var newRay in newRays) queue.Enqueue(newRay);
            return true;
        }

        ColorFilter filter = hit.collider.GetComponent<ColorFilter>();
        if (filter != null)
        {
            List<RayPacket> newRays = new List<RayPacket>();
            filter.ProcessFilter(ray.direction, ray.color, hit.point, ray.depth, ref newRays);
            foreach (var newRay in newRays) queue.Enqueue(newRay);
            return true;
        }

        LightReceiver receiver = hit.collider.GetComponent<LightReceiver>();
        if (receiver != null)
        {
            receiver.ReceiveLight(new Color(ray.color.x, ray.color.y, ray.color.z, 1f));
            return true;
        }

        return false;
    }
}