using UnityEngine;

public class AutoARCardTrigger : MonoBehaviour
{
    public PerceptionStateMachine stateMachine;
    public ARUXManager uxManager;

    void Start()
    {
        Debug.Log("[UIBRIDGE] AutoCardStarted");
        if (stateMachine != null)
        {
            stateMachine.onStateChanged.AddListener(OnStateChanged);
        }
    }

    private void OnStateChanged(PerceptionState state, DetectionResult result)
    {
        Debug.Log($"[UIBRIDGE] StateChanged received in AutoCardTrigger: {state}");
        if (uxManager == null) return;

        if (state == PerceptionState.CONFIRMED && result.hasBoundingBox)
        {
            Debug.Log($"[UIBRIDGE] AutoCardTriggered: triggering ARUXManager for {result.species_id}");
            // Coordinate Space:
            // Input: result.boundingBox.center (pixels in Unity Screen space, bottom-left origin)
            uxManager.HandleDetectionAtScreenPoint(result, result.boundingBox.center);
        }
        else if (state == PerceptionState.NO_TARGET || state == PerceptionState.UNKNOWN_TARGET)
        {
            uxManager.DismissActiveCard();
        }
    }

    void OnDestroy()
    {
        if (stateMachine != null)
        {
            stateMachine.onStateChanged.RemoveListener(OnStateChanged);
        }
    }
}
