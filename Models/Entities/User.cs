using System;
using Models.Enums;

namespace Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public Gender Gender { get; set; }
        public string BDate { get; set; }
        public string Address { get; set; }
        public int Otp { get; set; }
        public long OtpDate { get; set; }
        public short OtpRetry { get; set; }
        public OtpType OtpType { get; set; }
        public string Phone { get; set; }
        public bool PhoneConfirmed { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public Role Role { get; set; }
        public long RegistrationDate { get; set; }

        public bool AlreadyRegistered => RegistrationDate != 0;
    }
}