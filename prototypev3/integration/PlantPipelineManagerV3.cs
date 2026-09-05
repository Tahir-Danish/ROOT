using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;

namespace TeamCero.PlantLocalizer.V3
{
    /// <summary>
    /// Prototype V3 Sentis Pipeline Manager with Explicit V6 Data-First Preprocessing and Forensic Logging.
    /// Operates independently of production Pv002Pipeline.cs for controlled integration testing.
    /// </summary>
    public class PlantPipelineManagerV3 : MonoBehaviour
    {
        [Header("Model Assets")]
        public ModelAsset model1Asset;                  // flora_localizer_v1_tuned.onnx
        public ModelAsset model2V6DatafirstAsset;       // flora_species_classifier_v6_datafirst.onnx

        [Header("Operating Thresholds")]
        [Range(0.0f, 1.0f)] public float model1ConfThreshold = 0.40f;
        [Range(0.0f, 1.0f)] public float model2ConfThreshold = 0.50f;

        [Header("Forensic Logging")]
        public bool enableForensicLogging = true;
        public string customLogDir = null;

        // Sentis Workers
        private Worker m_WorkerModel1;
        private Worker m_WorkerModel2;
        private SentisV6ForensicLogger m_Logger;

        public static readonly string[] SPECIES_MAPPING = new string[]
        {
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

        void Awake()
        {
            InitializePipelineV3();
        }

        public void InitializePipelineV3()
        {
            if (model1Asset != null)
            {
                var m1Runtime = ModelLoader.Load(model1Asset);
                m_WorkerModel1 = new Worker(m1Runtime, BackendType.GPUCompute);
            }

            if (model2V6DatafirstAsset != null)
            {
                var m2Runtime = ModelLoader.Load(model2V6DatafirstAsset);
                m_WorkerModel2 = new Worker(m2Runtime, BackendType.GPUCompute);
            }

            if (enableForensicLogging)
            {
                m_Logger = new SentisV6ForensicLogger(customLogDir);
            }

            Debug.Log("[Team Cero V3 Pipeline] Initialized with Model 2 V6 Data-First Checkpoint & Explicit Aspect-Preserving Preprocessing.");
        }

        public void ProcessCameraFrameV3(Texture2D inputFrame, string frameSampleId = "sample_001")
        {
            float tStart = Time.realtimeSinceStartup * 1000.0f;
            int origW = inputFrame.width;
            int origH = inputFrame.height;

            // 1. Model 1 Detection
            using TensorFloat inputM1Tensor = PreprocessModel1(inputFrame);
            m_WorkerModel1.Schedule(inputM1Tensor);

            using TensorFloat outputM1Tensor = m_WorkerModel1.PeekOutput("output0") as TensorFloat;
            outputM1Tensor.MakeReadable();

            float bestConf = 0.0f;
            int bestIndex = -1;
            int numAnchors = outputM1Tensor.shape[2];

            for (int i = 0; i < numAnchors; i++)
            {
                float conf = outputM1Tensor[0, 4, i];
                if (conf > bestConf)
                {
                    bestConf = conf;
                    bestIndex = i;
                }
            }

            if (bestConf < model1ConfThreshold || bestIndex < 0)
            {
                float totalMs = (Time.realtimeSinceStartup * 1000.0f) - tStart;
                m_Logger?.LogSample(frameSampleId, false, bestConf, 0, 0, SPECIES_MAPPING, new float[10], 9, 0.0f, false, "NO_PLANT_DETECTED", totalMs);
                return;
            }

            // Box Conversion (640x640 letterbox -> original pixels)
            float cx = outputM1Tensor[0, 0, bestIndex];
            float cy = outputM1Tensor[0, 1, bestIndex];
            float bw = outputM1Tensor[0, 2, bestIndex];
            float bh = outputM1Tensor[0, 3, bestIndex];

            float scaleRatio = Mathf.Min(640.0f / origW, 640.0f / origH);
            float padW = (640.0f - origW * scaleRatio) / 2.0f;
            float padH = (640.0f - origH * scaleRatio) / 2.0f;

            float x1 = Mathf.Clamp((cx - bw / 2.0f - padW) / scaleRatio, 0, origW);
            float y1 = Mathf.Clamp((cy - bh / 2.0f - padH) / scaleRatio, 0, origH);
            float x2 = Mathf.Clamp((cx + bw / 2.0f - padW) / scaleRatio, 0, origW);
            float y2 = Mathf.Clamp((cy + bh / 2.0f - padH) / scaleRatio, 0, origH);

            int cropX = Mathf.FloorToInt(x1);
            int cropY = Mathf.FloorToInt(y1);
            int cropW = Mathf.CeilToInt(x2 - x1);
            int cropH = Mathf.CeilToInt(y2 - y1);

            // 2. Aspect-Preserving V6 Preprocessing
            using TensorFloat inputM2Tensor = SentisV6Preprocessor.PreprocessModel2CropV6(inputFrame, cropX, cropY, cropW, cropH, out var preprocRecord);
            m_WorkerModel2.Schedule(inputM2Tensor);

            using TensorFloat outputM2Tensor = m_WorkerModel2.PeekOutput("probabilities") as TensorFloat;
            outputM2Tensor.MakeReadable();

            // 3. Output Parsing
            float[] probs = new float[10];
            int top1Idx = 0;
            float top1Prob = 0.0f;

            for (int c = 0; c < 10; c++)
            {
                probs[c] = outputM2Tensor[0, c];
                if (probs[c] > top1Prob)
                {
                    top1Prob = probs[c];
                    top1Idx = c;
                }
            }

            bool isUnknown = (top1Idx == 9);
            bool accepted = (!isUnknown) && (top1Prob >= model2ConfThreshold);
            string rejReason = accepted ? "ACCEPTED" : (isUnknown ? "EXPLICIT_UNKNOWN_PLANT" : $"BELOW_THRESHOLD ({top1Prob:F4} < {model2ConfThreshold})");

            float totalLatencyMs = (Time.realtimeSinceStartup * 1000.0f) - tStart;
            m_Logger?.LogSample(frameSampleId, true, bestConf, cropW, cropH, SPECIES_MAPPING, probs, top1Idx, top1Prob, accepted, rejReason, totalLatencyMs);
        }

        private TensorFloat PreprocessModel1(Texture2D src)
        {
            RenderTexture rt = RenderTexture.GetTemporary(640, 640, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            Texture2D resized = new Texture2D(640, 640, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            resized.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
            resized.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            Color32[] pixels = resized.GetPixels32();
            float[] data = new float[1 * 3 * 640 * 640];
            for (int i = 0; i < pixels.Length; i++)
            {
                data[i] = pixels[i].r / 255.0f;
                data[640 * 640 + i] = pixels[i].g / 255.0f;
                data[2 * 640 * 640 + i] = pixels[i].b / 255.0f;
            }
            Destroy(resized);
            return new TensorFloat(new TensorShape(1, 3, 640, 640), data);
        }

        void OnDestroy()
        {
            m_WorkerModel1?.Dispose();
            m_WorkerModel2?.Dispose();
        }
    }
}
