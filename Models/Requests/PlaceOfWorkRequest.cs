using System;
using System.Text.Json.Serialization;

namespace Models.Requests
{
    public class PlaceOfWorkRequest
    {
        [JsonPropertyName("position")] public string Position { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("start_date")] public string StartDate { get; set; }
        [JsonPropertyName("end_date")] public string EndDate { get; set; }
        [JsonPropertyName("resume_id")] public Guid ResumeId { get; set; }
    }
}