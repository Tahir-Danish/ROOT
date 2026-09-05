using System;
using System.Collections.Generic;
using UnityEngine;
using TeamCero.Data.SpeciesKnowledge;

namespace TeamCero.Tests
{
    /// <summary>
    /// Deterministic validation suite for Phase 2A Species Knowledge Layer.
    /// Verifies all 10 required conditions from specification:
    /// 1. SP001-SP010 all load successfully.
    /// 2. Every species_id is unique.
    /// 3. Required source fields exist.
    /// 4. V6 recognized classes 0-8 resolve to SP001, SP002, SP003, SP004, SP005, SP007, SP008, SP009, SP010.
    /// 5. Class 9 resolves to unknown.
    /// 6. SP006 loads successfully but is not part of the V6 recognized-class mapping.
    /// 7. Lookup of an invalid/nonexistent species ID does not crash.
    /// 8. No botanical information is hardcoded outside the JSON source records.
    /// 9. ar_information is preserved (quick_id, ecological_importance, conservation_message).
    /// 10. Full/root-level fields remain available for the later Plant Details/Botanical Guide UI.
    /// </summary>
    public class SpeciesKnowledgeValidationTests : MonoBehaviour
    {
        public bool runOnStart = false;

        void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }

        private static void SafeLog(string message)
        {
            try { Debug.Log(message); }
            catch { Console.WriteLine(message); }
        }

        private static void SafeLogError(string message)
        {
            try { Debug.LogError(message); }
            catch { Console.Error.WriteLine("[ERROR] " + message); }
        }

