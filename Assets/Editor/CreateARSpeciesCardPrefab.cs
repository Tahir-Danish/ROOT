using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class CreateARSpeciesCardPrefab
{
    [MenuItem("Tools/Rebuild AR Species Card Prefab")]
    public static void CreatePrefab()
    {
        string path = "Assets/Prefabs/UI/ARSpeciesCard.prefab";

        GameObject root = new GameObject("ARSpeciesCard");
        var billboard = root.AddComponent<Billboard>();
        var speciesCard = root.AddComponent<ARSpeciesCard>();

        // --- MUSEUM PLAQUE STEM (Instead of plain line) ---
        // A polished tapered cylinder to visually ground the plaque
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "PlaqueStem";
        stem.transform.SetParent(root.transform, false);
        // Base is at -0.05y, top is at 0. So center is -0.025y.
        stem.transform.localPosition = new Vector3(0, -0.025f, 0);
        // Scale: cylinder default is 2 units tall, 1 unit wide.
        // We want it 0.05m tall, so y scale = 0.025. Width = 0.002.
        stem.transform.localScale = new Vector3(0.004f, 0.025f, 0.004f);
        
        Material stemMat = new Material(Shader.Find("Standard"));
        stemMat.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        stemMat.SetFloat("_Metallic", 0.5f);
        stemMat.SetFloat("_Glossiness", 0.5f);
        stem.GetComponent<MeshRenderer>().material = stemMat;
        Object.DestroyImmediate(stem.GetComponent<Collider>());
        
        // --- CANVAS SETUP ---
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasScaler = root.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 10;
        root.AddComponent<GraphicRaycaster>(); // REQUIRED for UI interaction
        
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 150);

        // --- COLLAPSED VIEW ---
        GameObject collapsed = new GameObject("CollapsedView");
        collapsed.transform.SetParent(root.transform, false);
        var collRt = collapsed.AddComponent<RectTransform>();
        collRt.anchorMin = Vector2.zero; collRt.anchorMax = Vector2.one;
        collRt.offsetMin = Vector2.zero; collRt.offsetMax = Vector2.zero;
        
        var collImage = collapsed.AddComponent<Image>();
        // Slightly rounded corner aesthetic (using standard sprite if available, otherwise solid)
        collImage.color = new Color(0.12f, 0.15f, 0.12f, 0.95f); // Dark botanical green

        // Content Container to push it forward in Z space for depth
        GameObject collContent = new GameObject("Content");
        collContent.transform.SetParent(collapsed.transform, false);
        var collContentRt = collContent.AddComponent<RectTransform>();
        collContentRt.anchorMin = Vector2.zero; collContentRt.anchorMax = Vector2.one;
        collContentRt.offsetMin = Vector2.zero; collContentRt.offsetMax = Vector2.zero;
        collContentRt.anchoredPosition3D = new Vector3(0, 0, -2f); // 2 units forward

        // Header
        Text headerLabel = CreateText(collContent, "HeaderLabel", "IDENTIFIED", 10, FontStyle.Bold, TextAnchor.UpperLeft);
        headerLabel.color = new Color(0.5f, 0.9f, 0.5f);
        SetRect(headerLabel, 15, -15, 100, 15);

        // Common Name
        Text commonName = CreateText(collContent, "CommonName", "Common Name", 22, FontStyle.Bold, TextAnchor.UpperLeft);
        SetRect(commonName, 15, -35, 270, 30);

        // Scientific Name
        Text scientificName = CreateText(collContent, "ScientificName", "Scientific Name", 14, FontStyle.Italic, TextAnchor.UpperLeft);
        scientificName.color = new Color(0.7f, 0.7f, 0.7f);
        SetRect(scientificName, 15, -65, 270, 20);

        // Family / Region
        Text familyRegion = CreateText(collContent, "FamilyRegion", "Family", 11, FontStyle.Normal, TextAnchor.UpperLeft);
        familyRegion.color = new Color(0.6f, 0.6f, 0.6f);
        SetRect(familyRegion, 15, -95, 270, 15);
        
        Text nativeRegion = CreateText(collContent, "NativeRegion", "Region", 11, FontStyle.Normal, TextAnchor.UpperLeft);
        nativeRegion.color = new Color(0.6f, 0.6f, 0.6f);
        SetRect(nativeRegion, 15, -110, 270, 15);

        // Explore Button
        GameObject exploreBtn = CreateButton(collContent, "ExploreButton", "Explore", out Text exploreText);
        exploreBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 1f); // Green accent
        var expRt = exploreBtn.GetComponent<RectTransform>();
        expRt.anchorMin = new Vector2(1, 0); expRt.anchorMax = new Vector2(1, 0);
        expRt.pivot = new Vector2(1, 0);
        expRt.anchoredPosition = new Vector2(-15, 15);
        expRt.sizeDelta = new Vector2(80, 30);

        // --- EXPANDED VIEW ---
        GameObject expanded = new GameObject("ExpandedView");
        expanded.transform.SetParent(root.transform, false);
        var expViewRt = expanded.AddComponent<RectTransform>();
        expViewRt.anchorMin = Vector2.zero; expViewRt.anchorMax = Vector2.one;
        expViewRt.offsetMin = new Vector2(0, 0); expViewRt.offsetMax = new Vector2(0, 180); // taller
        
        var expImage = expanded.AddComponent<Image>();
        expImage.color = new Color(0.12f, 0.15f, 0.12f, 0.98f);

        GameObject expContent = new GameObject("Content");
        expContent.transform.SetParent(expanded.transform, false);
        var expContentRt = expContent.AddComponent<RectTransform>();
        expContentRt.anchorMin = Vector2.zero; expContentRt.anchorMax = Vector2.one;
        expContentRt.offsetMin = Vector2.zero; expContentRt.offsetMax = Vector2.zero;
        expContentRt.anchoredPosition3D = new Vector3(0, 0, -2f);

        // Title
        Text expCommonName = CreateText(expContent, "ExpCommonName", "Common Name", 18, FontStyle.Bold, TextAnchor.UpperCenter);
        expCommonName.color = new Color(0.5f, 0.9f, 0.5f);
        SetRect(expCommonName, 15, -15, 270, 25);

        // About Label
        Text aboutLabel = CreateText(expContent, "AboutLabel", "ABOUT", 10, FontStyle.Bold, TextAnchor.UpperLeft);
        aboutLabel.color = new Color(0.6f, 0.6f, 0.6f);
        SetRect(aboutLabel, 15, -50, 270, 15);
        
        Text description = CreateText(expContent, "Description", "Description...", 11, FontStyle.Normal, TextAnchor.UpperLeft);
        SetRect(description, 15, -65, 270, 60);

        // Ecology Label
        Text ecoLabel = CreateText(expContent, "EcoLabel", "ECOLOGY", 10, FontStyle.Bold, TextAnchor.UpperLeft);
        ecoLabel.color = new Color(0.6f, 0.6f, 0.6f);
        SetRect(ecoLabel, 15, -135, 270, 15);

        Text ecoText = CreateText(expContent, "EcoText", "Ecology...", 11, FontStyle.Normal, TextAnchor.UpperLeft);
        SetRect(ecoText, 15, -150, 270, 60);
        
        // Conservation Label
        Text consLabel = CreateText(expContent, "ConsLabel", "CONSERVATION", 10, FontStyle.Bold, TextAnchor.UpperLeft);
        consLabel.color = new Color(0.6f, 0.6f, 0.6f);
        SetRect(consLabel, 15, -220, 270, 15);

        Text consText = CreateText(expContent, "ConsText", "Conservation...", 11, FontStyle.Normal, TextAnchor.UpperLeft);
        SetRect(consText, 15, -235, 270, 40);

        // Close Button
        GameObject closeBtn = CreateButton(expContent, "CloseButton", "Close", out Text closeBtnText);
        closeBtn.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        var clsRt = closeBtn.GetComponent<RectTransform>();
        clsRt.anchorMin = new Vector2(1, 0); clsRt.anchorMax = new Vector2(1, 0);
        clsRt.pivot = new Vector2(1, 0);
        clsRt.anchoredPosition = new Vector2(-15, 15);
        clsRt.sizeDelta = new Vector2(80, 30);

        // Dismiss Button
        GameObject dismissBtn = CreateButton(expContent, "DismissButton", "Dismiss Card", out Text dismissBtnText);
        dismissBtn.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f, 1f);
        var dsmRt = dismissBtn.GetComponent<RectTransform>();
        dsmRt.anchorMin = new Vector2(0, 0); dsmRt.anchorMax = new Vector2(0, 0);
        dsmRt.pivot = new Vector2(0, 0);
        dsmRt.anchoredPosition = new Vector2(15, 15);
        dsmRt.sizeDelta = new Vector2(100, 30);

        // Wiring
        speciesCard.collapsedView = collapsed;
        speciesCard.expandedView = expanded;
        speciesCard.commonNameText = commonName;
        speciesCard.scientificNameText = scientificName;
        speciesCard.familyText = familyRegion;
        speciesCard.nativeRegionText = nativeRegion;
        
        speciesCard.expandedCommonNameText = expCommonName;
        speciesCard.descriptionText = description;
        speciesCard.ecoImportanceText = ecoText;
        speciesCard.conservationStatusText = consText;

        // Button Events
        UnityEditor.Events.UnityEventTools.AddPersistentListener(exploreBtn.GetComponent<Button>().onClick, speciesCard.ToggleExplore);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.GetComponent<Button>().onClick, speciesCard.ToggleExplore);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(dismissBtn.GetComponent<Button>().onClick, speciesCard.CloseCard);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("ARSpeciesCard prefab rebuilt successfully at " + path);
    }

    private static Text CreateText(GameObject parent, string name, string defaultText, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Text txt = go.AddComponent<Text>();
        txt.text = defaultText;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

    private static GameObject CreateButton(GameObject parent, string name, string labelText, out Text textComp)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = labelText;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 12;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        
        RectTransform txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
        
        textComp = txt;
        return btnObj;
    }
}
