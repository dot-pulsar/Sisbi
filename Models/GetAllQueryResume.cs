using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Models.Enums;

namespace Models
{
    public class GetAllQueryResume
    {
        [FromQuery(Name = "user_id")] public Guid UserId { get; set; }
        [FromQuery(Name = "position")] public string Position { get; set; }
        [FromQuery(Name = "page")] public int Page { get; set; } = 1;
        [FromQuery(Name = "limit")] public int Limit { get; set; } = 20;
        [FromQuery(Name = "all")] public bool All { get; set; } = true;
        [FromQuery(Name = "cities[]")] public List<Guid> Cities { get; set; }
        [FromQuery(Name = "min_work_exp")] public int MinWorkExperience { get; set; }
        [FromQuery(Name = "max_work_exp")] public int MaxWorkExperience { get; set; }
        [FromQuery(Name = "min_salary")] public long MinSalary { get; set; }
        [FromQuery(Name = "max_salary")] public long MaxSalary { get; set; }
        [FromQuery(Name = "schedule")] public Schedule Schedule { get; set; }
        [FromQuery(Name = "employment_type")] public EmploymentType EmploymentType { get; set; }
        [FromQuery(Name = "sort_by")] public string SortBy { get; set; }
    }
}