using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveDisplay : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";

    private Text moveText;
    private Button menuButton;
    private bool menuLoading;
    private bool isInMenu = false;
    private ScreenFader screenFader;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.layer = 5; // UI layer
        SetupUI();
        
        screenFader = FindFirstObjectByType<ScreenFader>();
        if (screenFader == null)
        {
            GameObject fadeObj = new GameObject("ScreenFader");
            screenFader = fadeObj.AddComponent<ScreenFader>();
            DontDestroyOnLoad(fadeObj);
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == menuSceneName)
        {
            isInMenu = true;
            menuLoading = false;
            if (screenFader != null)
            {
                screenFader.FadeTo(0f, 0.5f);
            }
            
            gameObject.SetActive(false);
        }
        else
        {
            isInMenu = false;
            menuLoading = false;
            if (screenFader != null)
            {
                screenFader.FadeTo(0f, 0.5f);
            }
            
            gameObject.SetActive(true);
        }
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
            if (existing.GetComponent<StandaloneInputModule>() == null)
            {
                existing.gameObject.AddComponent<StandaloneInputModule>();
            }
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void SetupMoveText()
    {
        Transform textTransform = transform.Find("MoveText");
        if (textTransform == null)
        {
            GameObject textObj = new GameObject("MoveText");
            textObj.transform.SetParent(transform, false);
            if (textObj.GetComponent<RectTransform>() == null)
                textObj.AddComponent<RectTransform>();
            if (textObj.GetComponent<CanvasRenderer>() == null)
                textObj.AddComponent<CanvasRenderer>();
            moveText = textObj.AddComponent<Text>();
        }
        else
        {
            moveText = textTransform.GetComponent<Text>();
            if (moveText == null)
            {
                moveText = textTransform.gameObject.AddComponent<Text>();
            }
            if (textTransform.GetComponent<CanvasRenderer>() == null)
                textTransform.gameObject.AddComponent<CanvasRenderer>();
        }

        moveText.raycastTarget = false; 

        moveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        moveText.fontSize = 22;
        moveText.alignment = TextAnchor.UpperLeft;
        RectTransform rt = moveText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(14f, -14f);
        rt.sizeDelta = new Vector2(200f, 60f); 
        moveText.color = Color.white;
    }

    private void SetupMenuButton()
    {
        Transform buttonTransform = transform.Find("MenuButton");
        if (buttonTransform == null)
        {
            GameObject buttonObj = new GameObject("MenuButton");
            buttonObj.transform.SetParent(transform, false);
            if (buttonObj.GetComponent<RectTransform>() == null)
                buttonObj.AddComponent<RectTransform>();
            if (buttonObj.GetComponent<CanvasRenderer>() == null)
                buttonObj.AddComponent<CanvasRenderer>();
            Image img = buttonObj.AddComponent<Image>();
            img.color = Color.white; 
            menuButton = buttonObj.AddComponent<Button>();
            menuButton.targetGraphic = img;
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
                graphic.color = Color.white; 
                menuButton.targetGraphic = graphic;
            }
        }

        RectTransform rt = menuButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(170f, 52f);

        // Ensure button is on UI layer for raycasts
        menuButton.gameObject.layer = 5;
        
        Image buttonImage = menuButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.raycastTarget = true;
        }
        
        ColorBlock colors = menuButton.colors;
        colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        menuButton.colors = colors;

        Transform labelTransform = menuButton.transform.Find("Text");
        Text label;
        if (labelTransform == null)
        {
            GameObject labelObj = new GameObject("Text");
            labelObj.transform.SetParent(menuButton.transform, false);
            if (labelObj.GetComponent<RectTransform>() == null)
                labelObj.AddComponent<RectTransform>();
            if (labelObj.GetComponent<CanvasRenderer>() == null)
                labelObj.AddComponent<CanvasRenderer>();
            label = labelObj.AddComponent<Text>();
        }
        else
        {
            label = labelTransform.GetComponent<Text>();
            if (label == null)
            {
                label = labelTransform.gameObject.AddComponent<Text>();
            }
            if (labelTransform.GetComponent<CanvasRenderer>() == null)
                labelTransform.gameObject.AddComponent<CanvasRenderer>();
        }

        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        
        StartCoroutine(FadeAndLoadMenu());
    }
    
    private IEnumerator FadeAndLoadMenu()
    {
        if (screenFader != null)
        {
            yield return screenFader.FadeTo(1f, 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
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
        
        if (Input.GetKeyDown(KeyCode.Escape) && !menuLoading && !isInMenu)
        {
            HandleMenuButton();
        }
    }
}
