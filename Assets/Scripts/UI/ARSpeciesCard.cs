using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Upgraded production ARSpeciesCard presentation adhering to Spatial AR Spec v0.2.
/// Displays all six required contextual botanical fields simultaneously on a high-contrast,
/// opaque field plaque with distance-compensated scaling for clear readability at 0.5m-1.5m.
/// </summary>
public class ARSpeciesCard : MonoBehaviour
{
    [Header("Visual & Tether References")]
    [Tooltip("The floating plaque canvas/visual child that billboards and scales.")]
    public Transform plaqueVisual;

    [Tooltip("Optional reference to the 3D spatial tether component.")]
    public SpatialTether spatialTether;

    [Header("Status & Identification Badge")]
    public Text statusBadgeText;
    public Image statusBadgeBackground;

    [Header("Six Official Information Fields (Spec Section 12)")]
    [Tooltip("1. Common Name")]
    public Text commonNameText;

    [Tooltip("2. Scientific Name (Latin Binomial)")]
    public Text scientificNameText;

    [Tooltip("3. Plant Family")]
    public Text familyText;

    [Tooltip("4. Native / Native Region")]
    public Text nativeRegionText;

    [Tooltip("5. Ecological Importance")]
    public Text ecoImportanceText;

    [Tooltip("6. Conservation Status")]
    public Text conservationStatusText;

    [Header("Optional / Backward Compatibility References")]
    public Text descriptionText;
    public GameObject collapsedView;
    public GameObject expandedView;
    public Text expandedCommonNameText;

    [Header("Distance Scaling & Readability (Spec Section 13)")]
    [Tooltip("Base angular scale factor for world-space canvas.")]
    public float baseScale = 0.00040f;
    [Tooltip("Minimum physical scale at close viewing distance (0.5m).")]
    public float minScale = 0.00022f;
    [Tooltip("Maximum physical scale at farther viewing distance (1.5m).")]
    public float maxScale = 0.00065f;

    private Camera _mainCamera;
    private ARUXManager _uxManager;
    private bool _isExpanded = false;

    // High-contrast color palette for AR camera feed legibility
    private static readonly Color ColorIdentifiedGreen = new Color(0.20f, 0.83f, 0.60f, 1.0f); // #34D399 (Emerald)
    private static readonly Color ColorIdentifiedBadgeBg = new Color(0.02f, 0.31f, 0.23f, 0.85f); // Dark emerald
    private static readonly Color ColorUnknownAmber = new Color(0.98f, 0.75f, 0.14f, 1.0f); // #FBBF24 (Amber)
    private static readonly Color ColorUnknownBadgeBg = new Color(0.47f, 0.21f, 0.06f, 0.85f); // Dark amber

    void Awake()
    {
        _mainCamera = Camera.main;
        if (collapsedView != null) collapsedView.SetActive(true);
        if (expandedView != null) expandedView.SetActive(false);
    }

    void Start()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (spatialTether == null)
        {
            spatialTether = GetComponentInChildren<SpatialTether>();
        }
    }

    void Update()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            Transform targetTransform = plaqueVisual != null ? plaqueVisual : transform;
            float distance = Vector3.Distance(targetTransform.position, _mainCamera.transform.position);
            float targetScale = Mathf.Clamp(distance * baseScale, minScale, maxScale);

            if (plaqueVisual != null)
            {
                plaqueVisual.localScale = Vector3.one * targetScale;
            }
            else
            {
                transform.localScale = Vector3.one * targetScale;
            }
        }
    }

    public void Initialize(ARUXManager uxManager)
    {
        _uxManager = uxManager;

        if (spatialTether != null)
        {
            spatialTether.UpdateTetherGeometry();
        }
    }

    /// <summary>
    /// Populates all six official botanical fields simultaneously from the database.
    /// Supports both confidently identified species and unconfirmed/unknown targets.
    /// </summary>
    public void PopulateData(string speciesId)
    {
        bool isKnown = MockSpeciesDatabase.TryGetSpeciesData(speciesId, out SpeciesData data);

        if (isKnown && speciesId != "unknown_plant")
        {
            // Confirmed identification
            SetStatusBadge("● IDENTIFIED", ColorIdentifiedGreen, ColorIdentifiedBadgeBg);

            SetText(commonNameText, string.IsNullOrEmpty(data.commonName) || data.commonName == "Data Unavailable" ? FormatSpeciesTitle(speciesId) : data.commonName);
            SetText(scientificNameText, !string.IsNullOrEmpty(data.scientificName) ? data.scientificName : "Plantae sp.");
            SetText(familyText, data.family);
            SetText(nativeRegionText, data.nativeRegion);
            SetText(ecoImportanceText, data.ecologicalImportance);
            SetText(conservationStatusText, data.conservationStatus);

            // Backward compatibility
            SetText(expandedCommonNameText, data.commonName);
            SetText(descriptionText, data.description);
        }
        else if (isKnown && speciesId == "unknown_plant")
        {
            // Unconfirmed / Unknown target
            SetStatusBadge("● UNCONFIRMED TARGET", ColorUnknownAmber, ColorUnknownBadgeBg);

            SetText(commonNameText, "Unknown Species");
            SetText(scientificNameText, "Data Unavailable");
            SetText(familyText, "Data Unavailable");
            SetText(nativeRegionText, "Data Unavailable");
            SetText(ecoImportanceText, "Identification pending.");
            SetText(conservationStatusText, "Identification Pending");

            SetText(expandedCommonNameText, "Unknown Species");
            SetText(descriptionText, data.description);
        }
        else
        {
            // Unknown fallback
            SetStatusBadge("● UNCONFIRMED TARGET", ColorUnknownAmber, ColorUnknownBadgeBg);

            SetText(commonNameText, "Unknown Species");
            SetText(scientificNameText, "Data Unavailable");
            SetText(familyText, "Data Unavailable");
            SetText(nativeRegionText, "Data Unavailable");
            SetText(ecoImportanceText, "Identification pending.");
            SetText(conservationStatusText, "Identification Pending");

            SetText(expandedCommonNameText, "Unknown Species");
            SetText(descriptionText, "Data Unavailable");
        }

        _isExpanded = false;
        UpdateView();

        if (spatialTether != null)
        {
            spatialTether.UpdateTetherGeometry();
        }
    }

    private void SetStatusBadge(string label, Color textColor, Color bgColor)
    {
        if (statusBadgeText != null)
        {
            statusBadgeText.text = label;
            statusBadgeText.color = textColor;
        }

        if (statusBadgeBackground != null)
        {
            statusBadgeBackground.color = bgColor;
        }
    }

    public void ToggleExplore()
    {
        _isExpanded = !_isExpanded;
        UpdateView();
    }

    public void CloseCard()
    {
        if (_uxManager != null)
        {
            _uxManager.DismissCard(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void UpdateView()
    {
        if (collapsedView != null) collapsedView.SetActive(!_isExpanded);
        if (expandedView != null) expandedView.SetActive(_isExpanded);
    }

    private void SetText(Text textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = !string.IsNullOrEmpty(value) ? value : "Data Unavailable";
        }
    }

    private static string FormatSpeciesTitle(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId)) return "Native Plant";

        // Remove numeric prefix if present (e.g. "01_alstonia_scholaris" -> "Alstonia Scholaris")
        string clean = speciesId;
        int underscoreIdx = clean.IndexOf('_');
        if (underscoreIdx >= 0 && underscoreIdx <= 3)
        {
            clean = clean.Substring(underscoreIdx + 1);
        }

        string[] words = clean.Split('_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            }
        }

        return string.Join(" ", words);
    }
}
