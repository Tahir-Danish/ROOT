using UnityEngine;
using UnityEngine.UI;

public class DetectionVisualizer : MonoBehaviour
{
    public Pv002Pipeline pipeline;

    private Canvas _canvas;
    private RectTransform _boxRect;
    private Image _boxImage;
    private RectTransform _hudTextRect;
    private Text _hudText;
    private Text _heartbeatText;

    void Start()
    {
        Debug.Log("[UIBRIDGE] VisualizerStarted");

        // 1. Resolve a guaranteed valid runtime font in Unity 6
        Font font = ResolveFont();
        Debug.Log($"[UIBRIDGE] FontReady | font={(font != null ? font.name : "NULL")}");

        // 2. Create an overlay canvas robustly
        GameObject canvasObj = new GameObject("DetectionCanvas");
        canvasObj.layer = LayerMask.NameToLayer("UI");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100; // Ensure it renders on top

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Portrait reference
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 3. Create independent DebugHeartbeat (Child of DetectionCanvas)
        GameObject heartbeatObj = new GameObject("DebugHeartbeat");
        heartbeatObj.layer = LayerMask.NameToLayer("UI");
        heartbeatObj.transform.SetParent(canvasObj.transform, false);

        RectTransform heartbeatRect = heartbeatObj.AddComponent<RectTransform>();
        heartbeatRect.anchorMin = new Vector2(0, 1);
        heartbeatRect.anchorMax = new Vector2(1, 1);
        heartbeatRect.pivot = new Vector2(0.5f, 1);
        heartbeatRect.anchoredPosition = new Vector2(0, -40);
        heartbeatRect.sizeDelta = new Vector2(1000, 100);

        _heartbeatText = heartbeatObj.AddComponent<Text>();
        _heartbeatText.font = font;
        _heartbeatText.fontSize = 32;
        _heartbeatText.color = Color.magenta;
        _heartbeatText.alignment = TextAnchor.MiddleCenter;

        Outline hbOutline = heartbeatObj.AddComponent<Outline>();
        hbOutline.effectColor = Color.black;
        hbOutline.effectDistance = new Vector2(2, -2);

        _heartbeatText.text = "UI BRIDGE ONLINE";

        // 4. Create BoundingBox (Child of DetectionCanvas)
        GameObject boxObj = new GameObject("BoundingBox");
        boxObj.layer = LayerMask.NameToLayer("UI");
        boxObj.transform.SetParent(canvasObj.transform, false);
        _boxImage = boxObj.AddComponent<Image>();
        _boxImage.color = new Color(0, 1, 0, 0.3f);
        _boxRect = boxObj.GetComponent<RectTransform>();
        _boxRect.anchorMin = new Vector2(0, 0);
        _boxRect.anchorMax = new Vector2(0, 0);
        _boxRect.pivot = new Vector2(0, 0); 
        boxObj.SetActive(false);

        // 5. Create HUDText (Child of DetectionCanvas, NOT BoundingBox)
        GameObject hudTextObj = new GameObject("HUDText");
        hudTextObj.layer = LayerMask.NameToLayer("UI");
        hudTextObj.transform.SetParent(canvasObj.transform, false);
        _hudText = hudTextObj.AddComponent<Text>();
        _hudText.font = font;
        _hudText.fontSize = 36;
        _hudText.color = Color.white;
        _hudText.alignment = TextAnchor.UpperLeft;

        Outline hudOutline = hudTextObj.AddComponent<Outline>();
        hudOutline.effectColor = Color.black;
        hudOutline.effectDistance = new Vector2(2, -2);

        _hudTextRect = hudTextObj.GetComponent<RectTransform>();
        _hudTextRect.anchorMin = new Vector2(0, 0);
        _hudTextRect.anchorMax = new Vector2(0, 0);
        _hudTextRect.pivot = new Vector2(0, 0);
        _hudTextRect.sizeDelta = new Vector2(600, 140);
        hudTextObj.SetActive(false);

        // 6. Pipeline subscription
        if (pipeline == null)
        {
            pipeline = Object.FindAnyObjectByType<Pv002Pipeline>();
        }

        if (pipeline != null)
        {
            if (pipeline.onDetectionUpdated == null)
            {
                pipeline.onDetectionUpdated = new Pv002Pipeline.DetectionUpdateEvent();
            }
            pipeline.onDetectionUpdated.AddListener(OnDetectionUpdated);
            Debug.Log("[UIBRIDGE] PipelineSubscriptionReady");
        }

        // 7. Temporary Experimental AR Tracking Card Mode (Cleanly removable)
        if (GetComponent<ExperimentalARTrackingCard>() == null)
        {
            gameObject.AddComponent<ExperimentalARTrackingCard>();
        }
    }

