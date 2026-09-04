using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.InferenceEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

public class Pv002Pipeline : MonoBehaviour
{
    [Header("Models")]
    public ModelAsset localizerModelAsset;
    public ModelAsset classifierModelAsset;

    [Header("Perception")]
    public PerceptionStateMachine stateMachine;

    [System.Serializable]
    public class DetectionUpdateEvent : UnityEvent<DetectionResult> { }

    public DetectionUpdateEvent onDetectionUpdated = new DetectionUpdateEvent();

    [Header("Inference Rate")]
    public float inferenceInterval = 0.2f;

    // ML Assets
    private Worker _worker1;
    private Worker _worker2;
    private Model _model1;
    private Model _model2;
    
    private ARCameraManager _cameraManager;
    private Matrix4x4 _displayMatrix = Matrix4x4.identity;
    private Texture2D _cameraTexture;
    
    private bool _isRunning;
    private bool _inferenceInProgress;
    private float _lastInferenceTime;
    
    private const int MODEL1_SIZE = 640;
    private const int MODEL2_SIZE = 224;

    private readonly string[] CLASSES = new string[] {
        "01_alstonia_scholaris",
        "02_justicia_adhatoda",
        "03_acalypha_indica",
        "04_mimusops_elengi",
        "05_caryota_urens",
        "07_strobilanthes_kunthiana",
        "08_plumeria_alba",
        "09_bougainvillea_spectabilis",
        "10_gardenia_jasminoides",
        "unknown_plant"
    };

    public struct BoundingBox
    {
        public float x1, y1, x2, y2, confidence;
    }

