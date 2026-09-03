using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;

[RequireComponent(typeof(RawImage))]
public class CameraFrameToTexture : MonoBehaviour
{
    private ARCameraManager _cameraManager;
    private RawImage _rawImage;
    private Texture2D _cameraTexture;
    
    private float _lastConvertTime = 0f;
    private const float CONVERT_INTERVAL = 1.0f; // Limit to 1 fps for testing

    void Start()
    {
        _rawImage = GetComponent<RawImage>();
        _cameraManager = UnityEngine.Object.FindAnyObjectByType<ARCameraManager>();

        if (_cameraManager != null)
        {
            _cameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDestroy()
    {
        if (_cameraManager != null)
        {
            _cameraManager.frameReceived -= OnCameraFrameReceived;
        }
        if (_cameraTexture != null)
        {
            Destroy(_cameraTexture);
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (Time.time - _lastConvertTime < CONVERT_INTERVAL)
            return;

        _lastConvertTime = Time.time;

        if (_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            ConvertImage(image);
        }
    }

    private void ConvertImage(XRCpuImage image)
    {
        // Choose RGBA32 format for standard Texture2D
        var format = TextureFormat.RGBA32;

        if (_cameraTexture == null || _cameraTexture.width != image.width || _cameraTexture.height != image.height)
        {
            if (_cameraTexture != null) Destroy(_cameraTexture);
            
            _cameraTexture = new Texture2D(image.width, image.height, format, false);
            _rawImage.texture = _cameraTexture;
            
            // Adjust aspect ratio of the RawImage simply
            float aspect = (float)image.width / image.height;
            var rect = _rawImage.rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.y * aspect, rect.sizeDelta.y);
        }

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = format,
            // Depending on the device, the image may come in upside down or mirrored.
            // XRCpuImage.Transformation.MirrorY is often needed, but we'll try None or MirrorY.
            transformation = XRCpuImage.Transformation.None
        };

        // Convert the image directly into the Texture2D's raw texture data
        var rawTextureData = _cameraTexture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, rawTextureData);
            _cameraTexture.Apply();
            Debug.Log($"[CameraFrameToTexture] Successfully converted {image.width}x{image.height} to RGBA32.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CameraFrameToTexture] Conversion failed: {e.Message}");
        }
        finally
        {
            image.Dispose();
        }
    }
}
