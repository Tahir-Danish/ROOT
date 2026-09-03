using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DemoTapSelection : MonoBehaviour
{
    [Tooltip("Reference to the AR UX Manager to handle the resulting UI spawn.")]
    public ARUXManager uxManager;

    [Tooltip("The deterministic species ID to simulate.")]
    public string demoSpeciesId = "SP01";

    void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPosition = Pointer.current.position.ReadValue();

            // Don't register taps over UI elements
            if (EventSystem.current != null)
            {
                bool overUI = EventSystem.current.IsPointerOverGameObject();
                
                // Fallback for Touchscreen IDs
                if (!overUI && Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
                {
                    var touch = Touchscreen.current.touches[0];
                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        overUI = EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue());
                    }
                }

                if (overUI) return;
            }

            // Create a deterministic DetectionResult
            DetectionResult result = new DetectionResult
            {
                species_id = demoSpeciesId,
                confidence = 0.99f,
                status = "identified"
            };

            Debug.Log($"[DemoTapSelection] User tapped at {screenPosition}. Emitting detection for {demoSpeciesId}");

            // Forward to the UX Manager
            if (uxManager != null)
            {
                uxManager.HandleDetectionAtScreenPoint(result, screenPosition);
            }
            else
            {
                Debug.LogWarning("[DemoTapSelection] uxManager is not assigned!");
            }
        }
    }
}
