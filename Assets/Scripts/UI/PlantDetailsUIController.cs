using UnityEngine;
using UnityEngine.UI;

public class PlantDetailsUIController : MonoBehaviour
{
    public Text commonNameText;
    public Text scientificNameText;
    public Text familyText;
    public Text nativeRegionText;
    public Text ecoImportanceText;
    
    public GameObject botanicalGuideModal;
    public GameObject conservationModal;
    public GameObject closeUpModal;
    public GameObject unknownStatePanel;

    private SpeciesKnowledgeRecord _currentRecord;
    private AppNavigationShell _shell;

    public void Initialize(AppNavigationShell shell)
    {
        _shell = shell;
    }

    public void OpenDetails(string speciesId)
    {
        gameObject.SetActive(true);
        TextAsset jsonAsset = Resources.Load<TextAsset>($"SpeciesKnowledge/{speciesId}");
        
        if (jsonAsset != null)
        {
            _currentRecord = JsonUtility.FromJson<SpeciesKnowledgeRecord>(jsonAsset.text);
            PopulateUI(_currentRecord);
            if (unknownStatePanel != null) unknownStatePanel.SetActive(false);
        }
        else
        {
            if (unknownStatePanel != null) unknownStatePanel.SetActive(true);
        }
    }

    private void PopulateUI(SpeciesKnowledgeRecord record)
    {
        if (commonNameText) commonNameText.text = record.common_name;
        if (scientificNameText) scientificNameText.text = record.scientific_name;
        if (familyText) familyText.text = record.family;
        if (nativeRegionText) nativeRegionText.text = record.native_region;
        if (ecoImportanceText) ecoImportanceText.text = record.ecological_importance;
    }

    public void OpenBotanicalGuide()
    {
        if (botanicalGuideModal != null) botanicalGuideModal.SetActive(true);
    }

    public void CloseBotanicalGuide()
    {
        if (botanicalGuideModal != null) botanicalGuideModal.SetActive(false);
    }

    public void OpenConservation()
    {
        if (conservationModal != null) conservationModal.SetActive(true);
    }

    public void CloseConservation()
    {
        if (conservationModal != null) conservationModal.SetActive(false);
    }

    public void CloseDetails()
    {
        gameObject.SetActive(false);
        _shell.GoBackToExplore();
    }
}
