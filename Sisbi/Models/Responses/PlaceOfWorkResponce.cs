using System;
using System.Text.Json.Serialization;

namespace Models.Responses
{
    public class PlaceOfWorkResponse
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("position")] public string Position { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("start_date")] public string StartDate { get; set; }
        [JsonPropertyName("end_date")] public string EndDate { get; set; }
        [JsonPropertyName("resume_id")] public Guid ResumeId { get; set; }
    }
}