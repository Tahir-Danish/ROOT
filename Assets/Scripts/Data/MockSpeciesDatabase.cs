using System.Collections.Generic;
using UnityEngine;

public static class MockSpeciesDatabase
{
    private static Dictionary<string, SpeciesData> _database = new Dictionary<string, SpeciesData>()
    {
        {
            "SP01",
            new SpeciesData
            {
                speciesId = "SP01",
                commonName = "Rubber Fig",
                scientificName = "Ficus elastica",
                family = "Moraceae",
                nativeRegion = "South Asia & Southeast Asia",
                description = "A large tree in the banyan group of figs, known for its thick, shiny, oval leaves.",
                ecologicalImportance = "Provides habitat and food for wildlife in its native range. Globally significant as an ornamental plant known for robust growth and indoor air purification.",
                conservationStatus = "Least Concern"
            }
        }
    };

    public static bool TryGetSpeciesData(string speciesId, out SpeciesData data)
    {
        return _database.TryGetValue(speciesId, out data);
    }
}
