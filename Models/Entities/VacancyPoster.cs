using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("vacancy_poster")]
    public class VacancyPoster
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("name")] public string Name { get; set; }
        [Column("format")] public string Format { get; set; }
        [Column("vacancy_id")] public Guid VacancyId { get; set; }
        public virtual Vacancy Vacancy { get; set; }
        [NotMapped] public string Urn => $"images/{Name}.{Format}";
    }
}