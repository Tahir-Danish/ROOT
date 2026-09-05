using System;
using UnityEngine;
using Unity.Sentis;

namespace TeamCero.PlantLocalizer.V3
{
    /// <summary>
    /// Explicit V6 Data-First Preprocessing Contract Implementation for Unity Sentis.
    /// Reproduces exact PyTorch pipeline:
    /// Bounding Box Crop -> Aspect-Preserving Resize (shorter side 256) -> CenterCrop 224x224 -> ImageNet Normalization.
    /// </summary>
    public static class SentisV6Preprocessor
    {
        private static readonly float[] MEAN = new float[] { 0.485f, 0.456f, 0.406f };
        private static readonly float[] STD  = new float[] { 0.229f, 0.224f, 0.225f };

        public struct PreprocessRecord
        {
            public int originalCropW;
            public int originalCropH;
            public float aspect;
            public int scaledW;
            public int scaledH;
            public int cropOffsetX;
            public int cropOffsetY;
            public float tensorMin;
            public float tensorMax;
            public float tensorMean;
        }

        /// <summary>
        /// Preprocesses a bounding box crop from source texture according to V6 Contract.
        /// </summary>
        public static TensorFloat PreprocessModel2CropV6(Texture2D src, int x, int y, int w, int h, out PreprocessRecord record)
        {
            record = new PreprocessRecord
            {
                originalCropW = w,
                originalCropH = h,
                aspect = (float)w / (float)h
            };

            // 1. Calculate aspect-preserving dimensions (shorter side scaled to 256)
            int targetShort = 256;
            if (w < h)
            {
                record.scaledW = targetShort;
                record.scaledH = Mathf.RoundToInt(targetShort / record.aspect);
            }
            else
            {
                record.scaledH = targetShort;
                record.scaledW = Mathf.RoundToInt(targetShort * record.aspect);
            }

            // Render intermediate scaled crop
            RenderTexture rtScaled = RenderTexture.GetTemporary(record.scaledW, record.scaledH, 0, RenderTextureFormat.ARGB32);
            Graphics.SetRenderTarget(rtScaled);
            GL.Clear(true, true, Color.black);
            Graphics.Blit(src, rtScaled, new Vector2((float)w / src.width, (float)h / src.height), new Vector2((float)x / src.width, (float)y / src.height));

            // 2. Perform CenterCrop to 224x224
            record.cropOffsetX = (record.scaledW - 224) / 2;
            record.cropOffsetY = (record.scaledH - 224) / 2;

            RenderTexture rtFinal = RenderTexture.GetTemporary(224, 224, 0, RenderTextureFormat.ARGB32);
            Graphics.SetRenderTarget(rtFinal);
            GL.Clear(true, true, Color.black);
            Graphics.Blit(rtScaled, rtFinal, new Vector2(224.0f / record.scaledW, 224.0f / record.scaledH), new Vector2((float)record.cropOffsetX / record.scaledW, (float)record.cropOffsetY / record.scaledH));

            Texture2D cropped224 = new Texture2D(224, 224, TextureFormat.RGB24, false);
            RenderTexture.active = rtFinal;
            cropped224.ReadPixels(new Rect(0, 0, 224, 224), 0, 0);
            cropped224.Apply();
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(rtScaled);
            RenderTexture.ReleaseTemporary(rtFinal);

            // 3. ImageNet Normalization and Tensor Creation [1, 3, 224, 224]
            Color32[] pixels = cropped224.GetPixels32();
            float[] data = new float[1 * 3 * 224 * 224];

            float sum = 0.0f;
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;

            for (int i = 0; i < pixels.Length; i++)
            {
                float rNorm = (pixels[i].r / 255.0f - MEAN[0]) / STD[0];
                float gNorm = (pixels[i].g / 255.0f - MEAN[1]) / STD[1];
                float bNorm = (pixels[i].b / 255.0f - MEAN[2]) / STD[2];

                data[i] = rNorm;
                data[224 * 224 + i] = gNorm;
                data[2 * 224 * 224 + i] = bNorm;

                minVal = Mathf.Min(minVal, Mathf.Min(rNorm, Mathf.Min(gNorm, bNorm)));
                maxVal = Mathf.Max(maxVal, Mathf.Max(rNorm, Mathf.Max(gNorm, bNorm)));
                sum += rNorm + gNorm + bNorm;
            }

            record.tensorMin = minVal;
            record.tensorMax = maxVal;
            record.tensorMean = sum / (3.0f * pixels.Length);

            UnityEngine.Object.Destroy(cropped224);
            return new TensorFloat(new TensorShape(1, 3, 224, 224), data);
        }
    }
}
