using System;
using UnityEngine;
using UnityEngine.UI;

public enum AppScreenState
{
    Home,
    AR,
    PlantDetails
}

/// <summary>
/// Minimal Application Navigation Shell (Phase 2).
/// Manages navigation states: Home -> AR -> Plant Details -> AR.
/// Keeps ARSession, camera feed, spatial anchors, persistent cards, and ML pipelines intact.
/// </summary>
public class AppNavigationShell : MonoBehaviour
{
    public static AppNavigationShell Instance { get; private set; }

    [Header("Panels")]
    public GameObject homePanel;
    public GameObject plantDetailsPanel;
    public GameObject arOverlayPanel;

    [Header("Plant Details Placeholder References")]
    public Text detailsHeaderTitleText;
    public Text detailsSpeciesIdText;
    public Text detailsCommonNameText;
    public Text detailsScientificNameText;
    public Button detailsBackButton;

    [Header("Home Placeholder References")]
    public Button homeScanButton;

    [Header("AR Screen References")]
    public Button arHomeButton;
    public GameObject arExploreBanner;
    public Text arExploreBannerText;
    public Button arExploreBannerButton;

    [Header("Current State")]
    [SerializeField] private AppScreenState _currentScreen = AppScreenState.Home;
    [SerializeField] private string _currentSpeciesId = "";

    public AppScreenState CurrentScreen => _currentScreen;
    public string CurrentSpeciesId => _currentSpeciesId;

    public event Action<AppScreenState> OnScreenChanged;
    public event Action<string> OnPlantDetailsOpened;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<AppNavigationShell>();
            if (existing == null)
            {
                var go = new GameObject("AppNavigationShell");
                existing = go.AddComponent<AppNavigationShell>();
                Debug.Log("[AppNavigationShell] Bootstrapped navigation shell into active scene.");
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure canvas and minimal placeholder panels exist
        EnsureUIStructure();
    }

    void Start()
    {
        // Subscribe to card explore events if triggered from ARSpeciesCard
        ARSpeciesCard.OnExploreRequested += HandleCardExploreRequested;

        // Start in Home state as mandated by Phase 2 flow
        SetScreen(AppScreenState.Home);
        Debug.Log("[AppNavigationShell] Initialized. Current screen: HOME");
    }

    void OnDestroy()
    {
        ARSpeciesCard.OnExploreRequested -= HandleCardExploreRequested;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleCardExploreRequested(string speciesId)
    {
        Debug.Log($"[AppNavigationShell] Received OnExploreRequested for species: {speciesId}");
        NavigateToPlantDetails(speciesId);
    }

    /// <summary>
    /// Navigates to the Home screen overlay.
    /// </summary>
    public void NavigateToHome()
    {
        Debug.Log("[AppNavigationShell] Navigating to HOME");
        SetScreen(AppScreenState.Home);
    }

    /// <summary>
    /// Navigates from Home to the live AR experience.
    /// Does NOT create or re-create AR sessions or cameras.
    /// </summary>
    public void NavigateToAR()
    {
        Debug.Log("[AppNavigationShell] Navigating to AR EXPERIENCE");
        SetScreen(AppScreenState.AR);
    }

    /// <summary>
    /// Navigates from AR to Plant Details placeholder, receiving the active species ID context.
    /// Keeps AR session, camera, anchors, and cards completely alive.
    /// </summary>
    public void NavigateToPlantDetails(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId))
        {
            speciesId = "unknown_plant";
        }
        _currentSpeciesId = speciesId;
        Debug.Log($"[AppNavigationShell] Navigating to PLANT DETAILS with speciesId: '{_currentSpeciesId}'");

        UpdatePlantDetailsContent(_currentSpeciesId);
        SetScreen(AppScreenState.PlantDetails);

