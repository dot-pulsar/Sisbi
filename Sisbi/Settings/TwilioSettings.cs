using System.Security.AccessControl;
using System.Text.Json.Serialization;

namespace Sisbi.Settings
{
    public class TwilioSettings
    {
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
    }
}