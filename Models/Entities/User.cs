using System;

namespace Models.Entities
{
    public class User
    {
        public Guid id { get; set; }
        public string first_name { get; set; }
        public string second_name { get; set; }
        public string middle_name { get; set; }
        public long date_of_birth { get; set; }
        public string address { get; set; }
        public int otp { get; set; }
        public long otp_date { get; set; }
        public short otp_retry { get; set; }
        public OtpType otp_type { get; set; }

        public string phone { get; set; }
        public bool phone_confirmed { get; set; }
        public string email { get; set; }
        public bool email_confirmed { get; set; }
        public string password { get; set; }
        public string salt { get; set; }
        public Role role { get; set; }
        public long registration_date { get; set; }

        public bool AlreadyRegistered => registration_date != 0;
    }
}