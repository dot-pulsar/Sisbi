using System.Runtime.Serialization;

namespace Models
{
    public enum LoginType
    {
        [EnumMember(Value = "phone")] Phone,
        [EnumMember(Value = "email")] Email,
        BadLogin
    }
}