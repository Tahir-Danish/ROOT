using UnityEngine;
using UnityEngine.UI;
using TeamCero.Data.SpeciesKnowledge;

/// <summary>
/// Upgraded production ARSpeciesCard presentation adhering to Spatial AR Spec v0.2.
/// Displays all six required contextual botanical fields simultaneously on a high-contrast,
/// opaque field plaque with distance-compensated scaling for clear readability at 0.5m-1.5m.
/// </summary>
public class ARSpeciesCard : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    public static event System.Action<string> OnExploreRequested;

    [Header("Navigation Context")]
    [Tooltip("The species ID currently assigned to this card.")]
    [SerializeField] private string _currentSpeciesId;
    public string CurrentSpeciesId => _currentSpeciesId;

    [Tooltip("The canonical species ID (e.g. SP005) resolved via SpeciesClassMapping.")]
    [SerializeField] private string _canonicalSpeciesId = "";
    public string CanonicalSpeciesId => _canonicalSpeciesId;

    /// <summary>
    /// The authoritative SpeciesKnowledgeRecord currently bound to this card.
    /// Exposes all root fields (threats, actions, notes, sources, etc.) for UI and extensions.
    /// </summary>
    public TeamCero.Data.SpeciesKnowledge.SpeciesKnowledgeRecord CurrentRecord { get; private set; }

    [Header("Anchor-Lifetime Identity Lock")]
    [Tooltip("Whether the confirmed species identity is permanently locked for the lifetime of the AR anchor.")]
    [SerializeField] private bool _isIdentityLocked = false;
    [SerializeField] private string _lockedSpeciesId = "";

    public static float? TimeOverride = null;
    private static float CurrentTime => TimeOverride ?? Time.time;

    private string _lastIgnoredCandidate = "";
    private float _lastIgnoreLogTime = -1f;

    public bool IsIdentityLocked => _isIdentityLocked;
    public string LockedSpeciesId => _lockedSpeciesId;

    public void UnlockIdentity()
    {
        _isIdentityLocked = false;
        _lockedSpeciesId = "";
    }

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

    [Header("Action Controls")]
    [Tooltip("Explore button on the card plaque.")]
    public Button exploreButton;

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
        if (exploreButton != null)
        {
            exploreButton.onClick.AddListener(Explore);
        }
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

        Canvas plaqueCanvas = GetComponentInChildren<Canvas>();
        if (plaqueCanvas != null && plaqueCanvas.renderMode == RenderMode.WorldSpace && plaqueCanvas.worldCamera == null)
        {
            plaqueCanvas.worldCamera = _mainCamera;
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
    /// Implements temporal identity lock: while locked, botanical fields and species ID
    /// cannot be overwritten by subsequent classifier fluctuations.
    /// </summary>
    public void PopulateData(string speciesId)
    {
        // 1. If currently locked, prevent overwriting confirmed identity
        if (IsIdentityLocked)
        {
            if (speciesId != _lockedSpeciesId)
            {
                if (_lastIgnoredCandidate != speciesId || (CurrentTime - _lastIgnoreLogTime > 1.0f))
                {
                    Debug.Log($"[IdentityLock] IGNORE candidate={speciesId} locked={_lockedSpeciesId}");
                    _lastIgnoredCandidate = speciesId;
                    _lastIgnoreLogTime = CurrentTime;
                }
                return;
            }
        }

        // 2. Stable card policy: never degrade an already confirmed known species to unknown/empty
        if (!string.IsNullOrEmpty(_lockedSpeciesId) && _lockedSpeciesId != "unknown_plant")
        {
            if (string.IsNullOrEmpty(speciesId) || speciesId == "unknown_plant")
            {
                if (_lastIgnoredCandidate != speciesId || (CurrentTime - _lastIgnoreLogTime > 1.0f))
                {
                    Debug.Log($"[IdentityLock] IGNORE candidate={speciesId} locked={_lockedSpeciesId}");
                    _lastIgnoredCandidate = speciesId;
                    _lastIgnoreLogTime = CurrentTime;
                }
                return;
            }
        }

        _currentSpeciesId = speciesId;
        _canonicalSpeciesId = SpeciesKnowledgeAdapter.ResolveCanonicalId(speciesId);

        bool isAuthoritativeKnown = SpeciesKnowledgeAdapter.TryGetRecordForSpecies(speciesId, out SpeciesKnowledgeRecord record);
        CurrentRecord = record;

        if (isAuthoritativeKnown && record != null)
        {
            // Confirmed identification from authoritative knowledge layer
            SetStatusBadge("● IDENTIFIED", ColorIdentifiedGreen, ColorIdentifiedBadgeBg);

            string primaryCommonName = (record.common_names != null && record.common_names.Count > 0)
                ? record.common_names[0]
                : (!string.IsNullOrEmpty(record.accepted_name) ? record.accepted_name : FormatSpeciesTitle(speciesId));

            string ecoImportance = !string.IsNullOrEmpty(record.ar_information?.ecological_importance)
                ? record.ar_information.ecological_importance
                : (!string.IsNullOrEmpty(record.ecological_importance) ? record.ecological_importance : "Data Unavailable");

            string quickId = !string.IsNullOrEmpty(record.ar_information?.quick_identification)
                ? record.ar_information.quick_identification
                : (!string.IsNullOrEmpty(record.growth_form) ? record.growth_form : "Data Unavailable");

            SetText(commonNameText, primaryCommonName);
            SetText(scientificNameText, !string.IsNullOrEmpty(record.scientific_name) ? record.scientific_name : "Plantae sp.");
            SetText(familyText, !string.IsNullOrEmpty(record.family) ? record.family : "Data Unavailable");
            SetText(nativeRegionText, !string.IsNullOrEmpty(record.native_region) ? record.native_region : "Data Unavailable");
            SetText(ecoImportanceText, ecoImportance);
            SetText(conservationStatusText, !string.IsNullOrEmpty(record.conservation_status) ? record.conservation_status : "Data Unavailable");

            // Backward compatibility
            SetText(expandedCommonNameText, primaryCommonName);
            SetText(descriptionText, quickId);

            // Engage identity lock for confirmed species (immutable for anchor lifetime)
            _lockedSpeciesId = speciesId;
            _isIdentityLocked = true;
            Debug.Log($"[IdentityLock] LOCK species={speciesId} (canonical={_canonicalSpeciesId})");
        }
        else if (speciesId == "unknown_plant" || _canonicalSpeciesId == SpeciesClassMapping.UnknownSpeciesId)
        {
            // Unconfirmed / Unknown target - DO NOT LOCK
            _lockedSpeciesId = "";
            _isIdentityLocked = false;

            SetStatusBadge("● UNCONFIRMED TARGET", ColorUnknownAmber, ColorUnknownBadgeBg);

            SetText(commonNameText, "Unknown Species");
            SetText(scientificNameText, "Data Unavailable");
            SetText(familyText, "Data Unavailable");
            SetText(nativeRegionText, "Data Unavailable");
            SetText(ecoImportanceText, "Identification pending.");
            SetText(conservationStatusText, "Identification Pending");

            SetText(expandedCommonNameText, "Unknown Species");
            SetText(descriptionText, "Target under observation. Optical characteristics being analyzed.");
        }
        else
        {
            // Unknown fallback / missing knowledge record - DO NOT LOCK
            _lockedSpeciesId = "";
            _isIdentityLocked = false;

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

        if (AppNavigationShell.Instance != null)
        {
            AppNavigationShell.Instance.NotifyActiveCardAvailable(speciesId);
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
        Explore();
    }

    /// <summary>
    /// Opens the Plant Details screen using the locked canonical species context
    /// without triggering a new ML classification.
    /// </summary>
    public void Explore()
    {
        string targetSpeciesId = !string.IsNullOrEmpty(_canonicalSpeciesId) && _canonicalSpeciesId != "UNKNOWN"
            ? _canonicalSpeciesId
            : (!string.IsNullOrEmpty(_lockedSpeciesId) ? _lockedSpeciesId : _currentSpeciesId);

        if (string.IsNullOrEmpty(targetSpeciesId))
        {
            targetSpeciesId = "unknown_plant";
        }

        string canonicalId = SpeciesKnowledgeAdapter.ResolveCanonicalId(targetSpeciesId);

        Debug.Log($"[ARSpeciesCard] Card Explore invoked -> navigating to Plant Details with locked canonical species: '{canonicalId}' (raw: '{targetSpeciesId}')");

        OnExploreRequested?.Invoke(canonicalId);

        if (AppNavigationShell.Instance != null && AppNavigationShell.Instance.CurrentScreen != AppScreenState.PlantDetails)
        {
            AppNavigationShell.Instance.NavigateToPlantDetails(canonicalId);
        }
    }

    public void CloseCard()
    {
        if (AppNavigationShell.Instance != null)
        {
            AppNavigationShell.Instance.DismissActiveCardBanner();
        }

        if (_uxManager != null)
        {
            _uxManager.DismissCard(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        Debug.Log($"[ARSpeciesCard] OnPointerClick on plaque. Species: {_currentSpeciesId}");
        Explore();
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
