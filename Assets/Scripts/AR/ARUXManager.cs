using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARUXManager : MonoBehaviour
{
    [Tooltip("The raycast manager attached to the XR Origin.")]
    public ARRaycastManager raycastManager;

    [Tooltip("The prefab for the AR species card UI.")]
    public GameObject arSpeciesCardPrefab;

    [Tooltip("Vertical offset from the tapped surface to prevent clipping.")]
    public float verticalOffset = 0.05f;

    private GameObject _currentActiveCard;
    private ARSpeciesCard _currentCardComponent;

    // A list to store the results of the raycast
    private List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();

    void Start()
    {
        if (raycastManager == null)
        {
            raycastManager = UnityEngine.Object.FindAnyObjectByType<ARRaycastManager>();
            if (raycastManager == null)
            {
                Debug.LogError("[ARUXManager] ARRaycastManager not found. Ensure XR Origin has an ARRaycastManager attached.");
            }
        }
    }

    /// <summary>
    /// Processes a detection result associated with a specific screen coordinate.
    /// </summary>
    public void HandleDetectionAtScreenPoint(DetectionResult result, Vector2 screenPoint)
    {
        if (raycastManager == null) return;

        // Perform raycast against AR planes
        if (raycastManager.Raycast(screenPoint, _raycastHits, TrackableType.PlaneWithinPolygon))
        {
            // Get the closest hit
            Pose hitPose = _raycastHits[0].pose;
            Vector3 targetPosition = hitPose.position + (Vector3.up * verticalOffset);

            if (_currentActiveCard == null)
            {
                // Instantiate a new card
                _currentActiveCard = Instantiate(arSpeciesCardPrefab, targetPosition, Quaternion.identity);
                _currentCardComponent = _currentActiveCard.GetComponentInChildren<ARSpeciesCard>();
                
                if (_currentCardComponent != null)
                {
                    _currentCardComponent.Initialize(this);
                }
            }
            else
            {
                // Move/Update the existing card
                _currentActiveCard.SetActive(true);
                _currentActiveCard.transform.position = targetPosition;
                // Note: Rotation is actively managed by Billboard.cs
            }

            // Populate the card with the detection info
            if (_currentCardComponent != null)
            {
                _currentCardComponent.PopulateData(result.species_id);
            }
            else
            {
                Debug.LogWarning("[ARUXManager] Spawned prefab is missing the ARSpeciesCard component.");
            }
        }
        else
        {
            Debug.Log("[ARUXManager] Raycast missed. No AR plane detected at that screen position.");
        }
    }

    /// <summary>
    /// Hides the active species card, if any.
    /// </summary>
    public void DismissActiveCard()
    {
        if (_currentActiveCard != null)
        {
            _currentActiveCard.SetActive(false);
        }
    }
}
