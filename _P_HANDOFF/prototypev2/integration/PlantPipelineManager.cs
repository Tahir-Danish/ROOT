using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;

namespace TeamCero.PlantLocalizer
{
    /// <summary>
    /// Production Unity Sentis Pipeline Manager for Team Cero Model 1 -> Model 2 AR Identification.
    /// Integrates audited Model 1 (YOLO11n) and Model 2 (V6 Primary / V7 Experimental).
    /// </summary>
    public class PlantPipelineManager : MonoBehaviour
    {
        public enum Model2Variant
        {
            V6_PRIMARY,
            V7_ARCFACE_EXPERIMENTAL
        }

        [Header("Model 2 Checkpoint Selection")]
        [Tooltip("V6_PRIMARY is the production default (balanced recognition & low FAR). V7 is experimental.")]
        public Model2Variant activeModel2Variant = Model2Variant.V6_PRIMARY;

        [Header("Model Assets")]
        public ModelAsset model1Asset;       // flora_localizer_v1_tuned.onnx
        public ModelAsset model2V6Asset;     // flora_species_classifier_v6_autonomous.onnx (or flora_species_classifier_v6_autonomous.onnx)
        public ModelAsset model2V7Asset;     // plant_classifier_model2_v7.onnx

        [Header("Pipeline Operating Thresholds")]
        [Range(0.0f, 1.0f)]
        public float model1ConfThreshold = 0.40f;

        [Range(0.0f, 1.0f)]
        public float model2ConfThreshold = 0.50f;

        [Header("AR Card UI Integration")]
        public Text speciesNameText;
        public Text confidenceText;
        public GameObject arCardPanel;

        // Sentis Workers & Runtimes
        private Worker m_WorkerModel1;
        private Worker m_WorkerModel2;

        // Class Mapping (10 Classes)
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

        public struct PipelineResult
        {
            public bool plantDetected;
            public Rect boundingBox;
            public float model1Confidence;
            public int speciesIndex;
            public string speciesId;
            public float speciesConfidence;
            public bool accepted;
            public string finalState;
        }

        void Awake()
        {
            InitializePipeline();
        }

        public void InitializePipeline()
        {
            if (model1Asset != null)
            {
                var m1Runtime = ModelLoader.Load(model1Asset);
                m_WorkerModel1 = new Worker(m1Runtime, BackendType.GPUCompute);
            }

            ModelAsset selectedV2 = (activeModel2Variant == Model2Variant.V6_PRIMARY) ? model2V6Asset : model2V7Asset;
            if (selectedV2 == null && model2V6Asset != null) selectedV2 = model2V6Asset;

            if (selectedV2 != null)
            {
                var m2Runtime = ModelLoader.Load(selectedV2);
                m_WorkerModel2 = new Worker(m2Runtime, BackendType.GPUCompute);
            }

            Debug.Log($"[Team Cero Pipeline] Initialized with Model 2 Variant: {activeModel2Variant}");
        }

        /// <summary>
        /// Process incoming camera frame Texture2D through Model 1 -> Crop -> Model 2 -> AR UI.
        /// </summary>
        public PipelineResult ProcessCameraFrame(Texture2D inputFrame)
        {
            PipelineResult result = new PipelineResult
            {
                plantDetected = false,
                finalState = "NO_PLANT"
            };

            if (m_WorkerModel1 == null || m_WorkerModel2 == null)
            {
                Debug.LogError("[Team Cero Pipeline] Sentis workers not initialized!");
                return result;
            }

            int origW = inputFrame.width;
            int origH = inputFrame.height;

            // 1. Preprocess Frame for Model 1 (640x640 Letterbox)
            using TensorFloat inputM1Tensor = PreprocessModel1(inputFrame);
            m_WorkerModel1.Schedule(inputM1Tensor);

            using TensorFloat outputM1Tensor = m_WorkerModel1.PeekOutput("output0") as TensorFloat;
            outputM1Tensor.MakeReadable();

            // 2. Parse Model 1 Detection [1, 5, 8400]
            float bestConf = 0.0f;
            int bestIndex = -1;
            int numAnchors = outputM1Tensor.shape[2]; // 8400

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
                UpdateARUI(false, "No Plant Detected", 0.0f);
                return result;
            }

            // Extract best bounding box (center_x, center_y, width, height) in 640x640 space
            float cx = outputM1Tensor[0, 0, bestIndex];
            float cy = outputM1Tensor[0, 1, bestIndex];
            float bw = outputM1Tensor[0, 2, bestIndex];
            float bh = outputM1Tensor[0, 3, bestIndex];

