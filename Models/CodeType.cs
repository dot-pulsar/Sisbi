using System.Runtime.Serialization;

namespace Models
{
    public enum CodeType
    {
        [EnumMember(Value = "sing_up")] SingUp,
        [EnumMember(Value = "restore_password")] RestorePassword,
    }
}