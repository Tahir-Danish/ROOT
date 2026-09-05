using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamCero.Data.SpeciesKnowledge
{
    /// <summary>
    /// Phase 2B Adapter bridging authoritative Species Knowledge Records
    /// to presentation layers (ARSpeciesCard, AppNavigationShell, Plant Details).
    /// Provides zero-hardcoded botanical facts; all data is sourced dynamically
    /// from SpeciesKnowledgeRepository and resolved via SpeciesClassMapping.
    /// </summary>
    public static class SpeciesKnowledgeAdapter
    {
        /// <summary>
        /// Resolves any species identifier (canonical ID "SP005", V6 label "05_caryota_urens", etc.)
        /// to its canonical species ID ("SP005"). Returns "UNKNOWN" for unknown/unrecognized.
        /// </summary>
        public static string ResolveCanonicalId(string speciesIdOrLabel)
        {
            return SpeciesClassMapping.ResolveSpeciesId(speciesIdOrLabel);
        }

        /// <summary>
        /// Retrieves the authoritative SpeciesKnowledgeRecord for a given species identifier or V6 label.
        /// Returns null safely for unknown/unrecognized species.
        /// </summary>
        public static SpeciesKnowledgeRecord GetRecordForSpecies(string speciesIdOrLabel)
        {
            if (string.IsNullOrEmpty(speciesIdOrLabel)) return null;

            string canonicalId = ResolveCanonicalId(speciesIdOrLabel);
            if (canonicalId == SpeciesClassMapping.UnknownSpeciesId) return null;

            return SpeciesKnowledgeRepository.GetRecord(canonicalId);
        }

        /// <summary>
        /// Safely attempts to retrieve an authoritative SpeciesKnowledgeRecord.
        /// </summary>
        public static bool TryGetRecordForSpecies(string speciesIdOrLabel, out SpeciesKnowledgeRecord record)
        {
            record = GetRecordForSpecies(speciesIdOrLabel);
            return record != null;
        }

        /// <summary>
        /// Adapts an authoritative SpeciesKnowledgeRecord to the legacy/presentation SpeciesData struct,
        /// preserving backward compatibility without duplicating or hardcoding botanical facts.
        /// </summary>
        public static SpeciesData ToSpeciesData(this SpeciesKnowledgeRecord record)
        {
            if (record == null)
            {
                return default;
            }

            string primaryCommonName = (record.common_names != null && record.common_names.Count > 0)
                ? record.common_names[0]
                : (!string.IsNullOrEmpty(record.accepted_name) ? record.accepted_name : "Unknown Species");

            string quickId = (!string.IsNullOrEmpty(record.ar_information?.quick_identification))
                ? record.ar_information.quick_identification
                : (!string.IsNullOrEmpty(record.growth_form) ? record.growth_form : "Data Unavailable");

            string ecoImportance = (!string.IsNullOrEmpty(record.ar_information?.ecological_importance))
                ? record.ar_information.ecological_importance
                : (!string.IsNullOrEmpty(record.ecological_importance) ? record.ecological_importance : "Data Unavailable");

            return new SpeciesData
            {
                speciesId = record.species_id,
                commonName = primaryCommonName,
                scientificName = !string.IsNullOrEmpty(record.scientific_name) ? record.scientific_name : "Plantae sp.",
                family = !string.IsNullOrEmpty(record.family) ? record.family : "Data Unavailable",
                nativeRegion = !string.IsNullOrEmpty(record.native_region) ? record.native_region : "Data Unavailable",
                description = quickId,
                ecologicalImportance = ecoImportance,
                conservationStatus = !string.IsNullOrEmpty(record.conservation_status) ? record.conservation_status : "Data Unavailable"
            };
        }

        /// <summary>
        /// Attempts to get presentation SpeciesData directly from the authoritative knowledge layer.
        /// </summary>
        public static bool TryGetSpeciesData(string speciesIdOrLabel, out SpeciesData data)
        {
            if (TryGetRecordForSpecies(speciesIdOrLabel, out SpeciesKnowledgeRecord record))
            {
                data = record.ToSpeciesData();
                return true;
            }

            data = default;
            return false;
        }
    }
}
