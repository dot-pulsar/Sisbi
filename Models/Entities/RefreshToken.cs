using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("refresh_token")]
    public class RefreshToken
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("token")] public string Token { get; set; }
        [Column("expire_in")] public long ExpireIn { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }

        public virtual User User { get; set; }
    }
}