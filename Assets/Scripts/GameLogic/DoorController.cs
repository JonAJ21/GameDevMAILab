using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Настройки двери")]
    public Vector3 openOffset = new Vector3(0, 3, 0);
    public float speed = 3f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;
    private Coroutine moveCoroutine;

    void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
    }

    public void Open()
    {
        if (!isOpen)
        {
            isOpen = true;
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveToPosition(openPosition));
        }
    }

    public void Close()
    {
        if (isOpen)
        {
            isOpen = false;
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveToPosition(closedPosition));
        }
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, target);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPos, target, t);
            yield return null;
        }

        transform.position = target;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Vector3 targetPos = transform.position + openOffset;
        Gizmos.DrawLine(transform.position, targetPos);
        Gizmos.DrawWireCube(targetPos, transform.localScale);
    }
}