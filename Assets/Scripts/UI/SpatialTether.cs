using UnityEngine;

/// <summary>
/// Genuine 3D Spatial Tether / Leader Line adhering to Section 10 of Spatial AR Spec v0.2.
/// Connects the anchored spatial target location (Point A) to the information card (Point B).
/// Presentation geometry only: does not affect world-locked anchor tracking.
/// </summary>
public class SpatialTether : MonoBehaviour
{
    [Header("Tether Endpoints")]
    [Tooltip("Point A: Anchored target location on the physical surface.")]
    public Transform anchorPoint;

    [Tooltip("Point B: Base attachment location of the floating information card.")]
    public Transform cardPoint;

    [Header("3D Geometry")]
    [Tooltip("Transform of the 3D cylinder primitive representing the leader line.")]
    public Transform tetherCylinder;

    [Tooltip("Physical diameter of the 3D tether rod in meters.")]
    public float rodDiameter = 0.005f; // 5mm solid composite/metallic rod

    [Tooltip("Optional 3D marker at the surface anchor point (Point A).")]
    public Transform anchorMarker;

    void Start()
    {
        UpdateTetherGeometry();
    }

    void LateUpdate()
    {
        UpdateTetherGeometry();
    }

    public void UpdateTetherGeometry()
    {
        Vector3 pointA = GetPointA();
        Vector3 pointB = GetPointB();

        // Update anchor marker position if present
        if (anchorMarker != null)
        {
            anchorMarker.position = pointA;
        }

        if (tetherCylinder == null) return;

        Vector3 direction = pointB - pointA;
        float length = direction.magnitude;

        if (length < 0.0005f)
        {
            tetherCylinder.gameObject.SetActive(false);
            return;
        }

        if (!tetherCylinder.gameObject.activeSelf)
        {
            tetherCylinder.gameObject.SetActive(true);
        }

        // Spec Section 10:
        // midpoint = (A + B) / 2
        // tether is placed at midpoint, aligned along direction, with length equal to |B - A|
        Vector3 midpoint = (pointA + pointB) * 0.5f;
        tetherCylinder.position = midpoint;
        tetherCylinder.up = direction.normalized;

        // Default Unity Cylinder has height = 2 along Y, diameter = 1 along X/Z
        tetherCylinder.localScale = new Vector3(rodDiameter, length * 0.5f, rodDiameter);
    }

    private Vector3 GetPointA()
    {
        if (anchorPoint != null)
        {
            return anchorPoint.position;
        }

        // If not explicitly wired, Point A is the parent ARAnchor's position
        if (transform.parent != null)
        {
            return transform.parent.position;
        }

        // Fallback: assume standard 5cm vertical offset below current root
        return transform.position - new Vector3(0, 0.05f, 0);
    }

    private Vector3 GetPointB()
    {
        if (cardPoint != null)
        {
            return cardPoint.position;
        }

        return transform.position;
    }
}
