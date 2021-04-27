using System;
using System.Text.Json.Serialization;

namespace Models.Requests
{
    public class PlaceOfWorkRequest
    {
        [JsonPropertyName("position")] public string Position { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("start_date")] public DateTime StartDate { get; set; }
        [JsonPropertyName("end_date")] public DateTime EndDate { get; set; }
    }
}