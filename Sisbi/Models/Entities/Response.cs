using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

namespace Models.Entities
{
    [Table("response")]
    public class Response
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("resume_id")] public Guid ResumeId { get; set; }
        [Column("vacancy_id")] public Guid VacancyId { get; set; }
        [Column("sender")] public string Sender { get; set; }
        [Column("status")] public string Status { get; set; }
        
        public virtual Resume Resume { get; set; }
        public virtual Vacancy Vacancy { get; set; }
    }
}