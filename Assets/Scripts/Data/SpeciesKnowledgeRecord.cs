using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BotanicalChapter
{
    public string title;
    public string content;
}

[System.Serializable]
public class ConservationInfo
{
    public string status;
    public List<string> threats;
    public List<string> actions;
}

[System.Serializable]
public class SpeciesKnowledgeRecord
{
    public string species_id;
    public string scientific_name;
    public string common_name;
    public string family;
    public string native_region;
    public string ecological_importance;
    public ConservationInfo conservation;
    public List<BotanicalChapter> botanical_chapters;
}
