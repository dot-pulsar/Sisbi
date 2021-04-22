using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("response")]
    public class Response
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("resume_id")] public Guid ResumeId { get; set; }
        [Column("vacancy_id")] public Guid VacancyId { get; set; }
        [Column("sender")] public string Sender { get; set; }
    }
}