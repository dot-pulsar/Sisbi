using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Enums
{
    public enum AdStatus
    {
        [Column("created")] Created,
        [Column("sended")] Sended,
        [Column("confirmed")] Confirmed,
        [Column("not_confirmed")] NotConfirmed
    }
}