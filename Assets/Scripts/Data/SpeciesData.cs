using UnityEngine;

[System.Serializable]
public struct SpeciesData
{
    public string speciesId;
    public string commonName;
    public string scientificName;
    public string family;
    public string nativeRegion;
    [TextArea(3, 5)]
    public string description;
    [TextArea(3, 5)]
    public string ecologicalImportance;
    public string conservationStatus;
}
