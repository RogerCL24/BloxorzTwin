using UnityEngine;
using UnityEngine.UI;

public class MoveDisplay : MonoBehaviour
{
    private Text moveText;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SetupUI();
    }

    private void SetupUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
        }

        if (GetComponent<CanvasScaler>() == null)
        {
            gameObject.AddComponent<CanvasScaler>();
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        Transform textTransform = transform.Find("MoveText");
        if (textTransform == null)
        {
            GameObject textObj = new GameObject("MoveText");
            textObj.transform.SetParent(transform, false);
            moveText = textObj.AddComponent<Text>();
        }
        else
        {
            moveText = textTransform.GetComponent<Text>();
            if (moveText == null)
                moveText = textTransform.gameObject.AddComponent<Text>();
        }

        moveText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        moveText.fontSize = 20;
        moveText.alignment = TextAnchor.UpperLeft;
        RectTransform rt = moveText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);
        rt.sizeDelta = new Vector2(300f, 50f);
        moveText.color = Color.white;
    }

    private void Update()
    {
        if (moveText == null) return;
        if (MoveTracker.Instance != null)
            moveText.text = $"Moviments guanyats: {MoveTracker.Instance.TotalCompletedMoves}";
        else
            moveText.text = "Moviments guanyats: 0";
    }
}
