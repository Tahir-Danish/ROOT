using UnityEngine;
using UnityEngine.UI;

public class ConservationView : MonoBehaviour
{
    public Text statusText;
    public Text threatsText;
    public Text bestPracticesText;
    
    public void Populate(SpeciesData data)
    {
        if (statusText != null) 
            statusText.text = "Status: " + (string.IsNullOrEmpty(data.conservationStatus) ? "Unknown" : data.conservationStatus);
            
        // The data layer for threats and best practices is intentionally not populated with mock data yet.
        // We leave them as placeholders per the requirements.
        if (threatsText != null) 
            threatsText.text = "Threats:\nData unavailable for this species.";
            
        if (bestPracticesText != null) 
            bestPracticesText.text = "Best Practices:\nData unavailable for this species.";
    }
}
