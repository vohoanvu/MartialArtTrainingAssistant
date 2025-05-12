using System.Text.Json.Serialization;

namespace SharedEntities.Models
{
    public class AiAnalysisResultResponse
    {
        [JsonPropertyName("strengths")]
        public List<Strength> Strengths { get; set; }

        [JsonPropertyName("suggested_drills")]
        public List<SuggestedDrill> SuggestedDrills { get; set; }

        [JsonPropertyName("overall_description")]
        public string OverallDescription { get; set; }

        [JsonPropertyName("areas_for_improvement")]
        public List<AreaForImprovement> AreasForImprovement { get; set; }

        [JsonPropertyName("techniques_identified")]
        public List<TechniqueIdentified> TechniquesIdentified { get; set; }
    }

    public class Strength
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("related_technique")]
        public string? RelatedTechnique { get; set; }
    }

    public class SuggestedDrill
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("focus")]
        public string Focus { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("related_technique")]
        public string RelatedTechnique { get; set; }
    }

    public class AreaForImprovement
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("weakness_category")]
        public string? WeaknessCategory { get; set; }

        [JsonPropertyName("related_technique")]
        public string? RelatedTechnique { get; set; }
    }

    public class TechniqueIdentified
    {
        [JsonPropertyName("start_timestamp")]
        public string StartTimestamp { get; set; }
        [JsonPropertyName("end_timestamp")]
        public string EndTimestamp { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("technique_name")]
        public string TechniqueName { get; set; }

        [JsonPropertyName("technique_type")]
        public string TechniqueType { get; set; }

        [JsonPropertyName("positional_scenario")]
        public string PositionalScenario { get; set; }
    }
}