    private Font ResolveFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        if (font == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<Font>();
            if (fonts != null && fonts.Length > 0)
            {
                font = fonts[0];
            }
        }
        return font;
    }

    private void OnDetectionUpdated(DetectionResult result)
    {
        // 1. Update independent heartbeat text regardless of bounding box presence
        if (_heartbeatText != null)
        {
            _heartbeatText.text = $"ML OUTPUT RECEIVED | {result.status} | {(result.confidence * 100f):F0}%";
        }

        // 2. Check bounding box presence
        if (!result.hasBoundingBox)
        {
            if (_boxRect != null) _boxRect.gameObject.SetActive(false);
            if (_hudTextRect != null) _hudTextRect.gameObject.SetActive(false);
            return;
        }

        if (_boxRect != null) _boxRect.gameObject.SetActive(true);
        if (_hudTextRect != null) _hudTextRect.gameObject.SetActive(true);

        float screenW = Screen.width;
        float screenH = Screen.height;
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1.0f;
        if (scaleFactor <= 0.001f) scaleFactor = 1.0f;
        
        float cnvX = result.boundingBox.x / scaleFactor;
        float cnvY = result.boundingBox.y / scaleFactor;
        float cnvW = result.boundingBox.width / scaleFactor;
        float cnvH = result.boundingBox.height / scaleFactor;

        _boxRect.anchoredPosition = new Vector2(cnvX, cnvY);
        _boxRect.sizeDelta = new Vector2(cnvW, cnvH);

        // Position HUDText at the top edge of the bounding box
        _hudTextRect.anchoredPosition = new Vector2(cnvX, cnvY + cnvH + 5);

        if (result.status == "unrecognized")
        {
            _boxImage.color = new Color(1f, 1f, 0f, 0.35f); // Yellow for unknown
            _hudText.text = $"PLANT DETECTED\nUNKNOWN SPECIES\nConfidence: {(result.confidence * 100f):F1}%";
        }
        else
        {
            _boxImage.color = new Color(0f, 1f, 0f, 0.35f); // Green for known
            _hudText.text = $"{result.species_id}\nConfidence: {(result.confidence * 100f):F1}%";
        }

        bool insideBounds = (result.boundingBox.x >= 0 && result.boundingBox.x <= screenW &&
                             result.boundingBox.y >= 0 && result.boundingBox.y <= screenH);

        Debug.Log($"[UIBRIDGE] VisualizerRendered | scr: {screenW}x{screenH} | rawBbox: {result.boundingBox.x:F1},{result.boundingBox.y:F1},{result.boundingBox.width:F1},{result.boundingBox.height:F1} | cnvRect: {cnvX:F1},{cnvY:F1},{cnvW:F1},{cnvH:F1} | renderMode: {_canvas.renderMode} | scaleFactor: {scaleFactor:F3} | state: {result.status} | conf: {result.confidence:F2} | inside: {insideBounds}");
    }

    void OnDestroy()
    {
        if (pipeline != null && pipeline.onDetectionUpdated != null)
        {
            pipeline.onDetectionUpdated.RemoveListener(OnDetectionUpdated);
        }
    }
}
