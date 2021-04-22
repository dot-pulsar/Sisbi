using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("favorite_resume")]
    public class FavoriteResume
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("resume_id")] public Guid ResumeId { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }
        public virtual Resume Resume { get; set; }
        public virtual User User { get; set; }
    }
}