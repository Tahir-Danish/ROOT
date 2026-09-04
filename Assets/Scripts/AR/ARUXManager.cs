using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARUXManager : MonoBehaviour
{
    public enum SpatialState
    {
        NO_TARGET,
        ACQUIRING_SPATIAL,
        ANCHORED,
        TRACKING
    }

    [Tooltip("The raycast manager attached to the XR Origin.")]
    public ARRaycastManager raycastManager;

    [Tooltip("The anchor manager attached to the XR Origin.")]
    public ARAnchorManager anchorManager;

    [Tooltip("The plane manager attached to the XR Origin.")]
    public ARPlaneManager planeManager;

    [Tooltip("The prefab for the AR species card UI.")]
    public GameObject arSpeciesCardPrefab;

    [Tooltip("Vertical offset from the anchor surface to prevent clipping.")]
    public float verticalOffset = 0.05f;

    [Header("Spatial Lifecycle")]
    [SerializeField] private SpatialState _currentSpatialState = SpatialState.NO_TARGET;
    [SerializeField] private bool _isAnchored = false;

    public bool IsAnchored => _isAnchored;
    public SpatialState CurrentSpatialState => _currentSpatialState;

    private Transform _currentAnchor;
    private ARAnchor _currentARAnchor;
    private GameObject _currentActiveCard;
    private ARSpeciesCard _currentCardComponent;
    private string _anchoredSpeciesId = "";

    // A list to store the results of the raycast
    private readonly List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();

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

        if (planeManager == null)
        {
            planeManager = UnityEngine.Object.FindAnyObjectByType<ARPlaneManager>();
        }

        if (anchorManager == null)
        {
            anchorManager = UnityEngine.Object.FindAnyObjectByType<ARAnchorManager>();
            if (anchorManager == null && raycastManager != null)
            {
                anchorManager = raycastManager.gameObject.AddComponent<ARAnchorManager>();
                Debug.Log("[ARUXManager] ARAnchorManager dynamically added to XR Origin.");
            }
        }
    }

    private PerceptionState _lastLoggedState = PerceptionState.NO_TARGET;
    private float _lastLogTime = -1f;

    /// <summary>
    /// Processes a detection result associated with a specific screen coordinate.
    /// Invariant: Once anchored, detection updates content only and NEVER modifies position.
    /// </summary>
    public void HandleDetectionAtScreenPoint(DetectionResult result, Vector2 screenPoint, PerceptionState state = PerceptionState.CONFIRMED)
    {
        if (raycastManager == null) return;

        // Sparse diagnostic at decision boundary (avoid per-frame log spam):
        if (!_isAnchored || state != _lastLoggedState || (Time.time - _lastLogTime > 2.0f))
        {
            Debug.Log($"[ARUXManager] HandleDetectionAtScreenPoint state={state} screen={screenPoint}");
            _lastLoggedState = state;
            _lastLogTime = Time.time;
        }

        // 1. If already anchored, update content/species ONLY.
        if (_isAnchored && _currentAnchor != null)
        {
            if (_currentCardComponent != null && _anchoredSpeciesId != result.species_id)
            {
                _anchoredSpeciesId = result.species_id;
                _currentCardComponent.PopulateData(result.species_id);
            }
            return;
        }

        // 2. Spatial Acquisition: Attempt AR Raycast against detected planes (polygon + bounds)
        _currentSpatialState = SpatialState.ACQUIRING_SPATIAL;

        TrackableType raycastMask = TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds;

        if (raycastManager.Raycast(screenPoint, _raycastHits, raycastMask))
        {
            Pose hitPose = _raycastHits[0].pose;
            float hitDistance = _raycastHits[0].distance;
            TrackableId hitTrackableId = _raycastHits[0].trackableId;
            Debug.Log($"[ARUXManager] Raycast HIT: mask={raycastMask}, count={_raycastHits.Count}, trackable={hitTrackableId}, pose={hitPose}, distance={hitDistance:F2}m");

            _currentARAnchor = null;

            // Attempt to bind genuine native ARAnchor via ARAnchorManager
            ARPlane plane = planeManager != null ? planeManager.GetPlane(_raycastHits[0].trackableId) : null;
            if (anchorManager != null && plane != null)
            {
                try
                {
                    _currentARAnchor = anchorManager.AttachAnchor(plane, hitPose);
                    if (_currentARAnchor != null)
                    {
                        Debug.Log($"[ARUXManager] Genuine ARAnchor attached to plane {plane.trackableId}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ARUXManager] AttachAnchor exception: {ex.Message}");
                    _currentARAnchor = null;
                }
            }

            // CRITICAL CONTRACT: A card may exist ONLY after a genuine ARFoundation.ARAnchor has been successfully created/attached.
            // If AttachAnchor returns null, plane lookup fails, or subsystem is unavailable:
            // create NO card, create NO fallback GameObject, remain in ACQUIRING_SPATIAL, and retry on subsequent frames.
            if (_currentARAnchor == null)
            {
                _currentSpatialState = SpatialState.ACQUIRING_SPATIAL;
                Debug.Log($"[ARUXManager] Spatial acquisition: LOCATING SURFACE... (Valid plane hit, but genuine ARAnchor attachment pending/unavailable)");
                return;
            }

            _currentAnchor = _currentARAnchor.transform;

            // 3. Bind exactly ONE production card as presentation child under the genuine ARAnchor root
            if (_currentActiveCard == null)
            {
                _currentActiveCard = Instantiate(arSpeciesCardPrefab, _currentAnchor);
                _currentActiveCard.transform.localPosition = new Vector3(0, verticalOffset, 0);
                _currentActiveCard.transform.localRotation = Quaternion.identity;
                Debug.Log("[UIBRIDGE] CardInstantiated as child of native ARAnchor");
                _currentCardComponent = _currentActiveCard.GetComponentInChildren<ARSpeciesCard>();
                
                if (_currentCardComponent != null)
                {
                    _currentCardComponent.Initialize(this);
                }
            }
            else
            {
                _currentActiveCard.transform.SetParent(_currentAnchor, false);
                _currentActiveCard.transform.localPosition = new Vector3(0, verticalOffset, 0);
                _currentActiveCard.transform.localRotation = Quaternion.identity;
                _currentActiveCard.SetActive(true);
            }

            _anchoredSpeciesId = result.species_id;
            if (_currentCardComponent != null)
            {
                _currentCardComponent.PopulateData(result.species_id);
            }
            else
            {
                Debug.LogWarning("[ARUXManager] Spawned prefab is missing ARSpeciesCard component.");
            }

            _isAnchored = true;
            _currentSpatialState = SpatialState.ANCHORED;
            Debug.Log($"[ARUXManager] Spatial Target ANCHORED with genuine ARAnchor at {hitPose.position} for species {result.species_id}");
        }
        else
        {
            // Honest No-Hit Policy: Do NOT use camera-relative 1.0m fallback.
            // Remain in ACQUIRING_SPATIAL state and await next valid surface hit.
            _currentSpatialState = SpatialState.ACQUIRING_SPATIAL;
            Debug.Log($"[ARUXManager] Raycast NO HIT: mask={raycastMask}, count=0, screen={screenPoint} | Spatial acquisition: LOCATING SURFACE...");
        }
    }

    /// <summary>
    /// Hides the active species card and releases the spatial anchor.
    /// </summary>
    public void DismissActiveCard()
    {
        if (_currentActiveCard != null)
        {
            _currentActiveCard.SetActive(false);
            _currentActiveCard.transform.SetParent(null);
        }

        if (_currentARAnchor != null && anchorManager != null)
        {
            anchorManager.TryRemoveAnchor(_currentARAnchor);
        }

        _currentARAnchor = null;
        _currentAnchor = null;
        _isAnchored = false;
        _anchoredSpeciesId = "";
        _currentSpatialState = SpatialState.NO_TARGET;
        Debug.Log("[ARUXManager] Active card and genuine ARAnchor dismissed.");
    }
}
