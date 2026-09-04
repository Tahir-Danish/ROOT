using System.Collections.Generic;
using UnityEngine;

public static class MockSpeciesDatabase
{
    private static Dictionary<string, SpeciesData> _database = new Dictionary<string, SpeciesData>()
    {
        {
            "01_alstonia_scholaris",
            new SpeciesData
            {
                speciesId = "01_alstonia_scholaris",
                commonName = "Data Unavailable",
                scientificName = "Alstonia scholaris",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "02_justicia_adhatoda",
            new SpeciesData
            {
                speciesId = "02_justicia_adhatoda",
                commonName = "Data Unavailable",
                scientificName = "Justicia adhatoda",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "03_acalypha_indica",
            new SpeciesData
            {
                speciesId = "03_acalypha_indica",
                commonName = "Data Unavailable",
                scientificName = "Acalypha indica",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "04_mimusops_elengi",
            new SpeciesData
            {
                speciesId = "04_mimusops_elengi",
                commonName = "Data Unavailable",
                scientificName = "Mimusops elengi",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "05_caryota_urens",
            new SpeciesData
            {
                speciesId = "05_caryota_urens",
                commonName = "Data Unavailable",
                scientificName = "Caryota urens",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "07_strobilanthes_kunthiana",
            new SpeciesData
            {
                speciesId = "07_strobilanthes_kunthiana",
                commonName = "Data Unavailable",
                scientificName = "Strobilanthes kunthiana",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "08_plumeria_alba",
            new SpeciesData
            {
                speciesId = "08_plumeria_alba",
                commonName = "Data Unavailable",
                scientificName = "Plumeria alba",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "09_bougainvillea_spectabilis",
            new SpeciesData
            {
                speciesId = "09_bougainvillea_spectabilis",
                commonName = "Data Unavailable",
                scientificName = "Bougainvillea spectabilis",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "10_gardenia_jasminoides",
            new SpeciesData
            {
                speciesId = "10_gardenia_jasminoides",
                commonName = "Data Unavailable",
                scientificName = "Gardenia jasminoides",
                family = "Data Unavailable",
                nativeRegion = "Data Unavailable",
                description = "Data Unavailable",
                ecologicalImportance = "Data Unavailable",
                conservationStatus = "Data Unavailable"
            }
        },
        {
            "unknown_plant",
            new SpeciesData
            {
                speciesId = "unknown_plant",
                commonName = "Unknown Species",
                scientificName = "Plantae incognita",
                family = "Unknown",
                nativeRegion = "Unknown",
                description = "The plant was detected but could not be confidently identified by the current model.",
                ecologicalImportance = "Unknown",
                conservationStatus = "Unknown"
            }
        }
    };

    public static bool TryGetSpeciesData(string speciesId, out SpeciesData data)
    {
        return _database.TryGetValue(speciesId, out data);
    }
}
