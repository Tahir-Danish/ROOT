using UnityEngine;

public class AutoARCardTrigger : MonoBehaviour
{
    public PerceptionStateMachine stateMachine;
    public ARUXManager uxManager;
    public Pv002Pipeline pipeline;

    void Start()
    {
        Debug.Log("[UIBRIDGE] AutoCardStarted");
        if (stateMachine == null)
        {
            stateMachine = Object.FindAnyObjectByType<PerceptionStateMachine>();
        }
        if (stateMachine != null)
        {
            stateMachine.onStateChanged.AddListener(OnStateChanged);
        }

        if (uxManager == null)
        {
            uxManager = Object.FindAnyObjectByType<ARUXManager>();
        }

        if (pipeline == null)
        {
            pipeline = Object.FindAnyObjectByType<Pv002Pipeline>();
        }
        if (pipeline != null)
        {
            if (pipeline.onDetectionUpdated == null)
            {
                pipeline.onDetectionUpdated = new Pv002Pipeline.DetectionUpdateEvent();
            }
            pipeline.onDetectionUpdated.AddListener(OnDetectionUpdated);
        }
    }

    private PerceptionState _lastTriggerState = PerceptionState.NO_TARGET;
    private float _lastTriggerTime = -1f;

    private void OnStateChanged(PerceptionState state, DetectionResult result)
    {
        Debug.Log($"[UIBRIDGE] StateChanged received in AutoCardTrigger: {state}");
        if (uxManager == null) return;

        // Both CONFIRMED and UNKNOWN_TARGET are valid triggers for the spatial AR card
        if ((state == PerceptionState.CONFIRMED || state == PerceptionState.UNKNOWN_TARGET) && result.hasBoundingBox)
        {
            TriggerSpatialAcquisition(result, state);
        }
        // Invariant: Established AR anchors/cards are persistent for the session.
        // Moving the camera away or entering NO_TARGET MUST NOT dismiss or destroy existing cards.
    }

    private void OnDetectionUpdated(DetectionResult result)
    {
        if (uxManager == null || stateMachine == null) return;

        PerceptionState state = stateMachine.currentState;
        // While in CONFIRMED or UNKNOWN_TARGET state:
        // - Raycast checks spatial deduplication against existing anchors.
        // - If within threshold, content is updated without moving world position.
        // - If new distinct target, genuine ARAnchor and card are created.
        if ((state == PerceptionState.CONFIRMED || state == PerceptionState.UNKNOWN_TARGET) && result.hasBoundingBox)
        {
            TriggerSpatialAcquisition(result, state);
        }
    }

    private void TriggerSpatialAcquisition(DetectionResult result, PerceptionState state)
    {
        // Coordinate Space:
        // Input: result.boundingBox (pixels in Unity Screen space, bottom-left origin: 0..Screen.width, 0..Screen.height)
        // Preferred spatial reference: BOTTOM-CENTER of detection bbox
        Vector2 bottomCenter = new Vector2(result.boundingBox.center.x, result.boundingBox.yMin);

        // Sparse diagnostics at decision boundary (avoid per-frame log spam):
        if (uxManager != null)
        {
            if (state != _lastTriggerState || (Time.time - _lastTriggerTime > 1.0f))
            {
                Debug.Log($"[UIBRIDGE] ARUXTriggered: state={state} screen={bottomCenter}");
                _lastTriggerState = state;
                _lastTriggerTime = Time.time;
            }
        }

        uxManager.HandleDetectionAtScreenPoint(result, bottomCenter, state);
    }

    void OnDestroy()
    {
        if (stateMachine != null)
        {
            stateMachine.onStateChanged.RemoveListener(OnStateChanged);
        }
        if (pipeline != null && pipeline.onDetectionUpdated != null)
        {
            pipeline.onDetectionUpdated.RemoveListener(OnDetectionUpdated);
        }
    }
}
