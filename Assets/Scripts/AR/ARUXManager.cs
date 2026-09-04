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

        Debug.Log($"[UIBRIDGE] ARUXTriggered: screen point {screenPoint}");

        Vector3 targetPosition;
        if (raycastManager.Raycast(screenPoint, _raycastHits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = _raycastHits[0].pose;
            targetPosition = hitPose.position + (Vector3.up * verticalOffset);
        }
        else
        {
            Debug.Log("[ARUXManager] Raycast missed. Using camera-space fallback.");
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(screenPoint);
                targetPosition = ray.GetPoint(1.0f);
            }
            else return;
        }

        if (_currentActiveCard == null)
        {
            _currentActiveCard = Instantiate(arSpeciesCardPrefab, targetPosition, Quaternion.identity);
            Debug.Log("[UIBRIDGE] CardInstantiated");
            _currentCardComponent = _currentActiveCard.GetComponentInChildren<ARSpeciesCard>();
            
            if (_currentCardComponent != null)
            {
                _currentCardComponent.Initialize(this);
            }
        }
        else
        {
            _currentActiveCard.SetActive(true);
            _currentActiveCard.transform.position = targetPosition;
        }

        if (_currentCardComponent != null)
        {
            _currentCardComponent.PopulateData(result.species_id);
        }
        else
        {
            Debug.LogWarning("[ARUXManager] Spawned prefab is missing the ARSpeciesCard component.");
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
