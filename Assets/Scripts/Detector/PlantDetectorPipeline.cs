using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.InferenceEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// EXPERIMENT 4C — Detailed Instrumentation (Time & Allocations)
/// </summary>
public class PlantDetectorPipeline : MonoBehaviour
{
    public enum Experiment4CMode
    {
        CAMERA_ONLY,
        CAMERA_PLUS_PREPROCESS,
        FULL_PIPELINE
    }

    [Header("Experiment 4C Settings")]
    public Experiment4CMode currentMode = Experiment4CMode.FULL_PIPELINE;
    
    [Tooltip("Keep true for full pipeline evaluation")]
    public bool useAsyncReadback = true;

    [Header("Model")]
    public ModelAsset detectorModelAsset;

    [Header("Detection Thresholds")]
    public float confidenceThreshold = 0.25f;
    public float iouThreshold = 0.45f;

    [Header("Inference Rate")]
    public float inferenceInterval = 0.2f;

    // --- Runtime state ---
    private Worker _worker;
    private Model _model;
    private ARCameraManager _cameraManager;
    private float _lastInferenceTime;
    private bool _isRunning;
    private bool _inferenceInProgress;

    private Texture2D _cameraTexture;
    private Tensor<float> _inputTensor;

    private float _letterboxScale;
    private float _letterboxOffsetX;
    private float _letterboxOffsetY;
    private int _originalImageWidth;
    private int _originalImageHeight;

    // --- Profiling State ---
    private const int METRICS_SAMPLES = 50;

    public class StageMetrics
    {
        public Queue<float> times = new Queue<float>();
        public Queue<long> allocs = new Queue<long>();

        public void Record(float timeMs, long allocBytes)
        {
            times.Enqueue(timeMs);
            if (times.Count > METRICS_SAMPLES) times.Dequeue();

            allocs.Enqueue(allocBytes);
            if (allocs.Count > METRICS_SAMPLES) allocs.Dequeue();
        }

        public float GetMedianTime() { return GetMedian(times); }
        public float GetP95Time() { return GetP95(times); }
        public float GetMedianAllocMB() { return GetMedian(allocs) / (1024f * 1024f); }
        public float GetP95AllocMB() { return GetP95(allocs) / (1024f * 1024f); }

        private float GetMedian(IEnumerable<float> data)
        {
            if (!data.Any()) return 0f;
            var sorted = data.OrderBy(t => t).ToList();
            return sorted[sorted.Count / 2];
        }

        private float GetP95(IEnumerable<float> data)
        {
            if (!data.Any()) return 0f;
            var sorted = data.OrderBy(t => t).ToList();
            int idx = Mathf.FloorToInt(sorted.Count * 0.95f);
            if (idx >= sorted.Count) idx = sorted.Count - 1;
            return sorted[idx];
        }
        
        private float GetMedian(IEnumerable<long> data)
        {
            if (!data.Any()) return 0f;
            var sorted = data.OrderBy(t => t).ToList();
            return sorted[sorted.Count / 2];
        }

        private float GetP95(IEnumerable<long> data)
        {
            if (!data.Any()) return 0f;
            var sorted = data.OrderBy(t => t).ToList();
            int idx = Mathf.FloorToInt(sorted.Count * 0.95f);
            if (idx >= sorted.Count) idx = sorted.Count - 1;
            return sorted[idx];
        }
    }

    private StageMetrics _stageTotal = new StageMetrics();
    private StageMetrics _stageConvert = new StageMetrics();
    private StageMetrics _stagePreprocess = new StageMetrics();
    private StageMetrics _stageUpload = new StageMetrics();
    private StageMetrics _stageSchedule = new StageMetrics();
    
    // Detailed Readback stages
    private StageMetrics _stagePeek = new StageMetrics();
    private StageMetrics _stageReadReqAsync = new StageMetrics();
    private StageMetrics _stageReadReqSync = new StageMetrics();
    private StageMetrics _stagePollIsDone = new StageMetrics();
    private StageMetrics _stageDownload = new StageMetrics();

    private StageMetrics _stageDecode = new StageMetrics();

    private Queue<float> _frameTimes = new Queue<float>();
    private int _processedCount;
    private int _gcCollectionCountStart;

    public struct Detection
    {
        public float x1, y1, x2, y2;
        public float confidence;
    }

