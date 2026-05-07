using UnityEngine;
using System.Collections.Generic;

public class LightSource : MonoBehaviour
{
    [Header("Настройки луча")]
    public Color rayColor = Color.white;
    public float maxDistance = 30f;
    public float lineWidth = 0.1f;

    private List<GameObject> activeSegments = new List<GameObject>();
    private Color currentBeamColor;
    private Renderer indicatorRenderer;
    private Shader segmentShader;

    void Awake()
    {
        segmentShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (segmentShader == null) segmentShader = Shader.Find("Unlit/Color");

     
        GameObject ind = new GameObject("ColorIndicator");
        ind.transform.SetParent(transform, false);
        ind.transform.localScale = Vector3.one * 0.3f;
        MeshFilter mf = ind.AddComponent<MeshFilter>();
        mf.mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;
        indicatorRenderer = ind.AddComponent<MeshRenderer>();
        indicatorRenderer.sharedMaterial = new Material(segmentShader);
    }

    void Update()
    {
        TraceRay();
    }

    void TraceRay()
    {
       
        foreach (var seg in activeSegments) Destroy(seg);
        activeSegments.Clear();

        currentBeamColor = rayColor;
        Vector3 origin = transform.position + transform.forward * 0.15f;
        Vector3 dir = transform.forward;
        float distLeft = maxDistance;
        Vector3 prevPoint = origin;

        int steps = 0;
        Collider lastHit = null;
        int maxSteps = 100; 

        while (distLeft > 0.01f && steps < maxSteps)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, distLeft))
            {
            
                if (hit.collider == lastHit) { origin = hit.point + dir * 0.05f; continue; }
                lastHit = hit.collider;

                
                CreateSegment(prevPoint, hit.point, currentBeamColor);
                prevPoint = hit.point;

         
                ColorFilter filter = hit.collider.GetComponent<ColorFilter>();
                if (filter != null)
                {
                    Vector3 t = filter.GetTransmission();
                    currentBeamColor = new Color(
                        currentBeamColor.r * t.x,
                        currentBeamColor.g * t.y,
                        currentBeamColor.b * t.z,
                        1f
                    );
                    origin = hit.point + dir * 0.05f;
                    distLeft -= hit.distance;
                    steps++;
                    continue;
                }

         
                LightReceiver rec = hit.collider.GetComponent<LightReceiver>();
                if (rec != null) rec.ReceiveLight(currentBeamColor);
                break;
            }
            else
            {
      
                CreateSegment(prevPoint, origin + dir * distLeft, currentBeamColor);
                break;
            }
        }


        if (indicatorRenderer != null)
            indicatorRenderer.sharedMaterial.color = currentBeamColor;
    }

    void CreateSegment(Vector3 start, Vector3 end, Color col)
    {
        GameObject segObj = new GameObject($"BeamSegment_{activeSegments.Count}");
        segObj.transform.SetParent(transform, false);
        activeSegments.Add(segObj);

        LineRenderer lr = segObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;

        
        lr.startColor = col;
        lr.endColor = col;

        
        Material mat = new Material(segmentShader);
        mat.color = col;
        mat.enableInstancing = false;
        mat.hideFlags = HideFlags.DontSave;
        lr.material = mat;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}