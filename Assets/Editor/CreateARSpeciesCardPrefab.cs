using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Deterministic prefab generator for the Phase 3 production ARSpeciesCard.
/// Creates a solarpunk / visionOS-inspired 3D botanical field intelligence artifact:
/// - Genuine 3D physical chassis with beveled edges and rear thickness (not a flat 2D canvas).
/// - Frosted dark-tinted glass bezel and multi-layered depth.
/// - Slender, luminous non-magenta 3D tether leader rod (2.8mm optical composite).
/// - Compact, non-magenta botanical spatial reticle indicator on the surface plane.
/// - Crisp typography using Inter font with all six contextual fields simultaneously readable.
/// - Honest unknown/unconfirmed identification values.
/// - Dedicated serialized URP material and 3D mesh assets in the project.
/// </summary>
public class CreateARSpeciesCardPrefab
{
    private const string MaterialsDir = "Assets/Materials/SpatialAR";
    private const string ModelsDir = "Assets/Models/SpatialAR";
    private const string PrefabPath = "Assets/Prefabs/UI/ARSpeciesCard.prefab";

    [MenuItem("Tools/Rebuild AR Species Card Prefab")]
    public static void CreatePrefab()
    {
        EnsureDirectories();

        // 1. Resolve Resources
        Font uiFont = ResolveFont();
        Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/Round Radius 4.png");

        // 2. Serialized Project Materials (Permanent assets, zero in-memory/magenta missing shaders)
        Material tetherMat = GetOrCreateMaterial(
            $"{MaterialsDir}/SpatialTetherLuminous.mat",
            new Color(0.12f, 0.72f, 0.65f, 1.0f), // Luminous botanical teal
            0.10f, 0.95f,
            false,
            new Color(0.17f, 0.83f, 0.75f, 1.0f) * 1.3f // Glowing emission
        );

        Material anchorBaseMat = GetOrCreateMaterial(
            $"{MaterialsDir}/SpatialAnchorBase.mat",
            new Color(0.08f, 0.12f, 0.16f, 1.0f), // Dark basalt/slate base
            0.40f, 0.85f
        );

        Material anchorNodeMat = GetOrCreateMaterial(
            $"{MaterialsDir}/SpatialAnchorNode.mat",
            new Color(0.10f, 0.82f, 0.55f, 1.0f), // Luminous botanical emerald
            0.20f, 0.95f,
            false,
            new Color(0.10f, 0.82f, 0.55f, 1.0f) * 1.2f // Beacon emission
        );

        Material collarMat = GetOrCreateMaterial(
            $"{MaterialsDir}/SpatialMountingCollar.mat",
            new Color(0.16f, 0.22f, 0.28f, 1.0f), // Dark titanium socket fitting
            0.65f, 0.80f
        );

        Material casingMat = GetOrCreateMaterial(
            $"{MaterialsDir}/SpatialPlaqueCasing.mat",
            new Color(0.06f, 0.09f, 0.12f, 1.0f), // Deep obsidian / basalt chassis
            0.35f, 0.75f
        );

        Material glassBezelMat = GetOrCreateMaterial(
            $"{MaterialsDir}/SpatialGlassBezel.mat",
            new Color(0.07f, 0.18f, 0.22f, 0.85f), // Translucent frosted dark-teal glass
            0.15f, 0.96f,
            true, // Transparent
            new Color(0.10f, 0.55f, 0.48f, 1.0f) * 0.35f // Subtle edge sheen
        );

        // 3. Serialized 3D Mesh Assets
        Mesh chassisMesh = GetOrCreateChassisMesh($"{ModelsDir}/SpatialChassisMesh.asset", 636f, 396f, 22f, 22f, 5f);
        Mesh bezelMesh = GetOrCreateBezelMesh($"{ModelsDir}/SpatialBezelMesh.asset", 636f, 396f, 616f, 376f, 22f, 12f, 4f);

        AssetDatabase.SaveAssets();

        // 4. Root Presentation Object (World meter scale)
        GameObject root = new GameObject("ARSpeciesCard");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var speciesCard = root.AddComponent<ARSpeciesCard>();
        var spatialTether = root.AddComponent<SpatialTether>();

        // 5. Point A: Anchor Indicator (Surface Reticle on the detected plane)
        // Root is at (0, 0.05, 0) relative to anchor, so anchor plane is at (0, -0.05, 0)
        GameObject anchorReticle = new GameObject("AnchorReticle");
        anchorReticle.transform.SetParent(root.transform, false);
        anchorReticle.transform.localPosition = new Vector3(0, -0.05f, 0);
        anchorReticle.transform.localRotation = Quaternion.identity;
        anchorReticle.transform.localScale = Vector3.one;

        // 5.1 Base disc (2.8cm diameter, 1.2mm thin beveled plate)
        GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisc.name = "BaseDisc";
        baseDisc.transform.SetParent(anchorReticle.transform, false);
        baseDisc.transform.localPosition = Vector3.zero;
        baseDisc.transform.localScale = new Vector3(0.028f, 0.0006f, 0.028f);
        Object.DestroyImmediate(baseDisc.GetComponent<Collider>());
        baseDisc.GetComponent<MeshRenderer>().sharedMaterial = anchorBaseMat;

        // 5.2 Central optical node (7mm diameter, 1.6mm height)
        GameObject opticalNode = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        opticalNode.name = "OpticalNode";
        opticalNode.transform.SetParent(anchorReticle.transform, false);
        opticalNode.transform.localPosition = new Vector3(0, 0.0008f, 0);
        opticalNode.transform.localScale = new Vector3(0.007f, 0.0008f, 0.007f);
        Object.DestroyImmediate(opticalNode.GetComponent<Collider>());
        opticalNode.GetComponent<MeshRenderer>().sharedMaterial = anchorNodeMat;

        // 5.3 Lower socket collar (4.5mm diameter, 2.5mm height)
        GameObject lowerCollar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lowerCollar.name = "SocketCollar";
        lowerCollar.transform.SetParent(anchorReticle.transform, false);
        lowerCollar.transform.localPosition = new Vector3(0, 0.0018f, 0);
        lowerCollar.transform.localScale = new Vector3(0.0045f, 0.0012f, 0.0045f);
        Object.DestroyImmediate(lowerCollar.GetComponent<Collider>());
        lowerCollar.GetComponent<MeshRenderer>().sharedMaterial = collarMat;

        // 6. Point B: Card Anchor (Presentation elevation: 15cm above root, 20cm above surface)
        GameObject cardAnchor = new GameObject("CardAnchor");
        cardAnchor.transform.SetParent(root.transform, false);
        cardAnchor.transform.localPosition = new Vector3(0, 0.15f, 0);
        cardAnchor.transform.localRotation = Quaternion.identity;
        cardAnchor.transform.localScale = Vector3.one;

        // 6.1 Mechanical mounting collar at Point B
        GameObject upperCollar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        upperCollar.name = "MountingCollar";
        upperCollar.transform.SetParent(cardAnchor.transform, false);
        upperCollar.transform.localPosition = new Vector3(0, -0.003f, 0);
        upperCollar.transform.localScale = new Vector3(0.006f, 0.003f, 0.006f);
        Object.DestroyImmediate(upperCollar.GetComponent<Collider>());
        upperCollar.GetComponent<MeshRenderer>().sharedMaterial = collarMat;

        // 7. Genuine 3D Spatial Tether Rod (Spec Section 10)
        GameObject tetherObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tetherObj.name = "SpatialTetherRod";
        tetherObj.transform.SetParent(root.transform, false);
        Object.DestroyImmediate(tetherObj.GetComponent<Collider>());
        tetherObj.GetComponent<MeshRenderer>().sharedMaterial = tetherMat;

        spatialTether.anchorPoint = anchorReticle.transform;
        spatialTether.cardPoint = cardAnchor.transform;
        spatialTether.tetherCylinder = tetherObj.transform;
        spatialTether.anchorMarker = anchorReticle.transform;
        spatialTether.rodDiameter = 0.0028f; // 2.8mm slender filament
        spatialTether.UpdateTetherGeometry();

        // 8. Plaque Visual (Layered 3D Physical Body + Billboard + WorldSpace Canvas)
        GameObject plaqueVisual = new GameObject("PlaqueVisual");
        plaqueVisual.transform.SetParent(cardAnchor.transform, false);
        plaqueVisual.transform.localPosition = Vector3.zero;
        plaqueVisual.transform.localRotation = Quaternion.identity;
        plaqueVisual.transform.localScale = Vector3.one * 0.00040f;

        var billboard = plaqueVisual.AddComponent<Billboard>();
        billboard.fixedPitch = 25f; // 25 degree tilt back for natural downward viewing
        billboard.swivelSpeed = 6f;

        // 8.1 3D Physical Rear Chassis (Beveled Rounded Slab behind canvas)
        GameObject chassisObj = new GameObject("PhysicalChassis_3D");
        chassisObj.transform.SetParent(plaqueVisual.transform, false);
        chassisObj.transform.localPosition = new Vector3(0, 190f, 4f); // Sits from Z = 4 to Z = 26
        chassisObj.transform.localRotation = Quaternion.identity;
        chassisObj.transform.localScale = Vector3.one;

        var chassisFilter = chassisObj.AddComponent<MeshFilter>();
        chassisFilter.sharedMesh = chassisMesh;
        var chassisRenderer = chassisObj.AddComponent<MeshRenderer>();
        chassisRenderer.sharedMaterial = casingMat;

        // 8.2 3D Front Glass Bezel (Chamfered Rim Framing Canvas)
        GameObject bezelObj = new GameObject("GlassBezel_3D");
        bezelObj.transform.SetParent(plaqueVisual.transform, false);
        bezelObj.transform.localPosition = new Vector3(0, 190f, 0f); // Sits at Z = -2 to Z = +2
        bezelObj.transform.localRotation = Quaternion.identity;
        bezelObj.transform.localScale = Vector3.one;

        var bezelFilter = bezelObj.AddComponent<MeshFilter>();
        bezelFilter.sharedMesh = bezelMesh;
        var bezelRenderer = bezelObj.AddComponent<MeshRenderer>();
        bezelRenderer.sharedMaterial = glassBezelMat;

        // 8.3 UI WorldSpace Canvas
        var canvas = plaqueVisual.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasScaler = plaqueVisual.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 10;
        plaqueVisual.AddComponent<GraphicRaycaster>();

        RectTransform plaqueRt = plaqueVisual.GetComponent<RectTransform>();
        plaqueRt.sizeDelta = new Vector2(620, 380);
        plaqueRt.pivot = new Vector2(0.5f, 0f); // Pinned to Point B at bottom-center

        // 9. Canvas UI Structure
        // 9.1 Frosted Dark-Tinted Glass Background Panel
        GameObject bgObj = new GameObject("BackgroundPanel");
        bgObj.transform.SetParent(plaqueVisual.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        StretchFull(bgRt);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.04f, 0.07f, 0.09f, 0.88f); // Translucent frosted dark obsidian
        if (roundedSprite != null)
        {
            bgImage.sprite = roundedSprite;
            bgImage.type = Image.Type.Sliced;
        }

        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.08f, 0.72f, 0.65f, 0.45f); // Subtle botanical cyan edge highlight
        bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 9.2 Header Bar
        GameObject headerObj = new GameObject("HeaderBar");
        headerObj.transform.SetParent(bgObj.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1); headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = new Vector2(0, -12);
        headerRt.sizeDelta = new Vector2(-40, 40);

