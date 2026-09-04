using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EXPERIMENTAL MOCK CODE.
/// Displays the current perception state on screen to verify transitions.
/// </summary>
public class PerceptionStateObserver : MonoBehaviour
{
    public PerceptionStateMachine stateMachine;
    public Text statusText; // Using legacy Text to avoid TMP package dependency if not already set up.

    void OnEnable()
    {
        if (stateMachine != null)
        {
            stateMachine.onStateChanged.AddListener(OnStateChanged);
            UpdateUI(stateMachine.currentState, stateMachine.currentSpeciesId);
        }
    }

    void OnDisable()
    {
        if (stateMachine != null)
        {
            stateMachine.onStateChanged.RemoveListener(OnStateChanged);
        }
    }

    private void OnStateChanged(PerceptionState state, DetectionResult result)
    {
        UpdateUI(state, result.species_id);
    }

    private void UpdateUI(PerceptionState state, string speciesId)
    {
        if (statusText == null) return;

        string display = $"State: {state}";
        if (!string.IsNullOrEmpty(speciesId))
        {
            display += $"\nSpecies: {speciesId}";
        }
        statusText.text = display;

        switch (state)
        {
            case PerceptionState.NO_TARGET:
                statusText.color = Color.white;
                break;
            case PerceptionState.CANDIDATE:
                statusText.color = Color.yellow;
                break;
            case PerceptionState.CONFIRMED:
                statusText.color = Color.green;
                break;
        }
    }
}
