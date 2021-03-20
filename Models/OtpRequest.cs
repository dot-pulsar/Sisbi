using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class OtpRequest
    {
        public string Login { get; set; }
        public Role Role { get; set; }
    }
}