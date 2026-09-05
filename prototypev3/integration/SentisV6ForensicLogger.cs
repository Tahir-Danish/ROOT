using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TeamCero.PlantLocalizer.V3
{
    /// <summary>
    /// Machine-Readable JSONL & CSV Forensic Logger for Unity Sentis V3 Integration.
    /// Captures per-sample evidence per section A-H of UNITY_V6_INTEGRATION_FORENSIC_LOGGING.md.
    /// </summary>
    public class SentisV6ForensicLogger
    {
        private string logDirectory;
        private string jsonlPath;
        private string csvPath;

        public SentisV6ForensicLogger(string outputDirectory = null)
        {
            logDirectory = outputDirectory ?? Path.Combine(Application.persistentDataPath, "unity_v6_forensic");
            Directory.CreateDirectory(logDirectory);
            jsonlPath = Path.Combine(logDirectory, "INFERENCE_RESULTS.jsonl");
            csvPath = Path.Combine(logDirectory, "INFERENCE_RESULTS.csv");

            if (!File.Exists(csvPath))
            {
                File.WriteAllText(csvPath, "sample_id,timestamp,m1_detected,m1_conf,crop_w,crop_h,top1_species,top1_conf,accepted,rejection_reason,total_latency_ms\n");
            }
        }

        public void LogSample(
            string sampleId,
            bool m1Detected,
            float m1Conf,
            int cropW,
            int cropH,
            string[] speciesMapping,
            float[] probabilities,
            int top1Idx,
            float top1Conf,
            bool accepted,
            string rejectionReason,
            float totalLatencyMs)
        {
            string timestamp = DateTime.UtcNow.ToString("o");

            // Append CSV row
            string csvRow = $"{sampleId},{timestamp},{m1Detected},{m1Conf:F4},{cropW},{cropH},{speciesMapping[top1Idx]},{top1Conf:F4},{accepted},{rejectionReason},{totalLatencyMs:F2}\n";
            File.AppendAllText(csvPath, csvRow);

            // Build JSONL record
            StringBuilder json = new StringBuilder();
            json.Append("{");
            json.Append($"\"sample_id\":\"{sampleId}\",");
            json.Append($"\"timestamp\":\"{timestamp}\",");
            json.Append($"\"m1_detected\":{m1Detected.ToString().ToLower()},");
            json.Append($"\"m1_confidence\":{m1Conf:F4},");
            json.Append($"\"crop_bounds\":{{\"width\":{cropW},\"height\":{cropH}}},");
            json.Append($"\"top1_species\":\"{speciesMapping[top1Idx]}\",");
            json.Append($"\"top1_confidence\":{top1Conf:F4},");
            json.Append($"\"accepted\":{accepted.ToString().ToLower()},");
            json.Append($"\"rejection_reason\":\"{rejectionReason}\",");
            json.Append($"\"total_latency_ms\":{totalLatencyMs:F2},");
            json.Append("\"probabilities\":[");
            for (int i = 0; i < probabilities.Length; i++)
            {
                json.Append(probabilities[i].ToString("F6"));
                if (i < probabilities.Length - 1) json.Append(",");
            }
            json.Append("]}");

            File.AppendAllText(jsonlPath, json.ToString() + "\n");
        }
    }
}
