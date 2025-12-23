using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MoveDisplay : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";

    private Text moveText;
    private Button menuButton;
    private bool menuLoading;

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
        canvas.enabled = true;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        SetupMoveText();
        SetupMenuButton();
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = EventSystem.current;
        if (existing != null)
        {
            // Ensure it has an InputSystem module if the project uses the new Input System.
            if (existing.GetComponent<InputSystemUIInputModule>() == null && InputSystem.settings != null)
            {
                var legacy = existing.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Destroy(legacy);
                }
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        if (InputSystem.settings != null)
        {
            es.AddComponent<InputSystemUIInputModule>();
        }
        else
        {
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private void SetupMoveText()
    {
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
            {
                moveText = textTransform.gameObject.AddComponent<Text>();
            }
        }

        moveText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        moveText.fontSize = 22;
        moveText.alignment = TextAnchor.UpperLeft;
        RectTransform rt = moveText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(14f, -14f);
        rt.sizeDelta = new Vector2(360f, 60f);
        moveText.color = Color.white;
    }

    private void SetupMenuButton()
    {
        Transform buttonTransform = transform.Find("MenuButton");
        if (buttonTransform == null)
        {
            GameObject buttonObj = new GameObject("MenuButton");
            buttonObj.transform.SetParent(transform, false);
            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.78f);
            menuButton = buttonObj.AddComponent<Button>();
        }
        else
        {
            menuButton = buttonTransform.GetComponent<Button>();
            if (menuButton == null)
            {
                menuButton = buttonTransform.gameObject.AddComponent<Button>();
            }

            if (menuButton.targetGraphic == null)
            {
                Image graphic = buttonTransform.GetComponent<Image>();
                if (graphic == null)
                {
                    graphic = buttonTransform.gameObject.AddComponent<Image>();
                }
                graphic.color = new Color(0.1f, 0.1f, 0.1f, 0.78f);
                menuButton.targetGraphic = graphic;
            }
        }

        RectTransform rt = menuButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(170f, 52f);

        Transform labelTransform = menuButton.transform.Find("Text");
        Text label;
        if (labelTransform == null)
        {
            GameObject labelObj = new GameObject("Text");
            labelObj.transform.SetParent(menuButton.transform, false);
            label = labelObj.AddComponent<Text>();
        }
        else
        {
            label = labelTransform.GetComponent<Text>();
            if (label == null)
            {
                label = labelTransform.gameObject.AddComponent<Text>();
            }
        }

        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 20;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;

        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        label.text = "Volver al menu";

        menuButton.onClick.RemoveAllListeners();
        menuButton.onClick.AddListener(HandleMenuButton);
    }

    private void HandleMenuButton()
    {
        if (menuLoading)
        {
            return;
        }

        menuLoading = true;
        if (menuButton != null)
        {
            menuButton.interactable = false;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    private void Update()
    {
        if (moveText == null)
        {
            return;
        }

        int moves = MoveTracker.Instance != null ? MoveTracker.Instance.DisplayTotalMoves : 0;
        moveText.text = $"Movimientos: {moves}";
    }
}