        OnPlantDetailsOpened?.Invoke(_currentSpeciesId);
    }

    /// <summary>
    /// Navigates from Plant Details back to the same ongoing AR experience.
    /// Preserves existing AR state, anchors, cards, and session without duplication or reset.
    /// </summary>
    public void NavigateBackToAR()
    {
        Debug.Log("[AppNavigationShell] Navigating BACK TO SAME AR EXPERIENCE");
        SetScreen(AppScreenState.AR);
    }

    private void SetScreen(AppScreenState screen)
    {
        _currentScreen = screen;

        if (homePanel != null)
        {
            homePanel.SetActive(screen == AppScreenState.Home);
        }

        if (plantDetailsPanel != null)
        {
            plantDetailsPanel.SetActive(screen == AppScreenState.PlantDetails);
        }

        if (arOverlayPanel != null)
        {
            arOverlayPanel.SetActive(screen == AppScreenState.AR);
        }

        OnScreenChanged?.Invoke(_currentScreen);
    }

    /// <summary>
    /// Updates the minimal Plant Details placeholder text with the active species context.
    /// </summary>
    private void UpdatePlantDetailsContent(string speciesId)
    {
        string commonName = "Unknown Species";
        string scientificName = "Plantae";

        if (MockSpeciesDatabase.TryGetSpeciesData(speciesId, out SpeciesData data))
        {
            if (!string.IsNullOrEmpty(data.commonName) && data.commonName != "Data Unavailable")
            {
                commonName = data.commonName;
            }
            if (!string.IsNullOrEmpty(data.scientificName) && data.scientificName != "Data Unavailable")
            {
                scientificName = data.scientificName;
            }
        }

        if (detailsHeaderTitleText != null)
        {
            detailsHeaderTitleText.text = "PLANT DETAILS";
        }
        if (detailsSpeciesIdText != null)
        {
            detailsSpeciesIdText.text = $"Species ID:\n{speciesId}";
        }
        if (detailsCommonNameText != null)
        {
            detailsCommonNameText.text = $"Common Name: {commonName}";
        }
        if (detailsScientificNameText != null)
        {
            detailsScientificNameText.text = $"Scientific Name: {scientificName}";
        }
    }

    /// <summary>
    /// Called when an ARSpeciesCard is anchored or active in the spatial scene.
    /// Activates the screen-space Explore banner for high accessibility.
    /// </summary>
    public void NotifyActiveCardAvailable(string speciesId)
    {
        _currentSpeciesId = speciesId;
        if (arExploreBanner != null)
        {
            arExploreBanner.SetActive(true);
            if (arExploreBannerText != null)
            {
                arExploreBannerText.text = $"Explore: {speciesId}";
            }
        }
    }

    /// <summary>
    /// Dismisses the screen-space Explore banner when card is dismissed.
    /// </summary>
    public void DismissActiveCardBanner()
    {
        if (arExploreBanner != null)
        {
            arExploreBanner.SetActive(false);
        }
    }

    /// <summary>
    /// Builds or binds the minimal UI panels on the scene's screen-space Canvas.
    /// </summary>
    private void EnsureUIStructure()
    {
        // 1. Locate root Canvas in scene
        Canvas targetCanvas = null;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null && canvases.Length > 0)
        {
            targetCanvas = canvases[0];
        }

        if (targetCanvas == null)
        {
            var canvasGo = new GameObject("NavigationCanvas");
            targetCanvas = canvasGo.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        Transform canvasTransform = targetCanvas.transform;
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // 2. Build Home Panel if not assigned
        if (homePanel == null)
        {
            homePanel = CreatePanel(canvasTransform, "HomePanel", new Color(0.05f, 0.12f, 0.08f, 0.95f));

            // Home Container
            GameObject homeContainer = CreateContainer(homePanel.transform, "HomeCard", new Vector2(500, 450), new Color(0.08f, 0.20f, 0.13f, 0.98f));

            CreateLabel(homeContainer.transform, "Title", "groot", defaultFont, 42, FontStyle.Bold, Color.white, new Vector2(0, 130), new Vector2(450, 60));
            CreateLabel(homeContainer.transform, "Subtitle", "Botanical Field Guide & AR Perception", defaultFont, 18, FontStyle.Normal, new Color(0.7f, 0.9f, 0.75f), new Vector2(0, 75), new Vector2(450, 40));
            CreateLabel(homeContainer.transform, "Hint", "Point device at foliage to detect and identify botanical species in real time.", defaultFont, 15, FontStyle.Italic, new Color(0.6f, 0.75f, 0.65f), new Vector2(0, 10), new Vector2(420, 50));

            homeScanButton = CreateButton(homeContainer.transform, "ScanButton", "SCAN (ENTER AR)", defaultFont, 20, new Color(0.0f, 0.78f, 0.32f), Color.black, new Vector2(0, -90), new Vector2(360, 60));
            homeScanButton.onClick.AddListener(NavigateToAR);
        }

        // 3. Build Plant Details Placeholder Panel if not assigned
        if (plantDetailsPanel == null)
        {
            plantDetailsPanel = CreatePanel(canvasTransform, "PlantDetailsPanel", new Color(0.04f, 0.08f, 0.05f, 0.97f));

            // Details Container
            GameObject detailsContainer = CreateContainer(plantDetailsPanel.transform, "DetailsCard", new Vector2(520, 520), new Color(0.10f, 0.18f, 0.13f, 0.98f));

            detailsHeaderTitleText = CreateLabel(detailsContainer.transform, "HeaderTitle", "PLANT DETAILS", defaultFont, 34, FontStyle.Bold, Color.white, new Vector2(0, 190), new Vector2(460, 50));

            detailsSpeciesIdText = CreateLabel(detailsContainer.transform, "SpeciesIdText", "Species ID:\n--", defaultFont, 22, FontStyle.Bold, new Color(0.2f, 0.9f, 0.5f), new Vector2(0, 105), new Vector2(460, 70));

            detailsCommonNameText = CreateLabel(detailsContainer.transform, "CommonNameText", "Common Name: --", defaultFont, 18, FontStyle.Normal, Color.white, new Vector2(0, 30), new Vector2(460, 40));

            detailsScientificNameText = CreateLabel(detailsContainer.transform, "ScientificNameText", "Scientific Name: --", defaultFont, 16, FontStyle.Italic, new Color(0.75f, 0.85f, 0.80f), new Vector2(0, -15), new Vector2(460, 40));

            detailsBackButton = CreateButton(detailsContainer.transform, "BackButton", "[Back to AR]", defaultFont, 20, new Color(0.0f, 0.78f, 0.32f), Color.black, new Vector2(0, -140), new Vector2(360, 60));
            detailsBackButton.onClick.AddListener(NavigateBackToAR);
        }

        // 4. Build AR Overlay Panel if not assigned
        if (arOverlayPanel == null)
        {
            arOverlayPanel = new GameObject("AROverlayPanel", typeof(RectTransform));
            arOverlayPanel.transform.SetParent(canvasTransform, false);
            RectTransform rt = arOverlayPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            // Home Button in AR (Top-Left)
            arHomeButton = CreateButton(arOverlayPanel.transform, "ARHomeButton", "< Home", defaultFont, 16, new Color(0.12f, 0.25f, 0.16f, 0.85f), Color.white, new Vector2(70, -40), new Vector2(110, 45));
            RectTransform homeRt = arHomeButton.GetComponent<RectTransform>();
            homeRt.anchorMin = new Vector2(0, 1);
            homeRt.anchorMax = new Vector2(0, 1);
            homeRt.anchoredPosition = new Vector2(75, -45);
            arHomeButton.onClick.AddListener(NavigateToHome);

            // Explore Banner at Bottom of AR Screen
            arExploreBanner = CreateContainer(arOverlayPanel.transform, "ARExploreBanner", new Vector2(460, 70), new Color(0.05f, 0.22f, 0.14f, 0.90f));
            RectTransform bannerRt = arExploreBanner.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 0);
            bannerRt.anchorMax = new Vector2(0.5f, 0);
            bannerRt.anchoredPosition = new Vector2(0, 90);

            arExploreBannerText = CreateLabel(arExploreBanner.transform, "BannerText", "Explore Plant Details", defaultFont, 16, FontStyle.Bold, Color.white, new Vector2(-70, 0), new Vector2(280, 40));

            arExploreBannerButton = CreateButton(arExploreBanner.transform, "BannerButton", "EXPLORE", defaultFont, 16, new Color(0.0f, 0.78f, 0.32f), Color.black, new Vector2(145, 0), new Vector2(130, 44));
            arExploreBannerButton.onClick.AddListener(() =>
            {
                NavigateToPlantDetails(_currentSpeciesId);
            });

            arExploreBanner.SetActive(false);
        }
    }

    // --- Procedural UI Creation Helpers ---

    private GameObject CreatePanel(Transform parent, string name, Color bgColor)
    {
        var panelGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(parent, false);

        RectTransform rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = panelGo.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        return panelGo;
    }

    private GameObject CreateContainer(Transform parent, string name, Vector2 size, Color bgColor)
    {
        var cardGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardGo.transform.SetParent(parent, false);

        RectTransform rt = cardGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = cardGo.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        return cardGo;
    }

    private Text CreateLabel(Transform parent, string name, string text, Font font, int fontSize, FontStyle style, Color color, Vector2 position, Vector2 size)
    {
        var labelGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGo.transform.SetParent(parent, false);

        RectTransform rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Text txt = labelGo.GetComponent<Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;

        return txt;
    }

    private Button CreateButton(Transform parent, string name, string labelText, Font font, int fontSize, Color btnColor, Color textColor, Vector2 position, Vector2 size)
    {
        var btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = btnGo.GetComponent<Image>();
        img.color = btnColor;
        img.raycastTarget = true;

        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock colors = btn.colors;
        colors.normalColor = btnColor;
        colors.highlightedColor = btnColor * 1.15f;
        colors.pressedColor = btnColor * 0.85f;
        btn.colors = colors;

        CreateLabel(btnGo.transform, "BtnText", labelText, font, fontSize, FontStyle.Bold, textColor, Vector2.zero, size);

        return btn;
    }
}
