using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace TeamCero.Data.SpeciesKnowledge
{
    /// <summary>
    /// Runtime repository for Species Knowledge Records.
    /// Loads validated JSON records from Unity Resources or Assets/Data/SpeciesKnowledge/.
    /// Zero hardcoded botanical facts: all content is loaded dynamically from authoritative JSON records.
    /// </summary>
    public static class SpeciesKnowledgeRepository
    {
        private static readonly Dictionary<string, SpeciesKnowledgeRecord> _records = 
            new Dictionary<string, SpeciesKnowledgeRecord>(StringComparer.OrdinalIgnoreCase);

        private static bool _isLoaded = false;
        private static readonly object _lock = new object();

        // Canonical list of known IDs defined in the Species Knowledge Layer (SP001 - SP010)
        public static readonly string[] CanonicalSpeciesIds = new string[]
        {
            "SP001", "SP002", "SP003", "SP004", "SP005",
            "SP006", "SP007", "SP008", "SP009", "SP010"
        };

        public static bool IsLoaded
        {
            get
            {
                lock (_lock)
                {
                    return _isLoaded;
                }
            }
        }

        public static int Count
        {
            get
            {
                EnsureLoaded();
                lock (_lock)
                {
                    return _records.Count;
                }
            }
        }

        /// <summary>
        /// Explicitly initializes/loads the repository if not already loaded.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (_isLoaded) return;

            lock (_lock)
            {
                if (_isLoaded) return;
                LoadAllRecords();
                _isLoaded = true;
            }
        }

        /// <summary>
        /// Clears and reloads all species knowledge records from runtime resources / disk.
        /// </summary>
        public static void Reload()
        {
            lock (_lock)
            {
                _records.Clear();
                _isLoaded = false;
                EnsureLoaded();
            }
        }

        /// <summary>
        /// Retrieves a species record by species ID (e.g. "SP001").
        /// Returns null safely if the species ID is invalid, unknown, or not found.
        /// </summary>
        public static SpeciesKnowledgeRecord GetRecord(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return null;

            EnsureLoaded();
            lock (_lock)
            {
                if (_records.TryGetValue(speciesId.Trim(), out SpeciesKnowledgeRecord record))
                {
                    return record;
                }
            }
            return null;
        }

        /// <summary>
        /// Safely attempts to retrieve a species record.
        /// Returns false if not found without throwing exceptions.
        /// </summary>
        public static bool TryGetRecord(string speciesId, out SpeciesKnowledgeRecord record)
        {
            record = GetRecord(speciesId);
            return record != null;
        }

        /// <summary>
        /// Returns a read-only snapshot of all loaded species knowledge records.
        /// </summary>
        public static IReadOnlyDictionary<string, SpeciesKnowledgeRecord> GetAllRecords()
        {
            EnsureLoaded();
            lock (_lock)
            {
                return new Dictionary<string, SpeciesKnowledgeRecord>(_records);
            }
        }

        private static void LoadAllRecords()
        {
            foreach (string spId in CanonicalSpeciesIds)
            {
                string jsonText = LoadJsonText(spId);
                if (!string.IsNullOrEmpty(jsonText))
                {
                    try
                    {
                        var record = JsonConvert.DeserializeObject<SpeciesKnowledgeRecord>(jsonText);
                        if (record != null && !string.IsNullOrEmpty(record.species_id))
                        {
                            _records[record.species_id] = record;
                        }
                        else
                        {
                            LogWarning($"[SpeciesKnowledgeRepository] Deserialized record for '{spId}' was null or missing species_id.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[SpeciesKnowledgeRepository] Failed to deserialize JSON for '{spId}': {ex.Message}");
                    }
                }
                else
                {
                    LogWarning($"[SpeciesKnowledgeRepository] Could not locate JSON file or TextAsset for '{spId}'.");
                }
            }

            Log($"[SpeciesKnowledgeRepository] Successfully loaded {_records.Count} species knowledge records.");
        }

        private static void Log(string message)
        {
            try { Debug.Log(message); }
            catch { Console.WriteLine(message); }
        }

        private static void LogWarning(string message)
        {
            try { Debug.LogWarning(message); }
            catch { Console.WriteLine("[WARN] " + message); }
        }

        private static void LogError(string message)
        {
            try { Debug.LogError(message); }
            catch { Console.Error.WriteLine("[ERROR] " + message); }
        }

        private static string LoadJsonText(string speciesId)
        {
            // 1. Try Unity Resources (Standard runtime packaging in Android/Standalone builds)
            try
            {
                var textAsset = Resources.Load<TextAsset>($"SpeciesKnowledge/{speciesId}");
                if (textAsset != null && !string.IsNullOrEmpty(textAsset.text))
                {
                    return textAsset.text;
                }
            }
            catch
            {
                // Fallback to direct file loading when Resources system is uninitialized (e.g. standalone test runners)
            }

            // 2. Try Assets/Data/SpeciesKnowledge/ via Application.dataPath (Unity Editor & Standalone)
            try
            {
                if (!string.IsNullOrEmpty(Application.dataPath))
                {
                    string dataPath = Path.Combine(Application.dataPath, "Data", "SpeciesKnowledge", $"{speciesId}.json");
                    if (File.Exists(dataPath))
                    {
                        return File.ReadAllText(dataPath);
                    }
                }
            }
            catch
            {
                // Ignored
            }

            // 3. Try Current Directory fallback (CLI test runners, batch tools)
            try
            {
                string[] fallbackDirs = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "SpeciesKnowledge"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "SpeciesKnowledge"),
                    @"D:\Unity\Projects\Team_Cero\Assets\Data\SpeciesKnowledge",
                    @"D:\Unity\Projects\Team_Cero\Assets\Resources\SpeciesKnowledge"
                };

                foreach (var dir in fallbackDirs)
                {
                    string path = Path.Combine(dir, $"{speciesId}.json");
                    if (File.Exists(path))
                    {
                        return File.ReadAllText(path);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[SpeciesKnowledgeRepository] Exception during fallback file read for '{speciesId}': {ex.Message}");
            }

            return null;
        }
    }
}