    void Start()
    {
        _cameraManager = UnityEngine.Object.FindAnyObjectByType<ARCameraManager>();
        if (_cameraManager == null)
        {
            Debug.LogError("[PlantDetector] ARCameraManager not found.");
            enabled = false;
            return;
        }

        if (detectorModelAsset != null)
        {
            try
            {
                _model = ModelLoader.Load(detectorModelAsset);
                _worker = new Worker(_model, BackendType.GPUCompute);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlantDetector] Failed to create worker: {e.Message}");
            }
        }

        _isRunning = true;
        _gcCollectionCountStart = GC.CollectionCount(0);
        _cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDestroy()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived -= OnCameraFrameReceived;

        _inputTensor?.Dispose();
        _worker?.Dispose();
        if (_cameraTexture != null) Destroy(_cameraTexture);
    }

    void Update()
    {
        _frameTimes.Enqueue(Time.unscaledDeltaTime * 1000f);
        if (_frameTimes.Count > METRICS_SAMPLES) _frameTimes.Dequeue();
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!_isRunning || _inferenceInProgress) return;
        if (Time.time - _lastInferenceTime < inferenceInterval) return;

        if (currentMode == Experiment4CMode.CAMERA_ONLY)
        {
            _stageTotal.Record(0f, 0);
            _processedCount++;
            _lastInferenceTime = Time.time;
            return;
        }

        if (!_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            return;

        _inferenceInProgress = true;
        StartCoroutine(RunDetectionRoutine(cpuImage));
    }

    private IEnumerator RunDetectionRoutine(XRCpuImage cpuImage)
    {
        long memGlobalBefore = GC.GetAllocatedBytesForCurrentThread();
        var swGlobal = System.Diagnostics.Stopwatch.StartNew();
        var sw = new System.Diagnostics.Stopwatch();
        long memBefore = 0;

        // --- 1. Acquisition & Convert ---
        try
        {
            memBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            ConvertCpuImageToTexture(cpuImage);
            sw.Stop();
            _stageConvert.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);
        }
        finally
        {
            cpuImage.Dispose();
        }

        if (currentMode == Experiment4CMode.CAMERA_PLUS_PREPROCESS || currentMode == Experiment4CMode.FULL_PIPELINE)
        {
            // --- 2. Preprocess ---
            memBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            var inputData = PrepareInputTensorData();
            sw.Stop();
            _stagePreprocess.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);

            // --- 2B. Upload Tensor ---
            memBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            UploadTensor(inputData);
            sw.Stop();
            _stageUpload.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);
        }

        if (currentMode == Experiment4CMode.FULL_PIPELINE && _worker != null)
        {
            // --- 3. Schedule Inference ---
            memBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            _worker.Schedule(_inputTensor);
            sw.Stop();
            _stageSchedule.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);

            // --- 4. Readback Tensor Breakdown ---
            
            // 4A. Peek
            memBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            var outputTensor = _worker.PeekOutput("output0") as Tensor<float>;
            sw.Stop();
            _stagePeek.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);

            if (useAsyncReadback)
            {
                // 4B. ReadbackRequest
                memBefore = GC.GetAllocatedBytesForCurrentThread();
                sw.Restart();
                outputTensor.ReadbackRequest();
                sw.Stop();
                _stageReadReqAsync.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);
                _stageReadReqSync.Record(0, 0); // null out the other tracker

                // 4C. Poll IsDone
                long pollAlloc = 0;
                long pollTime = 0;
                
                while (true)
                {
                    long pollMemBefore = GC.GetAllocatedBytesForCurrentThread();
                    sw.Restart();
                    bool isDone = outputTensor.IsReadbackRequestDone();
                    sw.Stop();
                    pollTime += sw.ElapsedMilliseconds;
                    pollAlloc += (GC.GetAllocatedBytesForCurrentThread() - pollMemBefore);
                    
                    if (isDone) break;
                    yield return null; // this enables true async behavior across frames
                }
                _stagePollIsDone.Record(pollTime, pollAlloc);
            }
            else
            {
                // Sync mode (if enabled)
                memBefore = GC.GetAllocatedBytesForCurrentThread();
                sw.Restart();
                outputTensor.CompleteAllPendingOperations();
                sw.Stop();
                _stageReadReqSync.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);
                _stageReadReqAsync.Record(0, 0);
                _stagePollIsDone.Record(0, 0);
            }
            
            // 4D. Download
            memBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            var outputData = outputTensor.DownloadToArray();
            sw.Stop();
            _stageDownload.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);

            // --- 5. Decode & NMS ---
            memBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            DecodeOutput(outputData);
            sw.Stop();
            _stageDecode.Record(sw.ElapsedMilliseconds, GC.GetAllocatedBytesForCurrentThread() - memBefore);
        }

        swGlobal.Stop();
        long memGlobalAfter = GC.GetAllocatedBytesForCurrentThread();
        _stageTotal.Record(swGlobal.ElapsedMilliseconds, memGlobalAfter - memGlobalBefore);

        _processedCount++;
        _lastInferenceTime = Time.time;
        _inferenceInProgress = false;
    }

    private void ConvertCpuImageToTexture(XRCpuImage cpuImage)
    {
        _originalImageWidth = cpuImage.width;
        _originalImageHeight = cpuImage.height;

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

    private float[] PrepareInputTensorData()
    {
        const int modelSize = 640;

        float scaleX = (float)modelSize / _originalImageWidth;
        float scaleY = (float)modelSize / _originalImageHeight;
        _letterboxScale = Mathf.Min(scaleX, scaleY);

        int newW = Mathf.RoundToInt(_originalImageWidth * _letterboxScale);
        int newH = Mathf.RoundToInt(_originalImageHeight * _letterboxScale);
        _letterboxOffsetX = (modelSize - newW) / 2f;
        _letterboxOffsetY = (modelSize - newH) / 2f;

        float padValue = 114f / 255f;
        var inputData = new float[1 * 3 * modelSize * modelSize];
        for (int i = 0; i < inputData.Length; i++)
            inputData[i] = padValue;

        var pixels = _cameraTexture.GetPixels32();
        int offsetX = Mathf.RoundToInt(_letterboxOffsetX);
        int offsetY = Mathf.RoundToInt(_letterboxOffsetY);

        for (int dy = 0; dy < newH; dy++)
        {
            for (int dx = 0; dx < newW; dx++)
            {
                int sx = Mathf.Clamp(Mathf.RoundToInt(dx / _letterboxScale), 0, _originalImageWidth - 1);
                int sy = Mathf.Clamp(Mathf.RoundToInt(dy / _letterboxScale), 0, _originalImageHeight - 1);

                Color32 pixel = pixels[sy * _originalImageWidth + sx];

                int tx = offsetX + dx;
                int ty = offsetY + dy;

                int rIdx = 0 * modelSize * modelSize + ty * modelSize + tx;
                int gIdx = 1 * modelSize * modelSize + ty * modelSize + tx;
                int bIdx = 2 * modelSize * modelSize + ty * modelSize + tx;

                inputData[rIdx] = pixel.r / 255f;
                inputData[gIdx] = pixel.g / 255f;
                inputData[bIdx] = pixel.b / 255f;
            }
        }
        return inputData;
    }

    private void UploadTensor(float[] inputData)
    {
        const int modelSize = 640;
        if (_inputTensor == null)
            _inputTensor = new Tensor<float>(new TensorShape(1, 3, modelSize, modelSize), false);
            
        _inputTensor.Upload(inputData);
    }

    private void DecodeOutput(float[] output)
    {
        const int numPredictions = 8400;

        var rawDetections = new List<Detection>();

        for (int i = 0; i < numPredictions; i++)
        {
            float conf = output[4 * numPredictions + i];

            if (conf < confidenceThreshold) continue;

            float cx = output[0 * numPredictions + i];
            float cy = output[1 * numPredictions + i];
            float w  = output[2 * numPredictions + i];
            float h  = output[3 * numPredictions + i];

            float x1Model = cx - w / 2f;
            float y1Model = cy - h / 2f;
            float x2Model = cx + w / 2f;
            float y2Model = cy + h / 2f;

            float x1 = (x1Model - _letterboxOffsetX) / _letterboxScale;
            float y1 = (y1Model - _letterboxOffsetY) / _letterboxScale;
            float x2 = (x2Model - _letterboxOffsetX) / _letterboxScale;
            float y2 = (y2Model - _letterboxOffsetY) / _letterboxScale;

            rawDetections.Add(new Detection
            {
                x1 = Mathf.Clamp(x1, 0, _originalImageWidth),
                y1 = Mathf.Clamp(y1, 0, _originalImageHeight),
                x2 = Mathf.Clamp(x2, 0, _originalImageWidth),
                y2 = Mathf.Clamp(y2, 0, _originalImageHeight),
                confidence = conf
            });
        }
    }

    void OnGUI()
    {
        float medianFrame = _frameTimes.Count > 0 ? _frameTimes.OrderBy(t => t).ToList()[_frameTimes.Count / 2] : 0f;
        float fps = medianFrame > 0 ? 1000f / medianFrame : 0f;
        int totalGC = GC.CollectionCount(0) - _gcCollectionCountStart;
        string gfxInfo = SystemInfo.graphicsDeviceType.ToString();

        GUI.Label(new Rect(10, 10, Screen.width - 20, 600),
            $"<color=black><size=20><b>[Exp 4C Detailed] Mode: {currentMode} | GC Count: {totalGC}\n" +
            $"FPS: {fps:F0} (Med Frame: {medianFrame:F1}ms) | GFX: {gfxInfo}\n\n" +
            $"Stage        | Med ms | p95 ms | Med MB | p95 MB\n" +
            $"------------------------------------------------------\n" +
            $"Total Pipe   | {_stageTotal.GetMedianTime():F1} | {_stageTotal.GetP95Time():F1} | {_stageTotal.GetMedianAllocMB():F2} | {_stageTotal.GetP95AllocMB():F2}\n" +
            $"Convert      | {_stageConvert.GetMedianTime():F1} | {_stageConvert.GetP95Time():F1} | {_stageConvert.GetMedianAllocMB():F2} | {_stageConvert.GetP95AllocMB():F2}\n" +
            $"Preprocess   | {_stagePreprocess.GetMedianTime():F1} | {_stagePreprocess.GetP95Time():F1} | {_stagePreprocess.GetMedianAllocMB():F2} | {_stagePreprocess.GetP95AllocMB():F2}\n" +
            $"Upload       | {_stageUpload.GetMedianTime():F1} | {_stageUpload.GetP95Time():F1} | {_stageUpload.GetMedianAllocMB():F2} | {_stageUpload.GetP95AllocMB():F2}\n" +
            $"Schedule     | {_stageSchedule.GetMedianTime():F1} | {_stageSchedule.GetP95Time():F1} | {_stageSchedule.GetMedianAllocMB():F2} | {_stageSchedule.GetP95AllocMB():F2}\n" +
            $"Read/Peek    | {_stagePeek.GetMedianTime():F1} | {_stagePeek.GetP95Time():F1} | {_stagePeek.GetMedianAllocMB():F2} | {_stagePeek.GetP95AllocMB():F2}\n" +
            $"ReqAsync     | {_stageReadReqAsync.GetMedianTime():F1} | {_stageReadReqAsync.GetP95Time():F1} | {_stageReadReqAsync.GetMedianAllocMB():F2} | {_stageReadReqAsync.GetP95AllocMB():F2}\n" +
            $"ReqSync      | {_stageReadReqSync.GetMedianTime():F1} | {_stageReadReqSync.GetP95Time():F1} | {_stageReadReqSync.GetMedianAllocMB():F2} | {_stageReadReqSync.GetP95AllocMB():F2}\n" +
            $"Read/Poll    | {_stagePollIsDone.GetMedianTime():F1} | {_stagePollIsDone.GetP95Time():F1} | {_stagePollIsDone.GetMedianAllocMB():F2} | {_stagePollIsDone.GetP95AllocMB():F2}\n" +
            $"Read/Dwnld   | {_stageDownload.GetMedianTime():F1} | {_stageDownload.GetP95Time():F1} | {_stageDownload.GetMedianAllocMB():F2} | {_stageDownload.GetP95AllocMB():F2}\n" +
            $"Decode       | {_stageDecode.GetMedianTime():F1} | {_stageDecode.GetP95Time():F1} | {_stageDecode.GetMedianAllocMB():F2} | {_stageDecode.GetP95AllocMB():F2}\n" +
            $"</b></size></color>");
    }
}
