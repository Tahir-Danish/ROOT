using UnityEngine;
using UnityEngine.UI;

public class EcologyView : MonoBehaviour
{
    public Text ecologicalImportanceText;
    
    public void Populate(SpeciesData data)
    {
        if (ecologicalImportanceText != null) 
            ecologicalImportanceText.text = string.IsNullOrEmpty(data.ecologicalImportance) ? "Data unavailable for this species." : data.ecologicalImportance;
    }
}
