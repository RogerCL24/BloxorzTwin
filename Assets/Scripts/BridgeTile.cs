using UnityEngine;
using System.Collections;

public class BridgeTile : MonoBehaviour
{
    private bool isActive = false;
    private Vector3 targetPosition;
    private Vector3 startPosition; // For falling animation

    void Awake()
    {
        targetPosition = transform.position;
        startPosition = targetPosition + Vector3.up * 10f; // Fall from 10 units up
    }

    public void SetState(bool active, bool immediate = false)
    {
        isActive = active;
        
        // Enable/Disable renderers and colliders
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = active;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = active;
        
        if (active)
        {
            if (immediate)
            {
                transform.position = targetPosition;
            }
            else
            {
                StartCoroutine(FallAnimation());
            }
        }
    }
    
    public void Toggle()
    {
        SetState(!isActive);
    }

    private IEnumerator FallAnimation()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        transform.position = startPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Simple ease out
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        transform.position = targetPosition;
    }
}
