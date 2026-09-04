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

    [System.Serializable]
    public class AnchoredTarget
    {
        public ARAnchor arAnchor;
        public GameObject cardInstance;
        public ARSpeciesCard cardComponent;
        public string speciesId;
        public Vector3 initialAnchorPosition;

        public Vector3 WorldPosition => arAnchor != null ? arAnchor.transform.position : initialAnchorPosition;
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

    [Tooltip("Conservative spatial deduplication radius in meters. Raycast hits within this distance of an existing anchor are treated as the same target.")]
    public float spatialDeduplicationRadius = 0.25f;

    [Tooltip("Maximum persistent cards permitted simultaneously in an AR session.")]
    public int maxPersistentCards = 5;

    [Header("Spatial Candidate Confirmation")]
    [Tooltip("Number of consistent spatial observations required before creating a new permanent card.")]
    public int requiredConfirmationObservations = 3;

    [Tooltip("Maximum spatial distance in meters between consecutive candidate observations to consider them the same candidate (roughly 10-15 cm).")]
    public float candidateSpatialRadius = 0.15f;

    [Tooltip("Maximum time window in seconds between candidate observations before the candidate expires/resets.")]
    public float candidateTimeoutWindow = 1.5f;

    [Header("Spatial Lifecycle")]
    [SerializeField] private SpatialState _currentSpatialState = SpatialState.NO_TARGET;
    [SerializeField] private bool _isAnchored = false;

    public bool IsAnchored => _anchoredTargets.Count > 0;
    public SpatialState CurrentSpatialState => _currentSpatialState;

    // Temporary spatial candidate awaiting repeated consistent observations
    private class SpatialCandidate
    {
        public Vector3 position;
        public TrackableId planeTrackableId;
        public Pose lastHitPose;
        public string speciesId;
        public int observationCount;
        public float firstObservationTime;
        public float lastObservationTime;
    }

    private SpatialCandidate _currentCandidate = null;
    private float _lastMaxCapLogTime = -1f;

    // Persistent collection of anchored spatial targets
    private readonly List<AnchoredTarget> _anchoredTargets = new List<AnchoredTarget>();
    public IReadOnlyList<AnchoredTarget> AnchoredTargets => _anchoredTargets;

    // Legacy fields preserved for backward compatibility & inspector inspection
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

    void Update()
    {
        // Expire stale candidate if no consistent observation occurred within candidateTimeoutWindow
        if (_currentCandidate != null && (Time.time - _currentCandidate.lastObservationTime > candidateTimeoutWindow))
        {
            Debug.Log($"[ARUXManager] Spatial candidate at {_currentCandidate.position} timed out after {Time.time - _currentCandidate.lastObservationTime:F2}s without observations.");
            _currentCandidate = null;
        }
    }

    private PerceptionState _lastLoggedState = PerceptionState.NO_TARGET;
    private float _lastLogTime = -1f;

    /// <summary>
    /// Processes a detection result associated with a specific screen coordinate.
    /// Invariant: Once anchored, detection updates content only and NEVER modifies position.
    /// Subsequent distinct spatial targets must pass candidate confirmation before permanent card creation.
    /// </summary>
    public void HandleDetectionAtScreenPoint(DetectionResult result, Vector2 screenPoint, PerceptionState state = PerceptionState.CONFIRMED)
    {
        if (raycastManager == null) return;

        // Sparse diagnostic at decision boundary (avoid per-frame log spam):
        if (!_isAnchored || state != _lastLoggedState || (Time.time - _lastLogTime > 2.0f))
        {
            Debug.Log($"[ARUXManager] HandleDetectionAtScreenPoint state={state} screen={screenPoint} (anchors={_anchoredTargets.Count})");
            _lastLoggedState = state;
            _lastLogTime = Time.time;
        }

        // Spatial Acquisition: Attempt AR Raycast against detected planes (polygon + bounds)
        TrackableType raycastMask = TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds;

        if (!raycastManager.Raycast(screenPoint, _raycastHits, raycastMask))
        {
            // Honest No-Hit Policy: Do NOT use camera-relative 1.0m fallback.
            // Remain in ACQUIRING_SPATIAL state if no anchors exist, or maintain existing state.
            if (_anchoredTargets.Count == 0)
            {
                _currentSpatialState = SpatialState.ACQUIRING_SPATIAL;
            }
            Debug.Log($"[ARUXManager] Raycast NO HIT: mask={raycastMask}, count=0, screen={screenPoint} | Spatial acquisition: LOCATING SURFACE...");
            return;
        }

        Pose hitPose = _raycastHits[0].pose;
        float hitDistance = _raycastHits[0].distance;
        TrackableId hitTrackableId = _raycastHits[0].trackableId;

        // 1. Same-target deduplication:
        // Check if this raycast hit corresponds to an already-anchored spatial target.
        // Existing-card matching/deduplication continues to work immediately without confirmation delay.
        AnchoredTarget existingTarget = FindClosestTarget(hitPose.position, spatialDeduplicationRadius);

        if (existingTarget != null)
        {
            // SAME PHYSICAL TARGET:
            // "Existing anchored card world position is immutable after acquisition."
            // "Detection updates may update the CONTENT of an existing card, but must never update its world position."
            // "Do not destroy/recreate an anchored card merely because ML confidence changes."
            if (existingTarget.cardComponent != null && existingTarget.speciesId != result.species_id)
            {
                Debug.Log($"[ARUXManager] Updating content for existing anchored target (dist={Vector3.Distance(hitPose.position, existingTarget.WorldPosition):F3}m): '{existingTarget.speciesId}' -> '{result.species_id}'");
                existingTarget.speciesId = result.species_id;
                existingTarget.cardComponent.PopulateData(result.species_id);
            }

            // Viewing an already-anchored card cancels any pending unconfirmed candidate
            _currentCandidate = null;
            return;
        }

        // 2. Max Persistent Cards Limit Check:
        if (_anchoredTargets.Count >= maxPersistentCards)
        {
            if (Time.time - _lastMaxCapLogTime > 2.0f)
            {
                Debug.LogWarning($"[ARUXManager] Max persistent cards limit ({maxPersistentCards}) reached. Creation of additional spatial cards suppressed at {hitPose.position}");
                _lastMaxCapLogTime = Time.time;
            }
            _currentCandidate = null;
            return;
        }

        // 3. TEMPORARY SPATIAL CANDIDATE CONFIRMATION STAGE:
        // Before creating a NEW permanent anchor/card, require repeated consistent observations
        // at approximately the same spatial location (within candidateSpatialRadius) within candidateTimeoutWindow.
        if (_currentCandidate != null && (Time.time - _currentCandidate.lastObservationTime > candidateTimeoutWindow))
        {
            Debug.Log($"[ARUXManager] Previous candidate timed out ({Time.time - _currentCandidate.lastObservationTime:F2}s > {candidateTimeoutWindow}s). Resetting candidate.");
            _currentCandidate = null;
        }

        if (_currentCandidate == null)
        {
            // Initial observation of a new candidate target
            _currentCandidate = new SpatialCandidate
            {
                position = hitPose.position,
                planeTrackableId = hitTrackableId,
                lastHitPose = hitPose,
                speciesId = result.species_id,
                observationCount = 1,
                firstObservationTime = Time.time,
                lastObservationTime = Time.time
            };
            Debug.Log($"[ARUXManager] New spatial candidate initialized at {hitPose.position} for species '{result.species_id}' (obs 1/{requiredConfirmationObservations})");
            return;
        }

        // Candidate already exists: evaluate spatial consistency
        float distToCandidate = Vector3.Distance(hitPose.position, _currentCandidate.position);

        if (distToCandidate <= candidateSpatialRadius)
        {
            // Consistent observation within spatial tolerance (10-15 cm)
            _currentCandidate.observationCount++;
            _currentCandidate.lastObservationTime = Time.time;
            _currentCandidate.lastHitPose = hitPose;
            _currentCandidate.planeTrackableId = hitTrackableId;

            // Refine species identification if a known species is identified during candidate qualification
            if (!string.IsNullOrEmpty(result.species_id) && result.species_id != "unknown_plant")
            {
                _currentCandidate.speciesId = result.species_id;
            }

            if (_currentCandidate.observationCount < requiredConfirmationObservations)
            {
                Debug.Log($"[ARUXManager] Spatial candidate observed (obs {_currentCandidate.observationCount}/{requiredConfirmationObservations}, dist={distToCandidate:F3}m <= {candidateSpatialRadius:F2}m)");
                return;
            }

            // All required observations verified: CANDIDATE CONFIRMED!
            Debug.Log($"[ARUXManager] Spatial candidate CONFIRMED after {_currentCandidate.observationCount} observations over {Time.time - _currentCandidate.firstObservationTime:F2}s at {_currentCandidate.position}");
        }
        else
        {
            // Inconsistent observation: spatial jump exceeded threshold. Reset candidate to the new location.
            Debug.Log($"[ARUXManager] Spatial candidate reset: jump distance {distToCandidate:F3}m > {candidateSpatialRadius:F2}m. Starting new candidate at {hitPose.position}");
            _currentCandidate = new SpatialCandidate
            {
                position = hitPose.position,
                planeTrackableId = hitTrackableId,
                lastHitPose = hitPose,
                speciesId = result.species_id,
                observationCount = 1,
                firstObservationTime = Time.time,
                lastObservationTime = Time.time
            };
            return;
        }

        // 4. PERMANENT ARAnchor + Card Creation for Confirmed Candidate:
        Pose confirmedPose = _currentCandidate.lastHitPose;
        TrackableId confirmedPlaneId = _currentCandidate.planeTrackableId;
        string confirmedSpeciesId = _currentCandidate.speciesId;
        _currentCandidate = null; // Clear candidate after qualification

        Debug.Log($"[ARUXManager] Raycast HIT for CONFIRMED target: mask={raycastMask}, count={_raycastHits.Count}, trackable={confirmedPlaneId}, pose={confirmedPose}, distance={hitDistance:F2}m");

        ARAnchor newARAnchor = null;
        ARPlane plane = planeManager != null ? planeManager.GetPlane(confirmedPlaneId) : null;
        if (anchorManager != null && plane != null)
        {
            try
            {
                newARAnchor = anchorManager.AttachAnchor(plane, confirmedPose);
                if (newARAnchor != null)
                {
                    Debug.Log($"[ARUXManager] Genuine ARAnchor attached to plane {plane.trackableId} at {confirmedPose.position}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ARUXManager] AttachAnchor exception: {ex.Message}");
                newARAnchor = null;
            }
        }

        // CRITICAL CONTRACT: A card may exist ONLY after a genuine ARFoundation.ARAnchor has been successfully created/attached.
        // If AttachAnchor returns null, plane lookup fails, or subsystem is unavailable:
        // create NO card, create NO fallback GameObject, remain in ACQUIRING_SPATIAL, and retry on subsequent frames.
        if (newARAnchor == null)
        {
            if (_anchoredTargets.Count == 0)
            {
                _currentSpatialState = SpatialState.ACQUIRING_SPATIAL;
            }
            Debug.Log($"[ARUXManager] Spatial acquisition: LOCATING SURFACE... (Valid plane hit, but genuine ARAnchor attachment pending/unavailable)");
            return;
        }

        Transform anchorTransform = newARAnchor.transform;

        // Instantiate exactly ONE production card as presentation child under the genuine ARAnchor root
        GameObject cardInstance = Instantiate(arSpeciesCardPrefab, anchorTransform);
        cardInstance.transform.localPosition = new Vector3(0, verticalOffset, 0);
        cardInstance.transform.localRotation = Quaternion.identity;
        Debug.Log($"[UIBRIDGE] CardInstantiated as child of native ARAnchor ({newARAnchor.trackableId})");

        ARSpeciesCard cardComponent = cardInstance.GetComponentInChildren<ARSpeciesCard>();
        if (cardComponent != null)
        {
            cardComponent.Initialize(this);
            cardComponent.PopulateData(confirmedSpeciesId);
        }
        else
        {
            Debug.LogWarning("[ARUXManager] Spawned prefab is missing ARSpeciesCard component.");
        }

        // Register persistent target
        var targetEntry = new AnchoredTarget
        {
            arAnchor = newARAnchor,
            cardInstance = cardInstance,
            cardComponent = cardComponent,
            speciesId = confirmedSpeciesId,
            initialAnchorPosition = confirmedPose.position
        };
        _anchoredTargets.Add(targetEntry);

        // Update legacy references for backward compatibility / inspector
        _currentARAnchor = newARAnchor;
        _currentAnchor = anchorTransform;
        _currentActiveCard = cardInstance;
        _currentCardComponent = cardComponent;
        _anchoredSpeciesId = confirmedSpeciesId;
        _isAnchored = true;
        _currentSpatialState = SpatialState.ANCHORED;

        Debug.Log($"[ARUXManager] Spatial Target #{_anchoredTargets.Count} ANCHORED with genuine ARAnchor at {confirmedPose.position} for species {confirmedSpeciesId}");
    }

    /// <summary>
    /// Searches existing anchored targets for one within maxDistance of worldPos.
    /// Also prunes any targets whose anchor or card was destroyed externally.
    /// </summary>
    private AnchoredTarget FindClosestTarget(Vector3 worldPos, float maxDistance)
    {
        AnchoredTarget closest = null;
        float closestDist = maxDistance;

        for (int i = _anchoredTargets.Count - 1; i >= 0; i--)
        {
            var target = _anchoredTargets[i];
            if (target == null || target.arAnchor == null || target.cardInstance == null)
            {
                _anchoredTargets.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(worldPos, target.WorldPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = target;
            }
        }

        return closest;
    }

    /// <summary>
    /// Dismisses and cleans up a specific anchored card and its ARAnchor.
    /// </summary>
    public void DismissCard(ARSpeciesCard card)
    {
        if (card == null) return;
        DismissCard(card.gameObject);
    }

    /// <summary>
    /// Dismisses and cleans up a specific anchored card instance and its ARAnchor.
    /// </summary>
    public void DismissCard(GameObject cardInstance)
    {
        if (cardInstance == null) return;

        int index = _anchoredTargets.FindIndex(t => t != null && (t.cardInstance == cardInstance || (t.cardComponent != null && t.cardComponent.gameObject == cardInstance)));
        if (index >= 0)
        {
            var target = _anchoredTargets[index];
            _anchoredTargets.RemoveAt(index);

            if (target.cardInstance != null)
            {
                Destroy(target.cardInstance);
            }

            if (target.arAnchor != null && anchorManager != null)
            {
                try
                {
                    anchorManager.TryRemoveAnchor(target.arAnchor);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ARUXManager] TryRemoveAnchor exception: {ex.Message}");
                }
            }

            _isAnchored = _anchoredTargets.Count > 0;
            _currentSpatialState = _isAnchored ? SpatialState.ANCHORED : SpatialState.NO_TARGET;
            Debug.Log($"[ARUXManager] Dismissed specific card at index {index}. Remaining anchors: {_anchoredTargets.Count}");
        }
    }

    /// <summary>
    /// Dismisses all anchored species cards and releases their spatial anchors.
    /// </summary>
    public void DismissAllCards()
    {
        for (int i = _anchoredTargets.Count - 1; i >= 0; i--)
        {
            var target = _anchoredTargets[i];
            if (target != null)
            {
                if (target.cardInstance != null)
                {
                    Destroy(target.cardInstance);
                }

                if (target.arAnchor != null && anchorManager != null)
                {
                    try
                    {
                        anchorManager.TryRemoveAnchor(target.arAnchor);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ARUXManager] TryRemoveAnchor exception: {ex.Message}");
                    }
                }
            }
        }

        _anchoredTargets.Clear();
        _currentARAnchor = null;
        _currentAnchor = null;
        _currentActiveCard = null;
        _currentCardComponent = null;
        _anchoredSpeciesId = "";
        _currentCandidate = null;
        _isAnchored = false;
        _currentSpatialState = SpatialState.NO_TARGET;
        Debug.Log("[ARUXManager] All anchored cards and ARAnchors dismissed.");
    }

    /// <summary>
    /// Backward-compatible dismiss: dismisses all active anchored cards.
    /// </summary>
    public void DismissActiveCard()
    {
        DismissAllCards();
    }
}
