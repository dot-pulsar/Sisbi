using System;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Enums;

namespace Models.Entities
{
    [Table("user")]
    public class User
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("first_name")] public string FirstName { get; set; }
        [Column("second_name")] public string SecondName { get; set; }
        [Column("company")] public string Company { get; set; }
        [Column("gender",TypeName = "text")] public Gender Gender { get; set; }
        [Column("bdate")] public string BDate { get; set; }
        [Column("address")] public string Address { get; set; }
        [Column("otp")] public int? Otp { get; set; }
        [Column("otp_date")] public long? OtpDate { get; set; }
        [Column("otp_retry")] public short? OtpRetry { get; set; }
        [Column("otp_type",TypeName = "text")] public OtpType OtpType { get; set; }
        [Column("phone")] public string Phone { get; set; }
        [Column("phone_confirmed")] public bool PhoneConfirmed { get; set; }
        [Column("email")] public string Email { get; set; }
        [Column("email_confirmed")] public bool EmailConfirmed { get; set; }
        [Column("password")] public string Password { get; set; }
        [Column("salt")] public string Salt { get; set; }
        [Column("role",TypeName = "text")] public Role Role { get; set; }
        [Column("avatar")] public string Avatar { get; set; }
        [Column("registration_date")] public long RegistrationDate { get; set; }

        [NotMapped] public bool AlreadyRegistered => RegistrationDate != 0;
    }
}