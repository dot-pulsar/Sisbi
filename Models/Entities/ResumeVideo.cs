using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("resume_video")]
    public class ResumeVideo
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("name")] public string Name { get; set; }
        [Column("format")] public string Format { get; set; }
        [Column("resume_id")] public Guid ResumeId { get; set; }
        public virtual Resume Resume { get; set; }
        [NotMapped] public string Urn => $"videos/{Name}.{Format}";
    }
}