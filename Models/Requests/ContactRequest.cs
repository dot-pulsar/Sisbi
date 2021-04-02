using System;
using System.Text.Json.Serialization;

namespace Models.Requests
{
    public class ContactRequest
    {
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("resume_id")] public Guid ResumeId { get; set; }
    }
}