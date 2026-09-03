using UnityEngine;
using UnityEngine.UI;

public class SpeciesDetailView : MonoBehaviour
{
    public Text commonNameText;
    public Text scientificNameText;
    public Text familyText;
    public Text nativeRegionText;
    public Text descriptionText;

    public void Populate(SpeciesData data)
    {
        if (commonNameText != null) commonNameText.text = data.commonName;
        if (scientificNameText != null) scientificNameText.text = data.scientificName;
        if (familyText != null) familyText.text = "Family: " + data.family;
        if (nativeRegionText != null) nativeRegionText.text = "Native Region: " + data.nativeRegion;
        if (descriptionText != null) descriptionText.text = data.description;
    }
}
