using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Models
{
    public enum Role
    {
        [EnumMember(Value = "bad_role")] BadRole,
        [EnumMember(Value = "worker")] Worker,
        [EnumMember(Value = "employer")] Employer,
        [EnumMember(Value = "moderator")] Moderator,
        [EnumMember(Value = "administrator")] Administrator
    }
}