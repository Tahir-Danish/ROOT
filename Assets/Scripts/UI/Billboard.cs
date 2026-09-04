using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _mainCamera;

    [Tooltip("The fixed pitch (tilt back) of the plaque relative to the ground. 30-45 degrees is good for readability.")]
    public float fixedPitch = 30f;

    [Tooltip("How quickly the plaque swivels to face the user. Lower is heavier/smoother.")]
    public float swivelSpeed = 5f;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            // Calculate direction to camera
            Vector3 directionToCamera = _mainCamera.transform.position - transform.position;
            
            // 1. Planar Projection: Ignore camera height to prevent look-down singularity
            directionToCamera.y = 0;

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                // 2. Base Yaw: Face the user horizontally (-direction because UI front is -Z)
                Quaternion targetYaw = Quaternion.LookRotation(-directionToCamera, Vector3.up);

                // 3. Fixed Pitch: Tilt back for readability, avoiding camera pitch jitter
                Quaternion targetRotation = targetYaw * Quaternion.Euler(fixedPitch, 0, 0);

                // 4. Damped Interpolation: Give the object inertia so it feels physical
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * swivelSpeed);
            }
        }
    }
}
