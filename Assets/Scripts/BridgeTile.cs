using UnityEngine;
using System.Collections;

public class BridgeTile : MonoBehaviour
{
    private bool isActive = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private Coroutine currentAnim;

    void Awake()
    {
        targetPosition = transform.position;
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        Vector3 found = Vector3.zero;
        float bestDist = float.MaxValue;
        foreach (var c in hits)
        {
            if (c == null) continue;
            // skip colliders belonging to this bridge
            if (c.transform.IsChildOf(transform) || c.transform == transform) continue;
            float d = Vector3.Distance(c.transform.position, transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                found = c.transform.position;
            }
        }
        if (bestDist < float.MaxValue)
        {
            startPosition = new Vector3(found.x, targetPosition.y - 0.5f, found.z);
        }
        else
        {
            startPosition = targetPosition + Vector3.down * 0.5f;
        }
    }

    public void SetState(bool active, bool immediate = false)
    {
        isActive = active;

        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            currentAnim = null;
        }

        var renderers = GetComponentsInChildren<Renderer>();
        var colliders = GetComponentsInChildren<Collider>();

        if (active)
        {
            foreach (var r in renderers) r.enabled = true;
            foreach (var c in colliders) c.enabled = false;

            if (immediate)
            {
                transform.position = targetPosition;
                foreach (var c in colliders) c.enabled = true;
            }
            else
            {
                transform.position = startPosition;
                currentAnim = StartCoroutine(DeployAnimation(renderers, colliders));
            }
        }
        else
        {
            foreach (var c in colliders) c.enabled = false;
            if (immediate)
            {
                foreach (var r in renderers) r.enabled = false;
                transform.position = targetPosition;
            }
            else
            {
                currentAnim = StartCoroutine(RetractAnimation(renderers, colliders));
            }
        }
    }
    
    public void Toggle()
    {
        SetState(!isActive);
    }

    private IEnumerator DeployAnimation(Renderer[] renderers, Collider[] colliders)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // ease out
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        foreach (var c in colliders) c.enabled = true;
        currentAnim = null;
    }

    private IEnumerator RetractAnimation(Renderer[] renderers, Collider[] colliders)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 from = transform.position;
        Vector3 to = startPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); // ease in
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
        foreach (var r in renderers) r.enabled = false;
        currentAnim = null;
    }
}
