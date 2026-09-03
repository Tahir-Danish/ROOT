using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PostExploreController : MonoBehaviour
{
    private GameObject _overlayContainer;
    private SpeciesData _currentData;

    private SpeciesDetailView _detailView;
    private EcologyView _ecologyView;
    private ConservationView _conservationView;
    private BotanicalGuideView _guideView;

    private GameObject _detailPanel;
    private GameObject _ecologyPanel;
    private GameObject _conservationPanel;
    private GameObject _guidePanel;

    private void Awake()
    {
        CreateUI();
        _overlayContainer.SetActive(false);
    }

    private void OnEnable()
    {
        ARSpeciesCard.OnExploreRequested += HandleExploreRequested;
    }

    private void OnDisable()
    {
        ARSpeciesCard.OnExploreRequested -= HandleExploreRequested;
    }

    private void HandleExploreRequested(string speciesId)
    {
        if (MockSpeciesDatabase.TryGetSpeciesData(speciesId, out _currentData))
        {
            _overlayContainer.SetActive(true);
            
            // Populate views
            _detailView.Populate(_currentData);
            _ecologyView.Populate(_currentData);
            _conservationView.Populate(_currentData);
            _guideView.Populate(_currentData);

            ShowTab(_detailPanel);
        }
        else
        {
            Debug.LogWarning($"[PostExploreController] Could not find data for species ID {speciesId}");
        }
    }

    private void CloseUI()
    {
        _overlayContainer.SetActive(false);
    }

    private void ShowTab(GameObject panelToShow)
    {
        _detailPanel.SetActive(panelToShow == _detailPanel);
        _ecologyPanel.SetActive(panelToShow == _ecologyPanel);
        _conservationPanel.SetActive(panelToShow == _conservationPanel);
        _guidePanel.SetActive(panelToShow == _guidePanel);
    }

    private void CreateUI()
    {
        // 1. Canvas
        GameObject canvasObj = new GameObject("PostExploreCanvas");
        canvasObj.transform.SetParent(this.transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        _overlayContainer = canvasObj;

        // 2. Background (blocks raycasts to AR)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.15f, 0.1f, 1f); // Dark green background
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // 3. Header & Close Button
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(bgObj.transform, false);
        RectTransform hdrRt = headerObj.AddComponent<RectTransform>();
        hdrRt.anchorMin = new Vector2(0, 1);
        hdrRt.anchorMax = new Vector2(1, 1);
        hdrRt.pivot = new Vector2(0.5f, 1);
        hdrRt.anchoredPosition = Vector2.zero;
        hdrRt.sizeDelta = new Vector2(0, 120);
        Image hdrImg = headerObj.AddComponent<Image>();
        hdrImg.color = new Color(0.05f, 0.1f, 0.05f, 1f);

        Button closeBtn = CreateButton(headerObj, "CloseButton", "Close / Back to AR", new Vector2(200, 80));
        RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0, 0.5f);
        closeRt.anchorMax = new Vector2(0, 0.5f);
        closeRt.anchoredPosition = new Vector2(120, 0);
        closeBtn.onClick.AddListener(CloseUI);

        // 4. Tab Buttons
        GameObject tabsObj = new GameObject("TabsContainer");
        tabsObj.transform.SetParent(bgObj.transform, false);
        RectTransform tabsRt = tabsObj.AddComponent<RectTransform>();
        tabsRt.anchorMin = new Vector2(0, 1);
        tabsRt.anchorMax = new Vector2(1, 1);
        tabsRt.pivot = new Vector2(0.5f, 1);
        tabsRt.anchoredPosition = new Vector2(0, -120);
        tabsRt.sizeDelta = new Vector2(0, 100);

        HorizontalLayoutGroup hlg = tabsObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        
        Button btnDetail = CreateButton(tabsObj, "BtnDetail", "Detail", new Vector2(0, 0));
        Button btnEco = CreateButton(tabsObj, "BtnEcology", "Ecology", new Vector2(0, 0));
        Button btnCons = CreateButton(tabsObj, "BtnConservation", "Conservation", new Vector2(0, 0));
        Button btnGuide = CreateButton(tabsObj, "BtnGuide", "Guide", new Vector2(0, 0));

        // 5. Content Area
        GameObject contentObj = new GameObject("ContentArea");
        contentObj.transform.SetParent(bgObj.transform, false);
        RectTransform contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(40, 40);
        contentRt.offsetMax = new Vector2(-40, -260); // below tabs and above bottom

        // Build Panels
        _detailPanel = CreatePanel(contentObj, "DetailPanel");
        _ecologyPanel = CreatePanel(contentObj, "EcologyPanel");
        _conservationPanel = CreatePanel(contentObj, "ConservationPanel");
        _guidePanel = CreatePanel(contentObj, "GuidePanel");

        btnDetail.onClick.AddListener(() => ShowTab(_detailPanel));
        btnEco.onClick.AddListener(() => ShowTab(_ecologyPanel));
        btnCons.onClick.AddListener(() => ShowTab(_conservationPanel));
        btnGuide.onClick.AddListener(() => ShowTab(_guidePanel));

        // Setup Detail View
        _detailView = _detailPanel.AddComponent<SpeciesDetailView>();
        _detailView.commonNameText = CreateText(_detailPanel, "CommonName", "Common Name", 60, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1000, 80));
        _detailView.scientificNameText = CreateText(_detailPanel, "ScientificName", "Scientific Name", 40, FontStyle.Italic, TextAnchor.UpperLeft, new Vector2(0, -90), new Vector2(1000, 50));
        _detailView.familyText = CreateText(_detailPanel, "Family", "Family", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -150), new Vector2(1000, 40));
        _detailView.nativeRegionText = CreateText(_detailPanel, "NativeRegion", "Region", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -200), new Vector2(1000, 40));
        _detailView.descriptionText = CreateText(_detailPanel, "Description", "Description...", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -260), new Vector2(1000, 400));
        
        // Setup Ecology View
        _ecologyView = _ecologyPanel.AddComponent<EcologyView>();
        CreateText(_ecologyPanel, "EcoHeader", "Ecological Importance", 50, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1000, 60));
        _ecologyView.ecologicalImportanceText = CreateText(_ecologyPanel, "EcoText", "Eco details...", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -80), new Vector2(1000, 600));

        // Setup Conservation View
        _conservationView = _conservationPanel.AddComponent<ConservationView>();
        CreateText(_conservationPanel, "ConsHeader", "Conservation", 50, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1000, 60));
        _conservationView.statusText = CreateText(_conservationPanel, "Status", "Status: Unknown", 36, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -80), new Vector2(1000, 50));
        _conservationView.threatsText = CreateText(_conservationPanel, "Threats", "Threats...", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -150), new Vector2(1000, 150));
        _conservationView.bestPracticesText = CreateText(_conservationPanel, "BestPractices", "Best Practices...", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -320), new Vector2(1000, 150));

        // Setup Botanical Guide View
        _guideView = _guidePanel.AddComponent<BotanicalGuideView>();
        CreateText(_guidePanel, "GuideHeader", "Botanical Guide", 50, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(1000, 60));
        
        _guideView.responseAreaText = CreateText(_guidePanel, "ResponseArea", "Hello! Ask me a question.", 30, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -80), new Vector2(1000, 400));
        
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(_guidePanel.transform, false);
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = Color.white;
        RectTransform inputRt = inputObj.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0, 0); inputRt.anchorMax = new Vector2(1, 0);
        inputRt.pivot = new Vector2(0.5f, 0);
        inputRt.anchoredPosition = new Vector2(0, 100);
        inputRt.sizeDelta = new Vector2(-220, 80); // leave room for button
        
        Text inputText = CreateText(inputObj, "Text", "", 30, FontStyle.Normal, TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero);
        inputText.color = Color.black;
        inputText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        inputText.GetComponent<RectTransform>().anchorMax = Vector2.one;
        inputText.GetComponent<RectTransform>().sizeDelta = new Vector2(-20, 0);
        InputField iField = inputObj.AddComponent<InputField>();
        iField.textComponent = inputText;
        _guideView.questionInputField = iField;

        _guideView.askButton = CreateButton(_guidePanel, "AskButton", "Ask", new Vector2(180, 80));
        RectTransform askRt = _guideView.askButton.GetComponent<RectTransform>();
        askRt.anchorMin = new Vector2(1, 0); askRt.anchorMax = new Vector2(1, 0);
        askRt.pivot = new Vector2(1, 0);
        askRt.anchoredPosition = new Vector2(0, 100);
    }

    private Button CreateButton(GameObject parent, string name, string label, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.2f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (size.x > 0) rt.sizeDelta = size;

        Text txt = CreateText(btnObj, "Text", label, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        txt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        txt.GetComponent<RectTransform>().anchorMax = Vector2.one;
        txt.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        
        return btn;
    }

    private GameObject CreatePanel(GameObject parent, string name)
    {
        GameObject pnl = new GameObject(name);
        pnl.transform.SetParent(parent.transform, false);
        RectTransform rt = pnl.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return pnl;
    }

    private Text CreateText(GameObject parent, string name, string textStr, int fontSize, FontStyle style, TextAnchor align, Vector2 pos, Vector2 size)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = textStr;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color = Color.white;
        
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        if (size.x > 0) rt.sizeDelta = size;
        
        return txt;
    }
}
