using System.Runtime.Serialization;

namespace Models
{
    public enum OtpType
    {
        [EnumMember(Value = "two_factor_authentication")] TwoFactorAuthentication,
        [EnumMember(Value = "sign_up")] SignUp,
    }
}