using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ExperimentalARTrackingCard : MonoBehaviour
{
    [Header("Dependencies")]
    public Pv002Pipeline pipeline;
    public ARRaycastManager raycastManager;
    public GameObject arSpeciesCardPrefab;
    public PerceptionStateMachine stateMachine;

    [Header("Tracking & Smoothing")]
    [Tooltip("Smoothing speed for position updates.")]
    public float positionSmoothSpeed = 8f;

    [Tooltip("Vertical offset in meters to float above the detected target point.")]
    public float verticalOffset = 0.15f;

    [Tooltip("Fallback distance in meters along the camera ray if no plane is detected.")]
    public float fallbackDistance = 1.0f;

    private GameObject _cardInstance;
    private ARSpeciesCard _cardComponent;
    private Vector3 _targetPosition;
    private bool _hasTarget = false;
    private bool _isFirstPlacement = true;
    private readonly List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();

    void Start()
    {
        if (pipeline == null)
        {
            pipeline = Object.FindAnyObjectByType<Pv002Pipeline>();
        }

        if (raycastManager == null)
        {
            raycastManager = Object.FindAnyObjectByType<ARRaycastManager>();
        }

        if (stateMachine == null)
        {
            stateMachine = Object.FindAnyObjectByType<PerceptionStateMachine>();
        }

        if (arSpeciesCardPrefab == null)
        {
            var ux = Object.FindAnyObjectByType<ARUXManager>();
            if (ux != null)
            {
                arSpeciesCardPrefab = ux.arSpeciesCardPrefab;
            }
        }

        if (pipeline != null)
        {
            if (pipeline.onDetectionUpdated == null)
            {
                pipeline.onDetectionUpdated = new Pv002Pipeline.DetectionUpdateEvent();
            }
            pipeline.onDetectionUpdated.AddListener(OnDetectionUpdated);
            Debug.Log("[UIBRIDGE] ExperimentalARTrackingCardStarted | Subscribed to pipeline");
        }
        else
        {
            Debug.LogWarning("[ExperimentalARTrackingCard] Pv002Pipeline not found.");
        }
    }

    private void OnDetectionUpdated(DetectionResult result)
    {
        // If production pipeline has confirmed a known species, yield to production card
        if (stateMachine != null && stateMachine.currentState == PerceptionState.CONFIRMED && result.status == "identified")
        {
            if (_cardInstance != null && _cardInstance.activeSelf)
            {
                _cardInstance.SetActive(false);
            }
            _hasTarget = false;
            return;
        }

        // Hide when no valid target bounding box is available
        if (!result.hasBoundingBox)
        {
            if (_cardInstance != null && _cardInstance.activeSelf)
            {
                _cardInstance.SetActive(false);
            }
            _hasTarget = false;
            return;
        }

        // Compute 3D target position from the authoritative mapped screen center
        Vector2 screenPoint = result.boundingBox.center;
        Vector3 newTargetPos = CalculateWorldPosition(screenPoint);

        _targetPosition = newTargetPos;
        _hasTarget = true;

        // Ensure card instance is created
        if (_cardInstance == null && arSpeciesCardPrefab != null)
        {
            _cardInstance = Instantiate(arSpeciesCardPrefab, _targetPosition, Quaternion.identity);
            _cardComponent = _cardInstance.GetComponentInChildren<ARSpeciesCard>();
            _isFirstPlacement = true;
            Debug.Log("[UIBRIDGE] TempARCardInstantiated");
        }

        if (_cardInstance != null)
        {
            if (!_cardInstance.activeSelf)
            {
                _cardInstance.SetActive(true);
                _isFirstPlacement = true;
            }

            if (_isFirstPlacement)
            {
                _cardInstance.transform.position = _targetPosition;
                _isFirstPlacement = false;
            }

            // Populate card text fields
            UpdateCardContent(result);
            Debug.Log($"[UIBRIDGE] TempARCardUpdated | Pos: {_targetPosition} | State: {result.status} | Conf: {result.confidence:F2}");
        }
    }

    private Vector3 CalculateWorldPosition(Vector2 screenPoint)
    {
        Camera cam = Camera.main;
        Vector3 camUp = cam != null ? cam.transform.up : Vector3.up;

        // 1. Raycast against AR Planes
        if (raycastManager != null && raycastManager.Raycast(screenPoint, _raycastHits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = _raycastHits[0].pose;
            return hitPose.position + (camUp * verticalOffset) + (Vector3.up * 0.05f);
        }

        // 2. Camera-relative fallback
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPoint);
            return ray.GetPoint(fallbackDistance) + (camUp * verticalOffset);
        }

        return Vector3.zero;
    }

    private void UpdateCardContent(DetectionResult result)
    {
        if (_cardComponent == null) return;

        if (result.status == "unrecognized")
        {
            SetText(_cardComponent.commonNameText, "UNKNOWN PLANT");
            SetText(_cardComponent.scientificNameText, "UNRECOGNIZED");
            SetText(_cardComponent.familyText, $"Confidence: {(result.confidence * 100f):F0}%");
            SetText(_cardComponent.nativeRegionText, "Live AR Tracking");
        }
        else
        {
            string commonName = result.species_id;
            string scientificName = "Plant";
            if (MockSpeciesDatabase.TryGetSpeciesData(result.species_id, out SpeciesData data))
            {
                if (!string.IsNullOrEmpty(data.commonName) && data.commonName != "Data Unavailable")
                {
                    commonName = data.commonName;
                }
                if (!string.IsNullOrEmpty(data.scientificName) && data.scientificName != "Data Unavailable")
                {
                    scientificName = data.scientificName;
                }
            }

            SetText(_cardComponent.commonNameText, commonName);
            SetText(_cardComponent.scientificNameText, "RECOGNIZED");
            SetText(_cardComponent.familyText, $"Confidence: {(result.confidence * 100f):F0}%");
            SetText(_cardComponent.nativeRegionText, scientificName);
        }
    }

    private void SetText(UnityEngine.UI.Text textComp, string content)
    {
        if (textComp != null)
        {
            textComp.text = content;
        }
    }

    void Update()
    {
        if (_hasTarget && _cardInstance != null && _cardInstance.activeSelf)
        {
            // Smoothly move towards the target world position to eliminate jitter
            _cardInstance.transform.position = Vector3.Lerp(
                _cardInstance.transform.position,
                _targetPosition,
                Time.deltaTime * positionSmoothSpeed
            );
        }
    }

    void OnDestroy()
    {
        if (pipeline != null && pipeline.onDetectionUpdated != null)
        {
            pipeline.onDetectionUpdated.RemoveListener(OnDetectionUpdated);
        }

        if (_cardInstance != null)
        {
            Destroy(_cardInstance);
        }
    }
}
