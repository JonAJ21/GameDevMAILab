using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
public class Prism : MonoBehaviour
{
    [Header("Оптические свойства (n)")]
    [Range(1.0f, 3.0f)] public float n_Red = 1.50f;
    [Range(1.0f, 3.0f)] public float n_Yellow = 1.55f;
    [Range(1.0f, 3.0f)] public float n_Green = 1.60f;
    [Range(1.0f, 3.0f)] public float n_Cyan = 1.65f;
    [Range(1.0f, 3.0f)] public float n_Blue = 1.70f;
    [Range(1.0f, 3.0f)] public float n_Magenta = 1.75f;

    [Header("Ограничения")]
    public int maxInternalBounces = 5;

    private MeshCollider meshCollider;
    private bool originalQueriesHitBackfaces;

    void Awake()
    {
        originalQueriesHitBackfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            meshCollider.sharedMesh = mf.sharedMesh;
            meshCollider.convex = false;
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

    void OnDestroy() => Physics.queriesHitBackfaces = originalQueriesHitBackfaces;

    public void ProcessRefraction(Vector3 direction, Vector3 hitNormal, Vector3 color,
                                  Vector3 hitPoint, ref List<RayPacket> outputRays)
    {
        bool isWhite = color.x > 0.8f && color.y > 0.8f && color.z > 0.8f;

        if (isWhite)
        {
            // Расщепление белого света
            Vector3[] colors = { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0),
                                 new Vector3(0,1,1), new Vector3(0,0,1), new Vector3(1,0,1) };
            float[] nValues = { n_Red, n_Yellow, n_Green, n_Cyan, n_Blue, n_Magenta };

            for (int i = 0; i < 6; i++)
            {
                Vector3 refractedDir;
                // Вход (Воздух -> Стекло)
                if (TryRefract(direction, hitNormal, 1.0f, nValues[i], out refractedDir))
                    TraceInternalPath(hitPoint, refractedDir, colors[i], nValues[i], ref outputRays);
            }
        }
        else
        {
            float nGlass = GetNForColor(color);
            Vector3 refractedDir;
            // Вход (Воздух -> Стекло)
            if (TryRefract(direction, hitNormal, 1.0f, nGlass, out refractedDir))
                TraceInternalPath(hitPoint, refractedDir, color, nGlass, ref outputRays);
        }
    }

    // Ппошаговая трасировка внутри призмы
    void TraceInternalPath(Vector3 entryPoint, Vector3 startDir, Vector3 color, float nGlass, ref List<RayPacket> outputRays)
    {
        Vector3 currentPos = entryPoint + startDir * 0.02f; // Смещение от входа
        Vector3 currentDir = startDir;
        Vector3 lastSegmentStart = entryPoint; // Откуда рисуем линию
        int bounces = 0;

        while (bounces < maxInternalBounces)
        {
            // Ищем следующую грань внутри призмы
            Ray ray = new Ray(currentPos, currentDir);
            // Используем RaycastAll, чтобы отфильтровать случайные попадания в саму точку старта
            RaycastHit[] hits = Physics.RaycastAll(ray, 10f);

            RaycastHit? nextHit = null;
            float minDist = float.MaxValue;

            foreach (var h in hits)
            {
                // Берем ближайшее пересечение с ЭТИМ мешем, которое дальше 1 см (защита от себя)
                if (h.collider == meshCollider && h.distance > 0.01f && h.distance < minDist)
                {
                    minDist = h.distance;
                    nextHit = h;
                }
            }

            if (!nextHit.HasValue) break; // Луч ушел в пустоту (ошибка геометрии)

            Vector3 hitPoint = nextHit.Value.point;
            Vector3 hitNormal = nextHit.Value.normal; // Нормаль смотрит наружу

            // Проверяем физику на границе: Выход или Отражение (TIR)
            float cosI = -Vector3.Dot(hitNormal, currentDir);
            if (cosI < 0f) cosI = -cosI; // Коррекция нормали

            float eta = nGlass / 1.0f; // Из стекла в воздух
            float sinT2 = eta * eta * (1.0f - cosI * cosI);

            // Рисуем сегмент от предыдущей точки до текущего удара
            // Добавляем пакет isInternal=true, чтобы LightSource нарисовал линию, но не пересчитывал физику
            outputRays.Add(new RayPacket
            {
                origin = lastSegmentStart,
                direction = currentDir,
                color = color,
                depth = 1,
                endPoint = hitPoint,
                isInternal = true
            });

            // Если произошло полное внутреннее отражение (TIR)
            if (sinT2 > 1.0f)
            {
                currentDir = Vector3.Reflect(currentDir, hitNormal);
                currentPos = hitPoint + currentDir * 0.02f; // Смещение для следующего шага
                lastSegmentStart = hitPoint; // Следующая линия начнется отсюда
                bounces++;
                continue; // Луч остался внутри, ищем следующую грань
            }

            // 3. Если НЕ отражение, значит ВЫХОД (Преломление)
            Vector3 exitDir;

            if (TryRefract(currentDir, hitNormal, nGlass, 1.0f, out exitDir))
            {
                // Луч вышел. Добавляем финальный пакет, который вернет управление LightSource
                outputRays.Add(new RayPacket
                {
                    origin = hitPoint + exitDir * 0.02f,
                    direction = exitDir,
                    color = color,
                    depth = 1,
                    endPoint = Vector3.zero,
                    isInternal = false
                });
            }
            else
            {
                // На случай ошибки преломления, все равно выходим прямо
                outputRays.Add(new RayPacket
                {
                    origin = hitPoint + currentDir * 0.02f,
                    direction = currentDir,
                    color = color,
                    depth = 1,
                    endPoint = Vector3.zero,
                    isInternal = false
                });
            }

            return; // Выход из призмы завершен
        }

        // Если луч застрял или превысил лимит отскоков
        Vector3 fallbackEnd = currentPos + currentDir * 0.5f;
        outputRays.Add(new RayPacket { origin = lastSegmentStart, direction = currentDir, color = color, depth = 1, endPoint = fallbackEnd, isInternal = true });
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

        //  Определяем направление нормали относительно луча
        // Если Dot > 0, значит Нормаль и Луч смотрят в одну сторону (удар изнутри)
        float dot = Vector3.Dot(N, I);

        // Приводим нормаль к стандартному виду
        Vector3 normal = N;
        if (dot > 0f)
        {
            normal = -N; // Разворачиваем нормаль
            dot = -dot;  // Корректируем скалярное произведение
        }

        // Считаем угол падения (cosI)
        // normal уже смотрит навстречу I, поэтому Dot(-I, normal) будет положительным
        float cosI = -Vector3.Dot(normal, I);

        // Формула Снеллиуса
        float sinT2 = eta * eta * (1.0f - cosI * cosI);

        // Проверка на полное внутреннее отражение (Зеркало)
        if (sinT2 > 1.0f)
            return false; // Луч отразится

        float cosT = Mathf.Sqrt(1.0f - sinT2);

        // Финальный расчет вектора преломления
        T = (eta * I + (eta * cosI - cosT) * normal).normalized;
        return true;
    }
}