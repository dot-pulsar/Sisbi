using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("resume_poster")]
    public class ResumePoster
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("name")] public string Name { get; set; }
        [Column("format")] public string Format { get; set; }
        [Column("type")] public string Type { get; set; }
        [Column("selected")] public bool Selected { get; set; }
        [Column("number")] public int Number { get; set; }
        [Column("resume_id")] public Guid ResumeId { get; set; }
        public virtual Resume Resume { get; set; }
        [NotMapped] public string Urn => $"images/{Name}.{Format}";
    }
}