using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamCero.Data.SpeciesKnowledge
{
    [Serializable]
    public class KnowledgeSource
    {
        [JsonProperty("source_id")] public string source_id;
        [JsonProperty("source_name")] public string source_name;
    }

    [Serializable]
    public class ARInformation
    {
        [JsonProperty("quick_identification")] public string quick_identification;
        [JsonProperty("ecological_importance")] public string ecological_importance;
        [JsonProperty("conservation_message")] public string conservation_message;
    }

    [Serializable]
    public class CampusRelevance
    {
        [JsonProperty("status")] public string status;
        [JsonProperty("location")] public string location;
        [JsonProperty("verification_required")] public bool verification_required;
    }

    /// <summary>
    /// Strongly typed runtime representation of a plant species record
    /// strictly matching the schema in Species_Knowledge_Records.pdf.
    /// </summary>
    [Serializable]
    public class SpeciesKnowledgeRecord
    {
        [JsonProperty("species_id")] public string species_id;
        [JsonProperty("scientific_name")] public string scientific_name;
        [JsonProperty("accepted_name")] public string accepted_name;
        [JsonProperty("taxonomic_status")] public string taxonomic_status;
        [JsonProperty("synonyms")] public List<string> synonyms = new List<string>();
        [JsonProperty("common_names")] public List<string> common_names = new List<string>();
        [JsonProperty("family")] public string family;
        [JsonProperty("genus")] public string genus;
        [JsonProperty("native_region")] public string native_region;
        [JsonProperty("habitat")] public string habitat;
        [JsonProperty("growth_form")] public string growth_form;
        [JsonProperty("ecological_importance")] public string ecological_importance;
        [JsonProperty("conservation_status")] public string conservation_status;
        [JsonProperty("threats")] public List<string> threats = new List<string>();
        [JsonProperty("conservation_actions")] public List<string> conservation_actions = new List<string>();
        [JsonProperty("field_identification_notes")] public List<string> field_identification_notes = new List<string>();
        [JsonProperty("ml_relevance")] public string ml_relevance;
        [JsonProperty("ar_information")] public ARInformation ar_information = new ARInformation();
        [JsonProperty("vit_campus_relevance")] public CampusRelevance vit_campus_relevance = new CampusRelevance();
        [JsonProperty("confidence_level")] public string confidence_level;
        [JsonProperty("sources")] public List<KnowledgeSource> sources = new List<KnowledgeSource>();
        [JsonProperty("last_verified")] public string last_verified;
    }
}