    void Start()
    {
        _cameraManager = UnityEngine.Object.FindAnyObjectByType<ARCameraManager>();
        if (_cameraManager == null)
        {
            Debug.LogError("[Pv002Pipeline] ARCameraManager not found.");
            enabled = false;
            return;
        }

        if (stateMachine == null)
        {
            stateMachine = UnityEngine.Object.FindAnyObjectByType<PerceptionStateMachine>();
        }

        try
        {
            if (localizerModelAsset != null)
            {
                _model1 = ModelLoader.Load(localizerModelAsset);
                _worker1 = new Worker(_model1, BackendType.GPUCompute);
            }
            if (classifierModelAsset != null)
            {
                _model2 = ModelLoader.Load(classifierModelAsset);
                _worker2 = new Worker(_model2, BackendType.GPUCompute);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Pv002Pipeline] Failed to create workers: {e.Message}");
        }

        _isRunning = true;
        _cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDestroy()
    {
        if (_cameraManager != null) _cameraManager.frameReceived -= OnCameraFrameReceived;

        _worker1?.Dispose();
        _worker2?.Dispose();
        if (_cameraTexture != null) Destroy(_cameraTexture);
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (args.displayMatrix.HasValue)
        {
            _displayMatrix = args.displayMatrix.Value;
        }

        if (!_isRunning || _inferenceInProgress) return;
        if (Time.time - _lastInferenceTime < inferenceInterval) return;

        if (!_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            return;

        _inferenceInProgress = true;
        StartCoroutine(RunPipelineRoutine(cpuImage));
    }

    private IEnumerator RunPipelineRoutine(XRCpuImage cpuImage)
    {
        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        var swStage = new System.Diagnostics.Stopwatch();
        
        long timeM1 = 0, timeNMS = 0, timeCrop = 0, timeM2 = 0;

        Tensor<float> inputTensor1 = null;
        Tensor<float> outputTensor1 = null;
        Tensor<float> inputTensor2 = null;
        Tensor<float> outputTensor2 = null;

        try
        {
            // --- 1. Acquisition ---
            ConvertCpuImageToTexture(cpuImage);
            cpuImage.Dispose(); // Dispose as soon as converted

            // --- 2. Model 1 Preprocess & Upload ---
            swStage.Restart();
            inputTensor1 = PrepareModel1Input();
            
            // --- 3. Model 1 Inference ---
            _worker1.Schedule(inputTensor1);
            outputTensor1 = _worker1.PeekOutput("output0") as Tensor<float>;
            outputTensor1.ReadbackRequest();

            while (!outputTensor1.IsReadbackRequestDone())
            {
                yield return null;
            }
            var outputData1 = outputTensor1.DownloadToArray();
            swStage.Stop();
            timeM1 = swStage.ElapsedMilliseconds;

            // --- 4. Model 1 NMS & Postprocess ---
            swStage.Restart();
            var bestBox = DecodeAndNMS(outputData1, _cameraTexture.width, _cameraTexture.height);
            swStage.Stop();
            timeNMS = swStage.ElapsedMilliseconds;

            if (bestBox == null)
            {
                SubmitNoPlant();
                swTotal.Stop();
                LogTimings(timeM1, timeNMS, timeCrop, timeM2, swTotal.ElapsedMilliseconds);
                yield break; // Exit early, no plant detected
            }

            // --- 5. Model 2 Crop & Preprocess ---
            swStage.Restart();
            inputTensor2 = PrepareModel2Input(bestBox.Value);
            swStage.Stop();
            timeCrop = swStage.ElapsedMilliseconds;

            if (inputTensor2 == null)
            {
                // Crop too small or invalid
                SubmitNoPlant();
                swTotal.Stop();
                LogTimings(timeM1, timeNMS, timeCrop, timeM2, swTotal.ElapsedMilliseconds);
                yield break; 
            }

            // --- 6. Model 2 Inference ---
            swStage.Restart();
            _worker2.Schedule(inputTensor2);
            outputTensor2 = _worker2.PeekOutput("probabilities") as Tensor<float>;
            outputTensor2.ReadbackRequest();

            while (!outputTensor2.IsReadbackRequestDone())
            {
                yield return null;
            }
            var outputData2 = outputTensor2.DownloadToArray();
            swStage.Stop();
            timeM2 = swStage.ElapsedMilliseconds;

            // --- 7. Model 2 Postprocess & Acceptance ---
            ProcessModel2Output(outputData2, bestBox.Value);
            swTotal.Stop();
            
            LogTimings(timeM1, timeNMS, timeCrop, timeM2, swTotal.ElapsedMilliseconds);
        }
        finally
        {
            // Ensure all tensors are disposed to prevent memory leaks
            inputTensor1?.Dispose();
            // outputTensor1 is owned by the worker (PeekOutput), do not dispose
            inputTensor2?.Dispose();
            // outputTensor2 is owned by the worker (PeekOutput), do not dispose
            
            _processedCount++;
            _lastInferenceTime = Time.time;
            _inferenceInProgress = false;
        }
    }
    
    private int _processedCount;

    private void LogTimings(long m1, long nms, long crop, long m2, long total)
    {
        if (_processedCount % 10 == 0) // Log every 10 frames to avoid spam
        {
            Debug.Log($"[Pv002] Pipeline Timings - Total: {total}ms | M1: {m1}ms | NMS: {nms}ms | Crop: {crop}ms | M2: {m2}ms");
        }
    }

    private void ConvertCpuImageToTexture(XRCpuImage cpuImage)
    {
        if (_cameraTexture == null || _cameraTexture.width != cpuImage.width || _cameraTexture.height != cpuImage.height)
        {
            if (_cameraTexture != null) Destroy(_cameraTexture);
            _cameraTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.RGBA32, false);
        }

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
            outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        var rawTextureData = _cameraTexture.GetRawTextureData<byte>();
        cpuImage.Convert(conversionParams, rawTextureData);
        _cameraTexture.Apply();
    }

    private Tensor<float> PrepareModel1Input()
    {
        int origW = _cameraTexture.width;
        int origH = _cameraTexture.height;

        float scaleX = (float)MODEL1_SIZE / origW;
        float scaleY = (float)MODEL1_SIZE / origH;
        float scale = Mathf.Min(scaleX, scaleY);

        int newW = Mathf.RoundToInt(origW * scale);
        int newH = Mathf.RoundToInt(origH * scale);
        int offsetX = Mathf.RoundToInt((MODEL1_SIZE - newW) / 2f);
        int offsetY = Mathf.RoundToInt((MODEL1_SIZE - newH) / 2f);

        float padValue = 114f / 255f;
        var inputData = new float[1 * 3 * MODEL1_SIZE * MODEL1_SIZE];
        for (int i = 0; i < inputData.Length; i++) inputData[i] = padValue;

        var pixels = _cameraTexture.GetPixels32();

        for (int dy = 0; dy < newH; dy++)
        {
            for (int dx = 0; dx < newW; dx++)
            {
                int sx = Mathf.Clamp(Mathf.RoundToInt(dx / scale), 0, origW - 1);
                int sy = Mathf.Clamp(Mathf.RoundToInt(dy / scale), 0, origH - 1);

                Color32 pixel = pixels[sy * origW + sx];

                int tx = offsetX + dx;
                int ty = offsetY + dy;

                int rIdx = 0 * MODEL1_SIZE * MODEL1_SIZE + ty * MODEL1_SIZE + tx;
                int gIdx = 1 * MODEL1_SIZE * MODEL1_SIZE + ty * MODEL1_SIZE + tx;
                int bIdx = 2 * MODEL1_SIZE * MODEL1_SIZE + ty * MODEL1_SIZE + tx;

                inputData[rIdx] = pixel.r / 255f;
                inputData[gIdx] = pixel.g / 255f;
                inputData[bIdx] = pixel.b / 255f;
            }
        }

        var tensor = new Tensor<float>(new TensorShape(1, 3, MODEL1_SIZE, MODEL1_SIZE), false);
        tensor.Upload(inputData);
        return tensor;
    }

    private BoundingBox? DecodeAndNMS(float[] output, int origW, int origH)
    {
        const int numPredictions = 8400;
        float confidenceThreshold = 0.40f;
        float iouThreshold = 0.45f;

        float scaleX = (float)MODEL1_SIZE / origW;
        float scaleY = (float)MODEL1_SIZE / origH;
        float scale = Mathf.Min(scaleX, scaleY);
        float padW = (MODEL1_SIZE - origW * scale) / 2f;
        float padH = (MODEL1_SIZE - origH * scale) / 2f;

        var rawDetections = new List<BoundingBox>();
        float maxObservedConf = 0f;

        for (int i = 0; i < numPredictions; i++)
        {
            float conf = output[4 * numPredictions + i];
            if (conf > maxObservedConf) maxObservedConf = conf;

            if (conf < confidenceThreshold) continue;

            float cx = output[0 * numPredictions + i];
            float cy = output[1 * numPredictions + i];
            float w  = output[2 * numPredictions + i];
            float h  = output[3 * numPredictions + i];

            float x1 = (cx - w / 2f - padW) / scale;
            float y1 = (cy - h / 2f - padH) / scale;
            float x2 = (cx + w / 2f - padW) / scale;
            float y2 = (cy + h / 2f - padH) / scale;

            rawDetections.Add(new BoundingBox
            {
                x1 = Mathf.Max(0, Mathf.Floor(x1)),
                y1 = Mathf.Max(0, Mathf.Floor(y1)),
                x2 = Mathf.Min(origW, Mathf.Ceil(x2)),
                y2 = Mathf.Min(origH, Mathf.Ceil(y2)),
                confidence = conf
            });
        }

        Debug.Log($"[Pv002] M1 Diagnostic: {rawDetections.Count} boxes > threshold, Max Conf: {maxObservedConf:F4}");

        if (rawDetections.Count == 0) return null;

        // Apply NMS
        rawDetections.Sort((a, b) => b.confidence.CompareTo(a.confidence));
        var nmsResults = new List<BoundingBox>();

        foreach (var box in rawDetections)
        {
            bool keep = true;
            foreach (var keptBox in nmsResults)
            {
                if (ComputeIoU(box, keptBox) > iouThreshold)
                {
                    keep = false;
                    break;
                }
            }
            if (keep) nmsResults.Add(box);
        }

        if (nmsResults.Count > 0)
        {
            return nmsResults[0]; // Single highest-confidence surviving detection
        }

        return null;
    }

    private float ComputeIoU(BoundingBox a, BoundingBox b)
    {
        float x1 = Mathf.Max(a.x1, b.x1);
        float y1 = Mathf.Max(a.y1, b.y1);
        float x2 = Mathf.Min(a.x2, b.x2);
        float y2 = Mathf.Min(a.y2, b.y2);
        
        float intersectionArea = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float areaA = (a.x2 - a.x1) * (a.y2 - a.y1);
        float areaB = (b.x2 - b.x1) * (b.y2 - b.y1);
        
        return intersectionArea / (areaA + areaB - intersectionArea);
    }

    private Tensor<float> PrepareModel2Input(BoundingBox box)
    {
        int x = (int)box.x1;
        int y = (int)box.y1;
        int w = (int)(box.x2 - box.x1);
        int h = (int)(box.y2 - box.y1);

        if (w < 10 || h < 10) return null; // Too small

        var inputData = new float[1 * 3 * MODEL2_SIZE * MODEL2_SIZE];
        var pixels = _cameraTexture.GetPixels32();
        int origW = _cameraTexture.width;
        
        float scaleX = (float)w / MODEL2_SIZE;
        float scaleY = (float)h / MODEL2_SIZE;

        float[] mean = { 0.485f, 0.456f, 0.406f };
        float[] std = { 0.229f, 0.224f, 0.225f };

        for (int dy = 0; dy < MODEL2_SIZE; dy++)
        {
            for (int dx = 0; dx < MODEL2_SIZE; dx++)
            {
                int sx = x + Mathf.Clamp(Mathf.RoundToInt(dx * scaleX), 0, w - 1);
                int sy = y + Mathf.Clamp(Mathf.RoundToInt(dy * scaleY), 0, h - 1);

                Color32 pixel = pixels[sy * origW + sx];

                int rIdx = 0 * MODEL2_SIZE * MODEL2_SIZE + dy * MODEL2_SIZE + dx;
                int gIdx = 1 * MODEL2_SIZE * MODEL2_SIZE + dy * MODEL2_SIZE + dx;
                int bIdx = 2 * MODEL2_SIZE * MODEL2_SIZE + dy * MODEL2_SIZE + dx;

                inputData[rIdx] = ((pixel.r / 255f) - mean[0]) / std[0];
                inputData[gIdx] = ((pixel.g / 255f) - mean[1]) / std[1];
                inputData[bIdx] = ((pixel.b / 255f) - mean[2]) / std[2];
            }
        }

        var tensor = new Tensor<float>(new TensorShape(1, 3, MODEL2_SIZE, MODEL2_SIZE), false);
        tensor.Upload(inputData);
        return tensor;
    }

    private void ProcessModel2Output(float[] output, BoundingBox box)
    {
        float maxProb = -1f;
        int maxIndex = -1;

        for (int i = 0; i < CLASSES.Length; i++)
        {
            if (output[i] > maxProb)
            {
                maxProb = output[i];
                maxIndex = i;
            }
        }

        Debug.Log($"[Pv002] M2 Prediction: {CLASSES[maxIndex]} ({maxProb:F2})");

        if (maxIndex != 9 && maxProb >= 0.50f)
        {
            SubmitDetection(CLASSES[maxIndex], maxProb, "identified", box);
        }
        else
        {
            SubmitDetection("unknown_plant", maxProb, "unrecognized", box);
        }
    }

    private void SubmitNoPlant()
    {
        SubmitDetection("", 0f, "unrecognized", null);
    }

    private Rect MapBoxToScreen(BoundingBox box, int texW, int texH)
    {
        // Coordinate Space: 
        // Input: BoundingBox (pixels in camera texture, x: 0..texW, y: 0..texH)
        // Output: Rect (pixels in Unity Screen space, bottom-left origin: 0..Screen.width, 0..Screen.height)
        
        // AR Foundation / ARCore displayMatrix is ROW-MAJOR where:
        // u_cam = sx * m00 + sy * m10 + m20
        // v_cam = sx * m01 + sy * m11 + m21
        float a = _displayMatrix.m00;
        float b = _displayMatrix.m10;
        float tx0 = _displayMatrix.m20;

        float c = _displayMatrix.m01;
        float d = _displayMatrix.m11;
        float ty0 = _displayMatrix.m21;

        float det = a * d - b * c;
        bool hasValidDisplayMatrix = Mathf.Abs(det) > 1e-6f;

        Vector2 MapPoint(float px, float py)
        {
            if (hasValidDisplayMatrix)
            {
                // Exact 2D affine inversion:
                // [u - tx0]   [a  b] [sx]
                // [v - ty0] = [c  d] [sy]
                float u = px / (float)texW;
                float v = py / (float)texH;

                float du = u - tx0;
                float dv = v - ty0;

                float sx = (d * du - b * dv) / det;
                float sy = (-c * du + a * dv) / det;

                return new Vector2(sx * Screen.width, sy * Screen.height);
            }
            else
            {
                // Authoritative Fallback: Portrait AspectFill (scale-to-fill) mapping
                float screenW = Screen.width;
                float screenH = Screen.height;

                // Sensor 640x480 in 90-degree portrait orientation:
                float camPortraitW = (float)texH; // 480
                float camPortraitH = (float)texW; // 640

                float scale = Mathf.Max(screenW / camPortraitW, screenH / camPortraitH);
                float offX = (screenW - camPortraitW * scale) / 2f;
                float offY = (screenH - camPortraitH * scale) / 2f;

                float portX = py;
                float portY = px;

                return new Vector2(offX + portX * scale, offY + portY * scale);
            }
        }

        Vector2 p1 = MapPoint(box.x1, box.y1);
        Vector2 p2 = MapPoint(box.x2, box.y1);
        Vector2 p3 = MapPoint(box.x1, box.y2);
        Vector2 p4 = MapPoint(box.x2, box.y2);

        float minX = Mathf.Min(p1.x, p2.x, p3.x, p4.x);
        float maxX = Mathf.Max(p1.x, p2.x, p3.x, p4.x);
        float minY = Mathf.Min(p1.y, p2.y, p3.y, p4.y);
        float maxY = Mathf.Max(p1.y, p2.y, p3.y, p4.y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private void SubmitDetection(string species, float confidence, string status, BoundingBox? box)
    {
        Rect screenRect = default;
        if (box.HasValue && _cameraTexture != null)
        {
            screenRect = MapBoxToScreen(box.Value, _cameraTexture.width, _cameraTexture.height);
            Debug.Log($"[Pv002] BoxMapping | DetectorBox: ({box.Value.x1:F1}, {box.Value.y1:F1}, {box.Value.x2:F1}, {box.Value.y2:F1}) | ScreenRect: ({screenRect.x:F1}, {screenRect.y:F1}, {screenRect.width:F1}, {screenRect.height:F1}) | Screen: {Screen.width}x{Screen.height}");
        }

        var result = new DetectionResult
        {
            species_id = species,
            confidence = confidence,
            status = status,
            hasBoundingBox = box.HasValue,
            boundingBox = screenRect
        };

        Debug.Log($"[UIBRIDGE] PipelineDetection: {species}, conf: {confidence:F2}, status: {status}, box: {box.HasValue}");

        if (onDetectionUpdated == null)
        {
            onDetectionUpdated = new DetectionUpdateEvent();
        }
        onDetectionUpdated.Invoke(result);

        if (stateMachine != null)
        {
            stateMachine.SubmitDetection(result);
        }
    }
}
