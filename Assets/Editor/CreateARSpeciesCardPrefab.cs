using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Deterministic prefab generator for the Phase 3 production ARSpeciesCard.
/// Adheres strictly to TEAM_CERO_SPATIAL_AR_SPEC_v0.2.md.
/// Constructs:
/// - Meter-scale root with ARSpeciesCard & SpatialTether
/// - AnchorMarker (Point A) on the physical surface
/// - Genuine 3D Tether Cylinder (Section 10)
/// - CardAnchor (Point B) at presentation elevation
/// - Billboarding PlaqueVisual with high-contrast UI (620x380)
/// - All 6 official contextual fields simultaneously visible
/// </summary>
public class CreateARSpeciesCardPrefab
{
    [MenuItem("Tools/Rebuild AR Species Card Prefab")]
    public static void CreatePrefab()
    {
        string path = "Assets/Prefabs/UI/ARSpeciesCard.prefab";

        Font uiFont = ResolveFont();

        // 1. Root Presentation Object (World meter scale)
        GameObject root = new GameObject("ARSpeciesCard");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var speciesCard = root.AddComponent<ARSpeciesCard>();
        var spatialTether = root.AddComponent<SpatialTether>();

        // 2. Point A: Anchor Target Marker (Surface disc)
        // Root is instantiated at (0, 0.05, 0) relative to anchor, so anchor surface is at (0, -0.05, 0)
        GameObject anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anchorMarker.name = "AnchorMarker";
        anchorMarker.transform.SetParent(root.transform, false);
        anchorMarker.transform.localPosition = new Vector3(0, -0.05f, 0);
        anchorMarker.transform.localRotation = Quaternion.identity;
        anchorMarker.transform.localScale = new Vector3(0.06f, 0.0015f, 0.06f); // 6cm diameter, 3mm thin disc
        Object.DestroyImmediate(anchorMarker.GetComponent<Collider>());

        Material markerMat = CreateMaterial(new Color(0.06f, 0.73f, 0.51f, 0.95f), 0.2f, 0.8f); // Emerald #10B981
        anchorMarker.GetComponent<MeshRenderer>().material = markerMat;

        // 3. Point B: Card Anchor (Presentation elevation)
        // Elevated 15cm above root (so 20cm above the physical surface plane)
        GameObject cardAnchor = new GameObject("CardAnchor");
        cardAnchor.transform.SetParent(root.transform, false);
        cardAnchor.transform.localPosition = new Vector3(0, 0.15f, 0);
        cardAnchor.transform.localRotation = Quaternion.identity;
        cardAnchor.transform.localScale = Vector3.one;

        // 4. Genuine 3D Spatial Tether Cylinder (Spec Section 10)
        GameObject tetherObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tetherObj.name = "SpatialTetherRod";
        tetherObj.transform.SetParent(root.transform, false);
        Object.DestroyImmediate(tetherObj.GetComponent<Collider>());

        Material tetherMat = CreateMaterial(new Color(0.20f, 0.25f, 0.30f, 1.0f), 0.7f, 0.6f); // Titanium / graphite
        tetherObj.GetComponent<MeshRenderer>().material = tetherMat;

        // Configure SpatialTether component
        spatialTether.anchorPoint = anchorMarker.transform;
        spatialTether.cardPoint = cardAnchor.transform;
        spatialTether.tetherCylinder = tetherObj.transform;
        spatialTether.anchorMarker = anchorMarker.transform;
        spatialTether.rodDiameter = 0.005f; // 5mm solid rod
        spatialTether.UpdateTetherGeometry();

        // 5. Plaque Visual (Canvas + Billboard)
        // Child of CardAnchor, pivot at (0.5, 0.0) pinned directly to Point B
        GameObject plaqueVisual = new GameObject("PlaqueVisual");
        plaqueVisual.transform.SetParent(cardAnchor.transform, false);
        plaqueVisual.transform.localPosition = Vector3.zero;
        plaqueVisual.transform.localRotation = Quaternion.identity;
        plaqueVisual.transform.localScale = Vector3.one * 0.00040f; // Initial scale for editor preview

        var billboard = plaqueVisual.AddComponent<Billboard>();
        billboard.fixedPitch = 25f; // 25 degree tilt back for natural downward viewing
        billboard.swivelSpeed = 6f;

        var canvas = plaqueVisual.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasScaler = plaqueVisual.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 10;
        plaqueVisual.AddComponent<GraphicRaycaster>();

        RectTransform plaqueRt = plaqueVisual.GetComponent<RectTransform>();
        plaqueRt.sizeDelta = new Vector2(620, 380);
        plaqueRt.pivot = new Vector2(0.5f, 0f); // Pivot at bottom-center

        // 6. UI Structure
        // 6.1 Solid Opaque Background Panel
        GameObject bgObj = new GameObject("BackgroundPanel");
        bgObj.transform.SetParent(plaqueVisual.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.07f, 0.09f, 1.0f); // Fully opaque obsidian/charcoal #0D1117

        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.06f, 0.73f, 0.51f, 0.85f); // Emerald accent border #10B981
        bgOutline.effectDistance = new Vector2(2, -2);

