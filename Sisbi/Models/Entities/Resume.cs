using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Enums;

namespace Models.Entities
{
    [Table("resume")]
    public class Resume
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("position")] public string Position { get; set; }
        [Column("salary")] public long Salary { get; set; }
        [Column("city_id")] public Guid CityId { get; set; }
        [Column("schedule")] public string Schedule { get; set; }
        [Column("description")] public string Description { get; set; }
        [Column("email")] public string Email { get; set; }
        [Column("phone")] public string Phone { get; set; }
        [Column("status")] public string Status { get; set; }
        [Column("work_experience")] public int WorkExperience { get; set; }
        [Column("date_of_creation")] public long DateOfCreation { get; set; }
        [Column("date_of_change")] public long DateOfChange { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }

        public virtual User User { get; set; }
        public virtual City City { get; set; }
        public virtual ICollection<PlaceOfWork> PlacesOfWork { get; set; }
        public virtual ICollection<Response> Responses { get; set; }
        public virtual ICollection<ResumeVideo> Videos { get; set; }
        public virtual ICollection<ResumePoster> Posters { get; set; }
    }
}