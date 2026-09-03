using UnityEngine;
using UnityEngine.UI;

public class ARSpeciesCard : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject collapsedView;
    public GameObject expandedView;

    [Header("Collapsed View Text Fields")]
    public Text commonNameText;
    public Text scientificNameText;
    public Text familyText;
    public Text nativeRegionText;

    [Header("Expanded View Text Fields")]
    public Text expandedCommonNameText;
    public Text descriptionText;
    public Text ecoImportanceText;
    public Text conservationStatusText;

    [Header("Presentation")]
    [Tooltip("The base physical scale of the card.")]
    public float baseScale = 0.00035f;
    public float minScale = 0.00015f;
    public float maxScale = 0.001f;

    private bool _isExpanded = false;
    private Camera _mainCamera;
    private ARUXManager _uxManager;

    void Awake()
    {
        _mainCamera = Camera.main;
        if (collapsedView != null) collapsedView.SetActive(true);
        if (expandedView != null) expandedView.SetActive(false);
    }

    void Update()
    {
        if (_mainCamera != null)
        {
            float distance = Vector3.Distance(transform.position, _mainCamera.transform.position);
            float targetScale = Mathf.Clamp(distance * baseScale, minScale, maxScale);
            transform.localScale = Vector3.one * targetScale;
        }
    }
    
    public void Initialize(ARUXManager uxManager)
    {
        _uxManager = uxManager;
    }

    public void PopulateData(string speciesId)
    {
        if (MockSpeciesDatabase.TryGetSpeciesData(speciesId, out SpeciesData data))
        {
            SetText(commonNameText, data.commonName);
            SetText(scientificNameText, data.scientificName);
            SetText(familyText, data.family);
            SetText(nativeRegionText, data.nativeRegion);

            SetText(expandedCommonNameText, data.commonName);
            SetText(descriptionText, data.description);
            SetText(ecoImportanceText, data.ecologicalImportance);
            SetText(conservationStatusText, data.conservationStatus);
        }
        else
        {
            SetText(commonNameText, "Unknown Species");
            SetText(scientificNameText, "N/A");
            SetText(familyText, "N/A");
            SetText(nativeRegionText, "N/A");
        }

        _isExpanded = false;
        UpdateView();
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
            _uxManager.DismissActiveCard();
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
            textComponent.text = value;
        }
    }
}
