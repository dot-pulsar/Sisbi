using System;
using System.Text.Json.Serialization;

namespace Sisbi.Controllers
{
    public class CreateBodyResume
    {
        [JsonPropertyName("position")] public string Position { get; set; }
        [JsonPropertyName("salary")] public long Salary { get; set; }
        [JsonPropertyName("city_id")] public Guid CityId { get; set; }
        [JsonPropertyName("schedule")] public string Schedule { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
    }
}