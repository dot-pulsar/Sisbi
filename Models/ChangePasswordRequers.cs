using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Models
{
    public class ChangePasswordRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        [JsonPropertyName("new_password")] public string NewPassword { get; set; }
    }
}