using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("favorite_vacancy")]
    public class FavoriteVacancy
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("vacancy_id")] public Guid VacancyId { get; set; }
        [Column("user_id")] public Guid UserId { get; set; }
        public virtual Vacancy Vacancy { get; set; }
        public virtual User User { get; set; }
    }
}