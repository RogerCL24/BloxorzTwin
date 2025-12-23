using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    private Canvas canvas;
    private Image overlay;

    private void Awake()
    {
        // Ensure single overlay per scene
        canvas = GetComponentInChildren<Canvas>();
        overlay = GetComponentInChildren<Image>();

        if (canvas == null || overlay == null)
        {
            GameObject cObj = new GameObject("FadeCanvas");
            cObj.transform.SetParent(transform, false);
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            cObj.AddComponent<CanvasScaler>();
            cObj.AddComponent<GraphicRaycaster>();

            GameObject imgObj = new GameObject("Overlay");
            imgObj.transform.SetParent(cObj.transform, false);
            overlay = imgObj.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0);
            overlay.raycastTarget = false; 
            RectTransform rt = overlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    public Coroutine FadeTo(float targetAlpha, float duration)
    {
        return StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = overlay.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            overlay.color = new Color(0, 0, 0, a);
            
            overlay.raycastTarget = a > 0.01f;
            
            yield return null;
        }
        overlay.color = new Color(0, 0, 0, targetAlpha);
        overlay.raycastTarget = targetAlpha > 0.01f;
    }
}
