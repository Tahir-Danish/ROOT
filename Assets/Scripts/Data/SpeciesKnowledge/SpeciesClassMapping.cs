using System.Collections.Generic;

namespace TeamCero.Data.SpeciesKnowledge
{
    /// <summary>
    /// Centralized mapping layer resolving V6 ML classification output (class index or label)
    /// to canonical Species Knowledge IDs (SP001-SP010).
    /// Enforces the architectural rule: SP006 is not recognized by Model 2 V6.
    /// </summary>
    public static class SpeciesClassMapping
    {
        public const string UnknownSpeciesId = "UNKNOWN";

        // V6 ML Class Index to Canonical Species ID
        private static readonly Dictionary<int, string> IndexToSpeciesMap = new Dictionary<int, string>
        {
            { 0, "SP001" }, // 01_alstonia_scholaris
            { 1, "SP002" }, // 02_justicia_adhatoda
            { 2, "SP003" }, // 03_acalypha_indica
            { 3, "SP004" }, // 04_mimusops_elengi
            { 4, "SP005" }, // 05_caryota_urens
            { 5, "SP007" }, // 07_strobilanthes_kunthiana
            { 6, "SP008" }, // 08_plumeria_alba
            { 7, "SP009" }, // 09_bougainvillea_spectabilis
            { 8, "SP010" }, // 10_gardenia_jasminoides
            { 9, UnknownSpeciesId } // unknown_plant
        };

        // V6 ML Label string to Canonical Species ID
        private static readonly Dictionary<string, string> LabelToSpeciesMap = new Dictionary<string, string>
        {
            { "01_alstonia_scholaris", "SP001" },
            { "02_justicia_adhatoda", "SP002" },
            { "03_acalypha_indica", "SP003" },
            { "04_mimusops_elengi", "SP004" },
            { "05_caryota_urens", "SP005" },
            { "07_strobilanthes_kunthiana", "SP007" },
            { "08_plumeria_alba", "SP008" },
            { "09_bougainvillea_spectabilis", "SP009" },
            { "10_gardenia_jasminoides", "SP010" },
            { "unknown_plant", UnknownSpeciesId }
        };

        // Recognized V6 species IDs set (excludes SP006 and UNKNOWN)
        private static readonly HashSet<string> V6RecognizedSpeciesSet = new HashSet<string>
        {
            "SP001", "SP002", "SP003", "SP004", "SP005", "SP007", "SP008", "SP009", "SP010"
        };

        /// <summary>
        /// Resolves a V6 class index (0-9) to its canonical species ID.
        /// Returns "UNKNOWN" for class 9 or any invalid index.
        /// </summary>
        public static string FromV6ClassIndex(int classIndex)
        {
            if (IndexToSpeciesMap.TryGetValue(classIndex, out string speciesId))
            {
                return speciesId;
            }
            return UnknownSpeciesId;
        }

        /// <summary>
        /// Resolves a V6 model label string to its canonical species ID.
        /// Returns "UNKNOWN" for "unknown_plant", null, or unrecognized labels.
        /// </summary>
        public static string FromV6Label(string v6Label)
        {
            if (string.IsNullOrEmpty(v6Label))
            {
                return UnknownSpeciesId;
            }
            if (LabelToSpeciesMap.TryGetValue(v6Label.Trim(), out string speciesId))
            {
                return speciesId;
            }
            return UnknownSpeciesId;
        }

        /// <summary>
        /// Attempts to get a confirmed species ID from a V6 class index.
        /// Returns true only for recognized species (0-8 -> SP001-SP010 excluding SP006).
        /// Returns false for class 9 (unknown_plant) or out-of-range index.
        /// </summary>
        public static bool TryGetSpeciesId(int classIndex, out string speciesId)
        {
            speciesId = FromV6ClassIndex(classIndex);
            return speciesId != UnknownSpeciesId;
        }

        /// <summary>
        /// Attempts to get a confirmed species ID from a V6 label string.
        /// Returns true only for recognized species labels.
        /// Returns false for "unknown_plant", empty, or unrecognized labels.
        /// </summary>
        public static bool TryGetSpeciesId(string v6Label, out string speciesId)
        {
            speciesId = FromV6Label(v6Label);
            return speciesId != UnknownSpeciesId;
        }

        /// <summary>
        /// Checks if a given species ID is recognized by the current V6 ML model.
        /// Crucial rule: Returns false for SP006, as SP006 exists in the knowledge layer only.
        /// </summary>
        public static bool IsV6RecognizedSpecies(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return false;
            return V6RecognizedSpeciesSet.Contains(speciesId);
        }

        /// <summary>
        /// Resolves any input (canonical species ID, V6 label string, or unknown) to a canonical species ID.
        /// Returns "UNKNOWN" if unrecognized, null, or unknown.
        /// </summary>
        public static string ResolveSpeciesId(string input)
        {
            if (string.IsNullOrEmpty(input)) return UnknownSpeciesId;
            string trimmed = input.Trim();
            if (trimmed.StartsWith("SP", System.StringComparison.OrdinalIgnoreCase) && trimmed.Length == 5)
            {
                return trimmed.ToUpperInvariant();
            }
            if (LabelToSpeciesMap.TryGetValue(trimmed, out string mapped))
            {
                return mapped;
            }
            return UnknownSpeciesId;
        }
    }
}
