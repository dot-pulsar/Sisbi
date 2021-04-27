using System;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Enums;

namespace Models.Entities
{
    [Table("place_of_work")]
    public class PlaceOfWork
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("position")] public string Position { get; set; }
        [Column("company")] public string Company { get; set; }
        [Column("description")] public string Description { get; set; }
        [Column("start_date")] public string StartDate { get; set; }
        [Column("end_date")] public string EndDate { get; set; }
        [Column("resume_id")] public Guid ResumeId { get; set; }
        public virtual Resume Resume { get; set; }

    }
}