        // Status Badge Capsule (Left)
        GameObject badgeObj = new GameObject("StatusBadge");
        badgeObj.transform.SetParent(headerObj.transform, false);
        RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0, 0.5f); badgeRt.anchorMax = new Vector2(0, 0.5f);
        badgeRt.pivot = new Vector2(0, 0.5f);
        badgeRt.anchoredPosition = new Vector2(0, 0);
        badgeRt.sizeDelta = new Vector2(180, 28);

        Image badgeImg = badgeObj.AddComponent<Image>();
        badgeImg.color = new Color(0.02f, 0.31f, 0.23f, 0.9f); // Dark emerald capsule
        if (roundedSprite != null)
        {
            badgeImg.sprite = roundedSprite;
            badgeImg.type = Image.Type.Sliced;
        }

        Text statusText = CreateText(badgeObj, "StatusText", "● IDENTIFIED", 13, FontStyle.Bold, TextAnchor.MiddleCenter, uiFont);
        statusText.color = new Color(0.20f, 0.83f, 0.60f, 1.0f); // Emerald #34D399
        StretchFull(statusText.GetComponent<RectTransform>());

        // Brand Label (Right)
        GameObject brandObj = new GameObject("BrandTag");
        brandObj.transform.SetParent(headerObj.transform, false);
        RectTransform brandRt = brandObj.AddComponent<RectTransform>();
        brandRt.anchorMin = new Vector2(1, 0.5f); brandRt.anchorMax = new Vector2(1, 0.5f);
        brandRt.pivot = new Vector2(1, 0.5f);
        brandRt.anchoredPosition = new Vector2(0, 0);
        brandRt.sizeDelta = new Vector2(210, 24);

        Text brandText = CreateText(brandObj, "BrandText", "TEAM CERO // FIELD AR", 11, FontStyle.Bold, TextAnchor.MiddleRight, uiFont);
        brandText.color = new Color(0.40f, 0.48f, 0.58f, 0.95f); // Refined slate #64748B
        StretchFull(brandRt);

        // Header Divider Line
        GameObject headerDiv = new GameObject("HeaderDivider");
        headerDiv.transform.SetParent(bgObj.transform, false);
        RectTransform hDivRt = headerDiv.AddComponent<RectTransform>();
        hDivRt.anchorMin = new Vector2(0, 1); hDivRt.anchorMax = new Vector2(1, 1);
        hDivRt.pivot = new Vector2(0.5f, 1);
        hDivRt.anchoredPosition = new Vector2(0, -58);
        hDivRt.sizeDelta = new Vector2(-40, 1);
        Image hDivImg = headerDiv.AddComponent<Image>();
        hDivImg.color = new Color(0.12f, 0.20f, 0.26f, 0.7f); // Frosted hairline

        // 9.3 Identity Section (Common Name + Latin Scientific Name)
        // Common Name (26pt Bold White)
        Text commonName = CreateText(bgObj, "CommonName", "Common Name", 26, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        commonName.color = Color.white;
        SetRect(commonName, 25, -68, 570, 36);

        // Scientific Name (16pt Italic Botanical Teal #2DD4BF)
        Text scientificName = CreateText(bgObj, "ScientificName", "Scientific Name", 16, FontStyle.Italic, TextAnchor.UpperLeft, uiFont);
        scientificName.color = new Color(0.18f, 0.83f, 0.75f, 1.0f); // #2DD4BF
        SetRect(scientificName, 25, -106, 570, 26);

        // Identity Divider Line
        GameObject idDiv = new GameObject("IdentityDivider");
        idDiv.transform.SetParent(bgObj.transform, false);
        RectTransform idDivRt = idDiv.AddComponent<RectTransform>();
        idDivRt.anchorMin = new Vector2(0, 1); idDivRt.anchorMax = new Vector2(1, 1);
        idDivRt.pivot = new Vector2(0.5f, 1);
        idDivRt.anchoredPosition = new Vector2(0, -138);
        idDivRt.sizeDelta = new Vector2(-40, 1);
        Image idDivImg = idDiv.AddComponent<Image>();
        idDivImg.color = new Color(0.12f, 0.20f, 0.26f, 0.7f);

        // 9.4 Four Contextual Information Quadrants (2x2 Grid)
        // Sub-panel quadrant backing cards for clean modular organization
        CreateQuadrantBacking(bgObj, "QuadBacking_TL", 20, -148, 280, 95, roundedSprite);
        CreateQuadrantBacking(bgObj, "QuadBacking_TR", 320, -148, 280, 95, roundedSprite);
        CreateQuadrantBacking(bgObj, "QuadBacking_BL", 20, -252, 280, 110, roundedSprite);
        CreateQuadrantBacking(bgObj, "QuadBacking_BR", 320, -252, 280, 110, roundedSprite);

        // Top-Left: PLANT FAMILY (Spec Field 3)
        Text familyLabel = CreateText(bgObj, "FamilyLabel", "PLANT FAMILY", 10, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        familyLabel.color = new Color(0.48f, 0.56f, 0.65f, 1.0f);
        SetRect(familyLabel, 32, -156, 260, 16);

        Text familyValue = CreateText(bgObj, "FamilyValue", "Family Name", 15, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        familyValue.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        SetRect(familyValue, 32, -174, 260, 26);

        // Top-Right: NATIVE REGION (Spec Field 4)
        Text regionLabel = CreateText(bgObj, "RegionLabel", "NATIVE REGION", 10, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        regionLabel.color = new Color(0.48f, 0.56f, 0.65f, 1.0f);
        SetRect(regionLabel, 332, -156, 260, 16);

        Text regionValue = CreateText(bgObj, "RegionValue", "Native Region", 15, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        regionValue.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        SetRect(regionValue, 332, -174, 260, 26);

        // Bottom-Left: ECOLOGICAL IMPORTANCE (Spec Field 5)
        Text ecoLabel = CreateText(bgObj, "EcoLabel", "ECOLOGICAL IMPORTANCE", 10, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        ecoLabel.color = new Color(0.48f, 0.56f, 0.65f, 1.0f);
        SetRect(ecoLabel, 32, -260, 260, 16);

        Text ecoValue = CreateText(bgObj, "EcoValue", "Ecological importance details...", 12, FontStyle.Normal, TextAnchor.UpperLeft, uiFont);
        ecoValue.color = new Color(0.88f, 0.91f, 0.94f, 1f);
        ecoValue.horizontalOverflow = HorizontalWrapMode.Wrap;
        ecoValue.verticalOverflow = VerticalWrapMode.Truncate;
        SetRect(ecoValue, 32, -278, 260, 78);

        // Bottom-Right: CONSERVATION STATUS (Spec Field 6)
        Text consLabel = CreateText(bgObj, "ConsLabel", "CONSERVATION STATUS", 10, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        consLabel.color = new Color(0.48f, 0.56f, 0.65f, 1.0f);
        SetRect(consLabel, 332, -260, 260, 16);

        Text consValue = CreateText(bgObj, "ConsValue", "Least Concern (IUCN)", 14, FontStyle.Bold, TextAnchor.UpperLeft, uiFont);
        consValue.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Sky blue #38BDF8 accent
        consValue.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetRect(consValue, 332, -278, 260, 78);

        // 10. Wire ARSpeciesCard Component
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

        // 11. Save Prefab
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("[Phase 3] Solarpunk 3D ARSpeciesCard prefab successfully rebuilt at " + PrefabPath);
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(MaterialsDir))
        {
            Directory.CreateDirectory(MaterialsDir);
        }
        if (!Directory.Exists(ModelsDir))
        {
            Directory.CreateDirectory(ModelsDir);
        }
        AssetDatabase.Refresh();
    }

    private static Material GetOrCreateMaterial(string assetPath, Color baseColor, float metallic, float smoothness, bool isTransparent = false, Color? emissionColor = null)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                         ?? Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, assetPath);
        }

        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (isTransparent)
        {
            mat.SetFloat("_Surface", 1.0f);
            mat.SetFloat("_Blend", 0.0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_Surface", 0.0f);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = -1;
        }

        if (emissionColor.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor.Value);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Mesh GetOrCreateChassisMesh(string assetPath, float W, float H, float D, float R, float B)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        bool isNew = false;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "SpatialChassisMesh";
            isNew = true;
        }
        else
        {
            mesh.Clear();
        }

        int cornerSegs = 6;
        int totalPerim = cornerSegs * 4;

        Vector2[] p0 = BuildRoundedPerimeter(W - 2 * B, H - 2 * B, R - B, cornerSegs);
        Vector2[] p1 = BuildRoundedPerimeter(W, H, R, cornerSegs);
        Vector2[] p2 = BuildRoundedPerimeter(W, H, R, cornerSegs);
        Vector2[] p3 = BuildRoundedPerimeter(W - 2 * B, H - 2 * B, R - B, cornerSegs);

        var verts = new List<Vector3>();
        var tris = new List<int>();

        // Layer 0: Front chamfer start (Z = 0)
        for (int i = 0; i < totalPerim; i++) verts.Add(new Vector3(p0[i].x, p0[i].y, 0f));
        // Layer 1: Front edge (Z = B)
        for (int i = 0; i < totalPerim; i++) verts.Add(new Vector3(p1[i].x, p1[i].y, B));
        // Layer 2: Rear edge (Z = D - B)
        for (int i = 0; i < totalPerim; i++) verts.Add(new Vector3(p2[i].x, p2[i].y, D - B));
        // Layer 3: Rear chamfer end (Z = D)
        for (int i = 0; i < totalPerim; i++) verts.Add(new Vector3(p3[i].x, p3[i].y, D));

        // Stitch quads between layers with outward-facing normals
        for (int layer = 0; layer < 3; layer++)
        {
            int o1 = layer * totalPerim;
            int o2 = (layer + 1) * totalPerim;
            for (int i = 0; i < totalPerim; i++)
            {
                int next = (i + 1) % totalPerim;
                tris.Add(o1 + i); tris.Add(o2 + i); tris.Add(o2 + next);
                tris.Add(o1 + i); tris.Add(o2 + next); tris.Add(o1 + next);
            }
        }

        // Solid Back Face cap (Layer 3)
        int backCenterIdx = verts.Count;
        verts.Add(new Vector3(0, 0, D));
        int o3 = 3 * totalPerim;
        for (int i = 0; i < totalPerim; i++)
        {
            int next = (i + 1) % totalPerim;
            tris.Add(backCenterIdx); tris.Add(o3 + i); tris.Add(o3 + next);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (isNew)
        {
            AssetDatabase.CreateAsset(mesh, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(mesh);
        }

        return mesh;
    }

    private static Mesh GetOrCreateBezelMesh(string assetPath, float outerW, float outerH, float innerW, float innerH, float outerR, float innerR, float depth)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        bool isNew = false;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "SpatialBezelMesh";
            isNew = true;
        }
        else
        {
            mesh.Clear();
        }

        int cornerSegs = 6;
        int totalPerim = cornerSegs * 4;

        Vector2[] pOuter = BuildRoundedPerimeter(outerW, outerH, outerR, cornerSegs);
        Vector2[] pInner = BuildRoundedPerimeter(innerW, innerH, innerR, cornerSegs);

        var verts = new List<Vector3>();
        var tris = new List<int>();

        // Outer front (Z = -2)
        for (int i = 0; i < totalPerim; i++) verts.Add(new Vector3(pOuter[i].x, pOuter[i].y, -2f));
        // Inner front (Z = 0)
        for (int i = 0; i < totalPerim; i++) verts.Add(new Vector3(pInner[i].x, pInner[i].y, 0f));
        // Outer back (Z = depth)
        for (int i = 0; i < totalPerim; i++) verts.Add(new Vector3(pOuter[i].x, pOuter[i].y, depth));

        // Quads between outer front and inner front (slanted front bezel face)
        for (int i = 0; i < totalPerim; i++)
        {
            int next = (i + 1) % totalPerim;
            int oFront = i;
            int iFront = totalPerim + i;
            int oFrontNext = next;
            int iFrontNext = totalPerim + next;

            tris.Add(oFront); tris.Add(oFrontNext); tris.Add(iFrontNext);
            tris.Add(oFront); tris.Add(iFrontNext); tris.Add(iFront);
        }

        // Quads along outer rim (Z = -2 to Z = depth)
        for (int i = 0; i < totalPerim; i++)
        {
            int next = (i + 1) % totalPerim;
            int oFront = i;
            int oBack = 2 * totalPerim + i;
            int oFrontNext = next;
            int oBackNext = 2 * totalPerim + next;

            tris.Add(oFront); tris.Add(oBack); tris.Add(oBackNext);
            tris.Add(oFront); tris.Add(oBackNext); tris.Add(oFrontNext);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (isNew)
        {
            AssetDatabase.CreateAsset(mesh, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(mesh);
        }

        return mesh;
    }

    private static Vector2[] BuildRoundedPerimeter(float w, float h, float r, int cornerSegs)
    {
        int total = cornerSegs * 4;
        var pts = new Vector2[total];
        float hw = Mathf.Max(0, w * 0.5f - r);
        float hh = Mathf.Max(0, h * 0.5f - r);
        int idx = 0;

        Vector2[] centers = {
            new Vector2(hw, hh),   // Top-Right
            new Vector2(-hw, hh),  // Top-Left
            new Vector2(-hw, -hh), // Bottom-Left
            new Vector2(hw, -hh)   // Bottom-Right
        };
        float[] startAngles = { 0f, 90f, 180f, 270f };

        for (int c = 0; c < 4; c++)
        {
            for (int s = 0; s < cornerSegs; s++)
            {
                float angle = (startAngles[c] + s * (90f / cornerSegs)) * Mathf.Deg2Rad;
                pts[idx++] = centers[c] + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
            }
        }
        return pts;
    }

    private static Font ResolveFont()
    {
        Font inter = AssetDatabase.LoadAssetAtPath<Font>("Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular.ttf");
        if (inter != null) return inter;

        Font legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (legacy != null) return legacy;

        Font arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (arial != null) return arial;

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    private static void CreateQuadrantBacking(GameObject parent, string name, float x, float y, float w, float h, Sprite roundedSprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.06f, 0.12f, 0.16f, 0.55f); // Soft translucent quadrant glass
        if (roundedSprite != null)
        {
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
        }
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