        // 6.2 Header Bar (Y: 320 to 370)
        GameObject headerObj = new GameObject("HeaderBar");
        headerObj.transform.SetParent(bgObj.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1); headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = new Vector2(0, -10);
        headerRt.sizeDelta = new Vector2(-40, 45);

        // Status Badge Capsule (Left)
        GameObject badgeObj = new GameObject("StatusBadge");
        badgeObj.transform.SetParent(headerObj.transform, false);
        RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0, 0.5f); badgeRt.anchorMax = new Vector2(0, 0.5f);
        badgeRt.pivot = new Vector2(0, 0.5f);
        badgeRt.anchoredPosition = new Vector2(0, 0);
        badgeRt.sizeDelta = new Vector2(170, 30);

        Image badgeImg = badgeObj.AddComponent<Image>();
        badgeImg.color = new Color(0.02f, 0.31f, 0.23f, 0.9f); // Dark emerald capsule background

        Text statusText = CreateText(badgeObj, "StatusText", "● IDENTIFIED", 13, FontStyle.Bold, TextAnchor.MiddleCenter, uiFont);
        statusText.color = new Color(0.20f, 0.83f, 0.60f, 1.0f); // Vibrant emerald #34D399
        StretchFull(statusText.GetComponent<RectTransform>());

        // Brand Label (Right)
        GameObject brandObj = new GameObject("BrandTag");
        brandObj.transform.SetParent(headerObj.transform, false);
        RectTransform brandRt = brandObj.AddComponent<RectTransform>();
        brandRt.anchorMin = new Vector2(1, 0.5f); brandRt.anchorMax = new Vector2(1, 0.5f);
        brandRt.pivot = new Vector2(1, 0.5f);
        brandRt.anchoredPosition = new Vector2(0, 0);
        brandRt.sizeDelta = new Vector2(180, 24);

        Text brandText = CreateText(brandObj, "BrandText", "TEAM CERO FIELD AR", 11, FontStyle.Bold, TextAnchor.MiddleRight, uiFont);
        brandText.color = new Color(0.58f, 0.64f, 0.72f, 0.9f); // Slate #94A3B8
        StretchFull(brandRt);

        // Header Divider Line
        GameObject headerDiv = new GameObject("HeaderDivider");
        headerDiv.transform.SetParent(bgObj.transform, false);
        RectTransform hDivRt = headerDiv.AddComponent<RectTransform>();
        hDivRt.anchorMin = new Vector2(0, 1); hDivRt.anchorMax = new Vector2(1, 1);
        hDivRt.pivot = new Vector2(0.5f, 1);
        hDivRt.anchoredPosition = new Vector2(0, -60);
        hDivRt.sizeDelta = new Vector2(-40, 2);
        Image hDivImg = headerDiv.AddComponent<Image>();
        hDivImg.color = new Color(0.12f, 0.16f, 0.23f, 1f); // Slate divider #1E293B

        // 6.3 Identity Section (Common Name + Scientific Name)
        // Common Name (26pt Bold White)
        Text commonName = CreateText(bgObj, "CommonName", "Common Name", 26, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        commonName.color = Color.white;
        SetRect(commonName, 25, -70, 570, 36);

        // Scientific Name (17pt Italic Emerald)
        Text scientificName = CreateText(bgObj, "ScientificName", "Scientific Name", 17, FontStyle.Italic, TextAnchor.UpperLeft, uiFont);
        scientificName.color = new Color(0.20f, 0.83f, 0.60f, 1.0f); // Emerald #34D399
        SetRect(scientificName, 25, -108, 570, 28);

        // Identity Divider Line
        GameObject idDiv = new GameObject("IdentityDivider");
        idDiv.transform.SetParent(bgObj.transform, false);
        RectTransform idDivRt = idDiv.AddComponent<RectTransform>();
        idDivRt.anchorMin = new Vector2(0, 1); idDivRt.anchorMax = new Vector2(1, 1);
        idDivRt.pivot = new Vector2(0.5f, 1);
        idDivRt.anchoredPosition = new Vector2(0, -142);
        idDivRt.sizeDelta = new Vector2(-40, 1);
        Image idDivImg = idDiv.AddComponent<Image>();
        idDivImg.color = new Color(0.12f, 0.16f, 0.23f, 0.8f);

        // 6.4 Four Contextual Information Quadrants (2x2 Grid)
        // Top-Left: PLANT FAMILY (Spec Field 3)
        Text familyLabel = CreateText(bgObj, "FamilyLabel", "PLANT FAMILY", 11, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        familyLabel.color = new Color(0.58f, 0.64f, 0.72f, 0.9f);
        SetRect(familyLabel, 25, -155, 275, 18);

        Text familyValue = CreateText(bgObj, "FamilyValue", "Family Name", 15, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        familyValue.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        SetRect(familyValue, 25, -175, 275, 26);

        // Top-Right: NATIVE REGION (Spec Field 4)
        Text regionLabel = CreateText(bgObj, "RegionLabel", "NATIVE REGION", 11, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        regionLabel.color = new Color(0.58f, 0.64f, 0.72f, 0.9f);
        SetRect(regionLabel, 320, -155, 275, 18);

        Text regionValue = CreateText(bgObj, "RegionValue", "Native Region", 15, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        regionValue.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        SetRect(regionValue, 320, -175, 275, 26);

        // Grid Horizontal Divider
        GameObject gridHDiv = new GameObject("GridHDivider");
        gridHDiv.transform.SetParent(bgObj.transform, false);
        RectTransform ghDivRt = gridHDiv.AddComponent<RectTransform>();
        ghDivRt.anchorMin = new Vector2(0, 1); ghDivRt.anchorMax = new Vector2(1, 1);
        ghDivRt.pivot = new Vector2(0.5f, 1);
        ghDivRt.anchoredPosition = new Vector2(0, -212);
        ghDivRt.sizeDelta = new Vector2(-40, 1);
        Image ghDivImg = gridHDiv.AddComponent<Image>();
        ghDivImg.color = new Color(0.12f, 0.16f, 0.23f, 0.8f);

        // Bottom-Left: ECOLOGICAL IMPORTANCE (Spec Field 5)
        Text ecoLabel = CreateText(bgObj, "EcoLabel", "ECOLOGICAL IMPORTANCE", 11, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        ecoLabel.color = new Color(0.58f, 0.64f, 0.72f, 0.9f);
        SetRect(ecoLabel, 25, -222, 275, 18);

        Text ecoValue = CreateText(bgObj, "EcoValue", "Ecological importance details...", 13, FontStyle.Normal, TextAnchor.UpperLeft, uiFont);
        ecoValue.color = new Color(0.89f, 0.91f, 0.94f, 1f);
        ecoValue.horizontalOverflow = HorizontalWrapMode.Wrap;
        ecoValue.verticalOverflow = VerticalWrapMode.Truncate;
        SetRect(ecoValue, 25, -242, 275, 95);

        // Bottom-Right: CONSERVATION STATUS (Spec Field 6)
        Text consLabel = CreateText(bgObj, "ConsLabel", "CONSERVATION STATUS", 11, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        consLabel.color = new Color(0.58f, 0.64f, 0.72f, 0.9f);
        SetRect(consLabel, 320, -222, 275, 18);

        Text consValue = CreateText(bgObj, "ConsValue", "Least Concern (IUCN)", 14, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        consValue.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Sky blue #38BDF8 accent
        consValue.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetRect(consValue, 320, -242, 275, 95);

        // Grid Vertical Divider Line
        GameObject gridVDiv = new GameObject("GridVDivider");
        gridVDiv.transform.SetParent(bgObj.transform, false);
        RectTransform gvDivRt = gridVDiv.AddComponent<RectTransform>();
        gvDivRt.anchorMin = new Vector2(0.5f, 1); gvDivRt.anchorMax = new Vector2(0.5f, 1);
        gvDivRt.pivot = new Vector2(0.5f, 1);
        gvDivRt.anchoredPosition = new Vector2(0, -150);
        gvDivRt.sizeDelta = new Vector2(1, 195);
        Image gvDivImg = gridVDiv.AddComponent<Image>();
        gvDivImg.color = new Color(0.12f, 0.16f, 0.23f, 0.6f);

        // 7. Wiring Component Serialized Fields
        speciesCard.plaqueVisual = plaqueVisual.transform;
        speciesCard.spatialTether = spatialTether;
        speciesCard.statusBadgeText = statusText;
        speciesCard.statusBadgeBackground = badgeImg;
        speciesCard.commonNameText = commonName;
        speciesCard.scientificNameText = scientificName;
        speciesCard.familyText = familyValue;
        speciesCard.nativeRegionText = regionValue;
        speciesCard.ecoImportanceText = ecoValue;
        speciesCard.conservationStatusText = consValue;
        speciesCard.collapsedView = bgObj;
        speciesCard.expandedView = null;
        speciesCard.baseScale = 0.00040f;
        speciesCard.minScale = 0.00022f;
        speciesCard.maxScale = 0.00065f;

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[Phase 3] Production ARSpeciesCard prefab successfully rebuilt at " + path);
    }

    private static Font ResolveFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) return font;
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) return font;
        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    private static Material CreateMaterial(Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                     ?? Shader.Find("Universal Render Pipeline/Unlit") 
                     ?? Shader.Find("Standard");
        
        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    private static Text CreateText(GameObject parent, string name, string defaultText, int fontSize, FontStyle style, TextAnchor alignment, Font font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Text txt = go.AddComponent<Text>();
        txt.text = defaultText;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = alignment;
        txt.color = Color.white;
        return txt;
    }

    private static void SetRect(Text txt, float x, float y, float w, float h)
    {
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