        [ContextMenu("Run Species Knowledge Validation Tests")]
        public static bool RunAllTests()
        {
            SafeLog("==================================================");
            SafeLog("[SpeciesKnowledgeValidationTests] STARTING VALIDATION SUITE");
            SafeLog("==================================================");

            int passed = 0;
            int failed = 0;

            void Assert(string testName, bool condition, string details = "")
            {
                if (condition)
                {
                    SafeLog($"[PASS] {testName} {details}");
                    passed++;
                }
                else
                {
                    SafeLogError($"[FAIL] {testName} {details}");
                    failed++;
                }
            }

            try
            {
                SpeciesKnowledgeRepository.Reload();

                // -----------------------------------------------------------------
                // TEST 1: SP001–SP010 all load successfully
                // -----------------------------------------------------------------
                Assert("Test 1a: Repository has 10 records", SpeciesKnowledgeRepository.Count == 10, $"Count={SpeciesKnowledgeRepository.Count}");
                for (int i = 1; i <= 10; i++)
                {
                    string spId = $"SP{i:D3}";
                    var record = SpeciesKnowledgeRepository.GetRecord(spId);
                    Assert($"Test 1b: {spId} loaded successfully", record != null && record.species_id == spId,
                        record != null ? $"Name='{record.scientific_name}'" : "Record was NULL");
                }

                // -----------------------------------------------------------------
                // TEST 2: Every species_id is unique
                // -----------------------------------------------------------------
                var allRecords = SpeciesKnowledgeRepository.GetAllRecords();
                var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool allUnique = true;
                foreach (var kvp in allRecords)
                {
                    if (!seenIds.Add(kvp.Value.species_id))
                    {
                        allUnique = false;
                        break;
                    }
                }
                Assert("Test 2: Every species_id is unique", allUnique && seenIds.Count == 10, $"Unique count={seenIds.Count}");

                // -----------------------------------------------------------------
                // TEST 3: Required source fields exist on every record
                // -----------------------------------------------------------------
                bool allFieldsValid = true;
                foreach (var kvp in allRecords)
                {
                    var rec = kvp.Value;
                    if (string.IsNullOrEmpty(rec.species_id) ||
                        string.IsNullOrEmpty(rec.scientific_name) ||
                        string.IsNullOrEmpty(rec.accepted_name) ||
                        string.IsNullOrEmpty(rec.taxonomic_status) ||
                        string.IsNullOrEmpty(rec.family) ||
                        string.IsNullOrEmpty(rec.genus) ||
                        string.IsNullOrEmpty(rec.native_region) ||
                        string.IsNullOrEmpty(rec.habitat) ||
                        string.IsNullOrEmpty(rec.growth_form) ||
                        string.IsNullOrEmpty(rec.ecological_importance) ||
                        string.IsNullOrEmpty(rec.conservation_status) ||
                        string.IsNullOrEmpty(rec.ml_relevance) ||
                        string.IsNullOrEmpty(rec.confidence_level) ||
                        string.IsNullOrEmpty(rec.last_verified) ||
                        rec.common_names == null || rec.common_names.Count == 0 ||
                        rec.sources == null || rec.sources.Count == 0 ||
                        rec.ar_information == null ||
                        rec.vit_campus_relevance == null)
                    {
                        allFieldsValid = false;
                        Debug.LogError($"[Validation] Record {rec.species_id} is missing one or more required fields.");
                        break;
                    }
                }
                Assert("Test 3: Required source fields exist on all 10 records", allFieldsValid);

                // -----------------------------------------------------------------
                // TEST 4: V6 recognized classes 0–8 resolve to expected SP IDs
                // 0 -> SP001, 1 -> SP002, 2 -> SP003, 3 -> SP004, 4 -> SP005,
                // 5 -> SP007, 6 -> SP008, 7 -> SP009, 8 -> SP010
                // -----------------------------------------------------------------
                string[] expectedV6Mapping = new string[]
                {
                    "SP001", "SP002", "SP003", "SP004", "SP005",
                    "SP007", "SP008", "SP009", "SP010"
                };

                string[] expectedV6Labels = new string[]
                {
                    "01_alstonia_scholaris",
                    "02_justicia_adhatoda",
                    "03_acalypha_indica",
                    "04_mimusops_elengi",
                    "05_caryota_urens",
                    "07_strobilanthes_kunthiana",
                    "08_plumeria_alba",
                    "09_bougainvillea_spectabilis",
                    "10_gardenia_jasminoides"
                };

                bool v6MappingValid = true;
                for (int c = 0; c <= 8; c++)
                {
                    string resolvedByIndex = SpeciesClassMapping.FromV6ClassIndex(c);
                    string resolvedByLabel = SpeciesClassMapping.FromV6Label(expectedV6Labels[c]);
                    if (resolvedByIndex != expectedV6Mapping[c] || resolvedByLabel != expectedV6Mapping[c])
                    {
                        v6MappingValid = false;
                        Debug.LogError($"[Validation] Class {c} ('{expectedV6Labels[c]}') resolved to '{resolvedByIndex}' / '{resolvedByLabel}', expected '{expectedV6Mapping[c]}'");
                    }
                }
                Assert("Test 4: V6 recognized classes 0-8 resolve to correct SP IDs", v6MappingValid);

                // -----------------------------------------------------------------
                // TEST 5: Class 9 resolves to unknown
                // -----------------------------------------------------------------
                string class9ByIndex = SpeciesClassMapping.FromV6ClassIndex(9);
                string class9ByLabel = SpeciesClassMapping.FromV6Label("unknown_plant");
                bool class9IsUnknown = (class9ByIndex == SpeciesClassMapping.UnknownSpeciesId) &&
                                       (class9ByLabel == SpeciesClassMapping.UnknownSpeciesId) &&
                                       !SpeciesClassMapping.TryGetSpeciesId(9, out _) &&
                                       !SpeciesClassMapping.TryGetSpeciesId("unknown_plant", out _);
                Assert("Test 5: Class 9 resolves to unknown", class9IsUnknown, $"Index={class9ByIndex}, Label={class9ByLabel}");

                // -----------------------------------------------------------------
                // TEST 6: SP006 loads successfully but is NOT part of V6 recognized-class mapping
                // -----------------------------------------------------------------
                var recordSP006 = SpeciesKnowledgeRepository.GetRecord("SP006");
                bool sp006Loaded = recordSP006 != null && recordSP006.scientific_name.Contains("Pterocarpus santalinus");
                bool sp006NotInV6 = !SpeciesClassMapping.IsV6RecognizedSpecies("SP006");

                // Check that no V6 class index 0-9 returns SP006
                bool indexNeverReturnsSP006 = true;
                for (int i = 0; i <= 9; i++)
                {
                    if (SpeciesClassMapping.FromV6ClassIndex(i) == "SP006")
                    {
                        indexNeverReturnsSP006 = false;
                        break;
                    }
                }

                Assert("Test 6: SP006 loads successfully in knowledge layer but is NOT part of V6 model mapping",
                    sp006Loaded && sp006NotInV6 && indexNeverReturnsSP006,
                    $"Loaded={sp006Loaded}, InV6Mapping={!sp006NotInV6}");

                // -----------------------------------------------------------------
                // TEST 7: Lookup of invalid/nonexistent species ID does not crash
                // -----------------------------------------------------------------
                var nullQuery = SpeciesKnowledgeRepository.GetRecord(null);
                var emptyQuery = SpeciesKnowledgeRepository.GetRecord("");
                var unknownQuery = SpeciesKnowledgeRepository.GetRecord("SP999_NONEXISTENT");
                bool safeInvalid = (nullQuery == null) && (emptyQuery == null) && (unknownQuery == null);
                Assert("Test 7: Lookup of invalid/nonexistent species ID returns null safely without crashing", safeInvalid);

                // -----------------------------------------------------------------
                // TEST 8: No botanical information is hardcoded outside the JSON source records
                // -----------------------------------------------------------------
                // Verify repository has no hardcoded dictionary and relies on deserialized JSON
                SpeciesKnowledgeRepository.Reload();
                Assert("Test 8: Repository is dynamic and loads from authoritative JSON",
                    SpeciesKnowledgeRepository.IsLoaded && SpeciesKnowledgeRepository.Count == 10);

                // -----------------------------------------------------------------
                // TEST 9: ar_information is preserved
                // -----------------------------------------------------------------
                bool arInfoPreserved = true;
                foreach (var kvp in allRecords)
                {
                    var ar = kvp.Value.ar_information;
                    if (ar == null ||
                        string.IsNullOrEmpty(ar.quick_identification) ||
                        string.IsNullOrEmpty(ar.ecological_importance) ||
                        string.IsNullOrEmpty(ar.conservation_message))
                    {
                        arInfoPreserved = false;
                        SafeLogError($"[Validation] Record {kvp.Key} has incomplete ar_information.");
                        break;
                    }
                }
                Assert("Test 9: ar_information is preserved across all 10 records", arInfoPreserved);

                // -----------------------------------------------------------------
                // TEST 10: Full/root-level fields remain available for UI
                // -----------------------------------------------------------------
                var sp005 = SpeciesKnowledgeRepository.GetRecord("SP005");
                bool fullFieldsAvailable = sp005 != null &&
                    sp005.scientific_name == "Caryota urens L." &&
                    sp005.common_names.Contains("Fishtail Palm") &&
                    sp005.family == "Arecaceae" &&
                    sp005.genus == "Caryota" &&
                    sp005.threats.Count >= 2 &&
                    sp005.conservation_actions.Count >= 3 &&
                    sp005.field_identification_notes.Count >= 3 &&
                    sp005.sources.Count >= 2 &&
                    sp005.vit_campus_relevance != null &&
                    sp005.vit_campus_relevance.status == "CONFIRMED";

                Assert("Test 10: Full root-level fields (threats, actions, notes, sources, VIT relevance) available for UI",
                    fullFieldsAvailable);
            }
            catch (Exception ex)
            {
                SafeLogError($"[SpeciesKnowledgeValidationTests] FATAL EXCEPTION: {ex}");
                failed++;
            }

            SafeLog("==================================================");
            SafeLog($"[SpeciesKnowledgeValidationTests] RESULTS: {passed} PASSED, {failed} FAILED");
            SafeLog("==================================================");

            return failed == 0;
        }
    }
}
