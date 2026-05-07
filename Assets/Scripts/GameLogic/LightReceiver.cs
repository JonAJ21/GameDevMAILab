using UnityEngine;

public class LightReceiver : MonoBehaviour
{
    [Header("Настройки")]
    public Color targetColor = Color.white;
    [Range(0f, 1f)] public float tolerance = 0.15f;
    public Color idleColor = Color.gray; 

    private MeshRenderer myRenderer;
    private bool isActivated = false;

    void Awake()
    {
        myRenderer = GetComponent<MeshRenderer>();
        if (myRenderer != null)
            myRenderer.material.color = idleColor;
    }

 
    public void ReceiveLight(Color incomingColor)
    {
      
        myRenderer.material.color = incomingColor;

       
        bool rMatch = Mathf.Abs(incomingColor.r - targetColor.r) <= tolerance;
        bool gMatch = Mathf.Abs(incomingColor.g - targetColor.g) <= tolerance;
        bool bMatch = Mathf.Abs(incomingColor.b - targetColor.b) <= tolerance;

        if (rMatch && gMatch && bMatch && !isActivated)
        {
            isActivated = true;
        }
    }

 
    public void LostLight()
    {
        isActivated = false;
        myRenderer.material.color = idleColor;
    }
}