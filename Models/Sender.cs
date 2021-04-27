using System.Runtime.Serialization;

namespace Models
{
    public enum Sender
    {
        [EnumMember(Value = "worker")] Worker,
        [EnumMember(Value = "employer")] Employer
    }
}