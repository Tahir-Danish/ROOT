using UnityEngine;
using UnityEngine.Events;

public enum PerceptionState
{
    NO_TARGET,
    CANDIDATE,
    CONFIRMED
}

[System.Serializable]
public struct DetectionResult
{
    public string species_id;
    public float confidence;
    public string status; // "identified", "uncertain", "unrecognized"
}

public class PerceptionStateMachine : MonoBehaviour
{
    [Tooltip("Seconds a target must be continuously seen to become CONFIRMED.")]
    public float candidateDurationRequired = 0.5f; 
    
    [Tooltip("Seconds a CONFIRMED target can be missing before reverting to NO_TARGET.")]
    public float lostToleranceDuration = 1.0f; 
    
    [Tooltip("Minimum confidence required to consider the detection valid.")]
    public float confidenceThreshold = 0.75f;

    [Header("Runtime State")]
    public PerceptionState currentState = PerceptionState.NO_TARGET;
    public string currentSpeciesId = "";

    [System.Serializable]
    public class StateChangeEvent : UnityEvent<PerceptionState, string> { }
    
    public StateChangeEvent onStateChanged;

    private float _timeInCurrentState = 0f;
    private float _timeSinceLastValidDetection = 0f;
    private DetectionResult _latestResult;

    public void SubmitDetection(DetectionResult result)
    {
        _latestResult = result;
    }

    void Update()
    {
        bool isValidTarget = _latestResult.confidence >= confidenceThreshold && _latestResult.status == "identified";

        _timeInCurrentState += Time.deltaTime;

        if (isValidTarget)
        {
            _timeSinceLastValidDetection = 0f;
        }
        else
        {
            _timeSinceLastValidDetection += Time.deltaTime;
        }

        switch (currentState)
        {
            case PerceptionState.NO_TARGET:
                if (isValidTarget)
                {
                    TransitionTo(PerceptionState.CANDIDATE, _latestResult.species_id);
                }
                break;

            case PerceptionState.CANDIDATE:
                if (_timeSinceLastValidDetection > lostToleranceDuration)
                {
                    // Target lost before confirmation
                    TransitionTo(PerceptionState.NO_TARGET, "");
                }
                else if (isValidTarget)
                {
                    if (_latestResult.species_id == currentSpeciesId)
                    {
                        if (_timeInCurrentState >= candidateDurationRequired)
                        {
                            TransitionTo(PerceptionState.CONFIRMED, currentSpeciesId);
                        }
                    }
                    else
                    {
                        // Target changed abruptly during candidate phase
                        TransitionTo(PerceptionState.CANDIDATE, _latestResult.species_id);
                    }
                }
                break;

            case PerceptionState.CONFIRMED:
                if (_timeSinceLastValidDetection > lostToleranceDuration)
                {
                    TransitionTo(PerceptionState.NO_TARGET, "");
                }
                else if (isValidTarget && _latestResult.species_id != currentSpeciesId)
                {
                    // New target appeared while holding confirmed state of another.
                    TransitionTo(PerceptionState.CANDIDATE, _latestResult.species_id);
                }
                break;
        }
        
        // Mark current result as old by dropping its validity 
        // Real systems will push new ones. If none is pushed, timeSinceLastValidDetection increases.
        _latestResult.status = "unrecognized";
    }

    private void TransitionTo(PerceptionState newState, string speciesId)
    {
        if (currentState == newState && currentSpeciesId == speciesId) return;

        Debug.Log($"[PerceptionStateMachine] {currentState} -> {newState} ({speciesId})");
        currentState = newState;
        currentSpeciesId = speciesId;
        _timeInCurrentState = 0f;
        
        onStateChanged?.Invoke(currentState, currentSpeciesId);
    }
}
