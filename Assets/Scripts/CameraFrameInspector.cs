using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CameraFrameInspector : MonoBehaviour
{
    private ARCameraManager _cameraManager;
    private float _lastLogTime = 0f;
    private const float LOG_INTERVAL = 1.0f; // approx once per second

    void Start()
    {
        _cameraManager = GetComponent<ARCameraManager>();
        if (_cameraManager == null)
        {
            _cameraManager = UnityEngine.Object.FindAnyObjectByType<ARCameraManager>();
        }

        if (_cameraManager == null)
        {
            Debug.LogError("[CameraFrameInspector] No ARCameraManager found in the scene.");
            return;
        }

        Debug.Log($"[CameraFrameInspector] Subscribing to ARCameraManager on {_cameraManager.gameObject.name}");
        _cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDestroy()
    {
        if (_cameraManager != null)
        {
            _cameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (Time.time - _lastLogTime < LOG_INTERVAL)
        {
            return; // Only log approx once per second
        }

        _lastLogTime = Time.time;

        if (_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            // Acquisition succeeded!
            Debug.Log($"[CameraFrameInspector] SUCCESS: Acquired CPU Image. " +
                      $"Width: {image.width}, " +
                      $"Height: {image.height}, " +
                      $"Format: {image.format}, " +
                      $"Plane Count: {image.planeCount}, " +
                      $"Timestamp: {image.timestamp}");

            // IMPORTANT: The image MUST be disposed to prevent memory leaks.
            image.Dispose();
        }
        else
        {
            // Acquisition failed
            Debug.LogWarning("[CameraFrameInspector] FAILURE: TryAcquireLatestCpuImage returned false.");
        }
    }
}
