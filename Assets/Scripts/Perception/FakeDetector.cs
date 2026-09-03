using UnityEngine;

/// <summary>
/// EXPERIMENTAL MOCK CODE.
/// This replaces the real Sentis model inference temporarily to prove the state machine.
/// </summary>
public class FakeDetector : MonoBehaviour
{
    public PerceptionStateMachine stateMachine;
    
    [Header("Fake ML Result to emit every frame")]
    public string fakeSpeciesId = "SP01";
    [Range(0f, 1f)]
    public float fakeConfidence = 0.90f;
    public bool isDetecting = false;

    void Update()
    {
        if (stateMachine == null) return;

        if (isDetecting)
        {
            stateMachine.SubmitDetection(new DetectionResult
            {
                species_id = fakeSpeciesId,
                confidence = fakeConfidence,
                status = "identified"
            });
        }
        else
        {
            stateMachine.SubmitDetection(new DetectionResult
            {
                species_id = "",
                confidence = 0f,
                status = "unrecognized"
            });
        }
    }
}