            // Convert to original pixel coordinates
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

            if (cropW < 10 || cropH < 10)
            {
                UpdateARUI(false, "Invalid Crop", 0.0f);
                return result;
            }

            result.plantDetected = true;
            result.model1Confidence = bestConf;
            result.boundingBox = new Rect(cropX, cropY, cropW, cropH);

            // 3. Preprocess Crop for Model 2 (224x224 RGB Float32 + ImageNet Norm)
            using TensorFloat inputM2Tensor = PreprocessModel2Crop(inputFrame, cropX, cropY, cropW, cropH);
            m_WorkerModel2.Schedule(inputM2Tensor);

            using TensorFloat outputM2Tensor = m_WorkerModel2.PeekOutput("probabilities") as TensorFloat;
            outputM2Tensor.MakeReadable();

            // 4. Parse Model 2 Output [1, 10]
            int top1Idx = 0;
            float top1Prob = 0.0f;

            for (int c = 0; c < 10; c++)
            {
                float prob = outputM2Tensor[0, c];
                if (prob > top1Prob)
                {
                    top1Prob = prob;
                    top1Idx = c;
                }
            }

            result.speciesIndex = top1Idx;
            result.speciesId = SPECIES_MAPPING[top1Idx];
            result.speciesConfidence = top1Prob;

            // 5. Apply Rejection Contract
            bool isUnknownClass = (top1Idx == 9);
            result.accepted = (!isUnknownClass) && (top1Prob >= model2ConfThreshold);

            if (result.accepted)
            {
                result.finalState = "TARGET_SPECIES";
                UpdateARUI(true, result.speciesId, top1Prob);
            }
            else
            {
                result.finalState = "UNKNOWN_PLANT";
                UpdateARUI(false, "Unknown Plant", top1Prob);
            }

            return result;
        }

        private TensorFloat PreprocessModel1(Texture2D src)
        {
            // Convert Texture2D into [1, 3, 640, 640] normalized tensor
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
                data[i] = pixels[i].r / 255.0f;                             // Red channel
                data[640 * 640 + i] = pixels[i].g / 255.0f;                 // Green channel
                data[2 * 640 * 640 + i] = pixels[i].b / 255.0f;             // Blue channel
            }

            Destroy(resized);
            return new TensorFloat(new TensorShape(1, 3, 640, 640), data);
        }

        private TensorFloat PreprocessModel2Crop(Texture2D src, int x, int y, int w, int h)
        {
            // Extract crop and resize to 224x224 with ImageNet normalization
            RenderTexture rt = RenderTexture.GetTemporary(224, 224, 0, RenderTextureFormat.ARGB32);
            Graphics.SetRenderTarget(rt);
            GL.Clear(true, true, Color.black);

            Graphics.Blit(src, rt, new Vector2((float)w / src.width, (float)h / src.height), new Vector2((float)x / src.width, (float)y / src.height));

            Texture2D cropped = new Texture2D(224, 224, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            cropped.ReadPixels(new Rect(0, 0, 224, 224), 0, 0);
            cropped.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            Color32[] pixels = cropped.GetPixels32();
            float[] data = new float[1 * 3 * 224 * 224];

            // ImageNet Stats
            float[] mean = new float[] { 0.485f, 0.456f, 0.406f };
            float[] std = new float[] { 0.229f, 0.224f, 0.225f };

            for (int i = 0; i < pixels.Length; i++)
            {
                float rNorm = (pixels[i].r / 255.0f - mean[0]) / std[0];
                float gNorm = (pixels[i].g / 255.0f - mean[1]) / std[1];
                float bNorm = (pixels[i].b / 255.0f - mean[2]) / std[2];

                data[i] = rNorm;
                data[224 * 224 + i] = gNorm;
                data[2 * 224 * 224 + i] = bNorm;
            }

            Destroy(cropped);
            return new TensorFloat(new TensorShape(1, 3, 224, 224), data);
        }

        private void UpdateARUI(bool showCard, string speciesName, float confidence)
        {
            if (arCardPanel != null) arCardPanel.SetActive(showCard);
            if (speciesNameText != null) speciesNameText.text = speciesName;
            if (confidenceText != null) confidenceText.text = $"{confidence * 100.0f:F1}%";
        }

        void OnDestroy()
        {
            m_WorkerModel1?.Dispose();
            m_WorkerModel2?.Dispose();
        }
    }
}
