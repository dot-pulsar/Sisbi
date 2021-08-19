using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Enums
{
    public class WorkExperience
    {
        public const string NotExperience = "без опыта";
        public const string FromOneToThreeYears = "от 1 до 3 лет";
        public const string FromThreeToSixYears = "от 3 до 6 лет";
        public const string OverSixYears = "больше 6 лет";
    }

    public enum WorkExperiences
    {
        [Column("default")] Default,
        [Column("not_experience")] NotExperience,
        [Column("from_one_to_three_years")]  FromOneToThreeYears,
        [Column("from_three_to_six_years")]  FromThreeToSixYears,
        [Column("over_six_years")]  OverSixYears,
    }
}