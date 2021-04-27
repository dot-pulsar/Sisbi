using System.Runtime.Serialization;

namespace Models
{
    public enum OtpType
    {
        [EnumMember(Value = "bad_type")] BadType,
        [EnumMember(Value = "sign_up")] SignUp,
        [EnumMember(Value = "two_factor_authentication")] TwoFactorAuthentication
    }
}