using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DebugOverlay : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField][Range(0f, 1f)] private float backgroundOpacity = 0.2f;
    [SerializeField] private Color textColor = Color.green;
    [SerializeField] private int fontSize = 25;
    [SerializeField] private Vector2 padding = new(10, 10);
    [SerializeField] private Vector2 resolution = new(1920, 1080);
    [SerializeField] private int panelWidth = 400;
    [SerializeField] private TMP_FontAsset font;

    [Header("Toggle Settings")]
    [SerializeField] private bool isVisible = true;
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showPerformance = true;

    private Canvas debugCanvas;
    private Image backgroundPanel;
    private TextMeshProUGUI debugText;
    private System.Text.StringBuilder stringBuilder = new(256);
    private readonly Dictionary<string, string> debugValues = new();

    private readonly object lockObject = new object(); // Lock for thread safety

    // FPS calculation variables
    private float fpsUpdateTimer;
    private int frameCount;
    private float fps;

    // Performance tracking variables
    private float performanceUpdateTimer;
    private float frameTime;
    private long memoryUsage;
    private int gcCollectionCount;

    // NEW INPUT SYSTEM
#if ENABLE_INPUT_SYSTEM 
    [SerializeField] private Key toggleKey = Key.Backquote;
#else
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; 
#endif

    private static DebugOverlay _instance;
    public static DebugOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance
                _instance = FindFirstObjectByType<DebugOverlay>();

                // If no instance exists, create one
                if (_instance == null)
                {
                    GameObject debuggerObj = new("VisualDebugger");
                    _instance = debuggerObj.AddComponent<DebugOverlay>();
                    DontDestroyOnLoad(debuggerObj);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugCanvas == null)
        {
            InitializeUI();
        }
    }

    private void InitializeUI()
    {   
        GameObject canvasObj = new("DebugCanvas"); // Canvas
        canvasObj.transform.SetParent(transform);

        debugCanvas = canvasObj.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        debugCanvas.sortingOrder = 1000; // Ensure it's on top of everything

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = resolution;

        GameObject panelObj = new("DebugPanel"); // Background panel
        panelObj.transform.SetParent(canvasObj.transform, false);

        backgroundPanel = panelObj.AddComponent<Image>();
        backgroundPanel.color = new Color(0, 0, 0, backgroundOpacity);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new(0, 1);
        panelRect.anchorMax = new(0, 1);
        panelRect.pivot = new(0, 1);
        panelRect.anchoredPosition = new(padding.x, -padding.y);
        panelRect.sizeDelta = new(400, 200);

        GameObject textObj = new("DebugText"); // Debug text
        textObj.transform.SetParent(panelObj.transform, false);

        debugText = textObj.AddComponent<TextMeshProUGUI>();
        debugText.color = textColor;
        debugText.fontSize = fontSize;
        debugText.alignment = TextAlignmentOptions.TopLeft;
        debugText.textWrappingMode = TextWrappingModes.Normal;
        debugText.font = font;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new(-15, -15);

        if (showFPS)
            SetDebugValue("FPS", "0");
            
        if(showPerformance)
        {
            SetDebugValue("Frame Time", "0.0ms");
            SetDebugValue("Memory", "0MB");
            SetDebugValue("GC Collections", "0");
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            ToggleVisibility();
#else
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleVisibility();
        }
#endif

        if(showFPS)
            CalculateAndUpdateFPS();
            
        if(showPerformance)
            CalculateAndUpdatePerformance();
            
        UpdateDebugDisplay();
    }

    private void ToggleVisibility()
    {
        isVisible = !isVisible;

        if (debugCanvas != null)
            debugCanvas.gameObject.SetActive(isVisible); 
    }

    private void CalculateAndUpdateFPS()
    {
        frameCount++;
        fpsUpdateTimer += Time.unscaledDeltaTime;

        if (fpsUpdateTimer >= updateInterval)
        {
            fps = frameCount / fpsUpdateTimer;
            frameCount = 0;
            fpsUpdateTimer = 0;

            SetDebugValue("FPS", ((int)fps).ToString());
        }
    }

    private void CalculateAndUpdatePerformance()
    {
        performanceUpdateTimer += Time.unscaledDeltaTime;
        frameTime = Time.unscaledDeltaTime * 1000f;

        if (performanceUpdateTimer >= updateInterval)
        {
            memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024;
            
            gcCollectionCount = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            performanceUpdateTimer = 0;

            SetDebugValue("Frame Time", frameTime.ToString("F1") + "ms");
            SetDebugValue("Memory", memoryUsage.ToString() + "MB");
            SetDebugValue("GC Collections", gcCollectionCount.ToString());
        }
    }

    private void UpdateDebugDisplay()
    {
        if (debugText == null) return;

        stringBuilder.Clear();        
    
        foreach (KeyValuePair<string, string> pair in debugValues)
        {
            if (pair.Value == "SPACER")
                stringBuilder.AppendLine("");
            else 
                stringBuilder.AppendLine($"{pair.Key}: {pair.Value}");
        }

        debugText.text = stringBuilder.ToString();

        RectTransform panelRect = backgroundPanel.GetComponent<RectTransform>();
        float height = Mathf.Min(debugText.preferredHeight + 20, Screen.height * 0.7f);
        panelRect.sizeDelta = new(panelWidth, height);
    }


    public static void AddSpacer(int id)
    {
        Instance.SetDebugValueInternal($"Spacer_{id}", "SPACER");
    }

    public static void SetDebugValue(string key, object value)
    {
        Instance.SetDebugValueInternal(key, value);
    }

    private void SetDebugValueInternal(string key, object value)
    {
        lock (lockObject)
        {
            string valueText = value?.ToString() ?? "null";
            debugValues[key] = valueText;
        }
    }
}
