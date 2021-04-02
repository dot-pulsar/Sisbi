using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.Entities;
using Models.Enums;
using Sisbi.Extensions;

namespace Sisbi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ResumesController : ControllerBase
    {
        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllQuery query)
        {
            IEnumerable<dynamic> resumes;
            if (query.UserId == Guid.Empty)
            {
                if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Worker"))
                {
                    resumes = await SisbiContext.Connection.QueryAsync("SELECT * FROM resume");
                }
                else
                {
                    resumes = await SisbiContext.Connection.QueryAsync("SELECT * FROM resume");
                }
            }
            else
            {
                if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Worker"))
                {
                    resumes = await SisbiContext.Connection.QueryAsync("SELECT * FROM resume");
                }
                else
                {
                    resumes = await SisbiContext.Connection.QueryAsync("SELECT * FROM resume");
                }
            }

            return Ok(resumes);
        }

        /*[AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllQuery query)
        {
            IEnumerable<Resume> resumes;

            if (query.UserId == Guid.Empty)
            {
                resumes = await SisbiContext.Connection.QueryAsync<Resume>(
                    $"SELECT * FROM resume LIMIT {query.Count} OFFSET {query.Offset}");
            }
            else
            {
                resumes = await SisbiContext.Connection.QueryAsync<Resume>(
                    $"SELECT * FROM resume WHERE user_id = '{query.UserId}' LIMIT {query.Count} OFFSET {query.Offset}");
            }

            return Ok(new
            {
                success = true,
                resumes = resumes.Select(r => new
                {
                    id = r.Id,
                    position = r.Position,
                    salary = r.Salary,
                    city_id = r.CityId,
                    schedule = r.Schedule,
                    description = r.Description,
                    user_id = r.UserId
                })
            });
        }*/

        [AllowAnonymous, HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            var resume = await SisbiContext.Connection.QuerySingleOrDefaultAsync<Resume>(
                $"SELECT * FROM resume WHERE id = '{id}'");

            if (resume == null)
            {
                return BadRequest("Такого резюме не найдено!");
            }

            return Ok(new
            {
                success = true,
                id = resume.Id,
                position = resume.Position,
                salary = resume.Salary,
                city_id = resume.CityId,
                schedule = resume.Schedule,
                description = resume.Description,
                user_id = resume.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBody body)
        {
            //TODO: убрать перезапись данных

            var userId = User.Id();
            var user = await SisbiContext.GetAsync<User>(userId);

            if (string.IsNullOrEmpty(user.FirstName) ||
                string.IsNullOrEmpty(user.SecondName) ||
                string.IsNullOrEmpty(user.BDate) ||
                user.Gender == Gender.Unknown)
            {
                return BadRequest("Профиль не заполнен");
            }

            var resume = await SisbiContext.CreateAsync<Resume>(new
            {
                position = body.Position,
                salary = body.Salary,
                city_id = body.CityId,
                schedule = body.Schedule,
                description = body.Description,
            }, returning: true);

            return Ok(new
            {
                success = true,
                id = resume.Id,
                position = resume.Position,
                salary = resume.Salary,
                city_id = resume.CityId,
                schedule = resume.Schedule,
                description = resume.Description,
                user_id = userId
            });
        }

        [Authorize(Roles = "Worker"), HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] EditBody body)
        {
            var resume = await SisbiContext.GetAsync<Resume>(id);

            if (resume == null)
            {
                return BadRequest("Такого резюме не найдено!");
            }

            if (resume.UserId != User.Id())
            {
                return BadRequest("У вас нет прав на изменение этого резюме!");
            }

            resume = await SisbiContext.UpdateAsync<Resume>(id, new
            {
                position = body.Position,
                salary = body.Salary,
                city_id = body.CityId,
                schedule = body.Schedule,
                description = body.Description,
            }, returning: true);

            return Ok(new
            {
                success = true,
                id = resume.Id,
                position = resume.Position,
                salary = resume.Salary,
                city_id = resume.CityId,
                schedule = resume.Schedule,
                description = resume.Description,
                user_id = resume.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var resume = await SisbiContext.GetAsync<Resume>(id);

            if (resume == null)
            {
                return BadRequest("Такого резюме не найдено!");
            }

            if (resume.UserId != User.Id())
            {
                return BadRequest("У вас нет прав на удаление этого резюме!");
            }

            await SisbiContext.DeleteAsync<Resume>(id);

            return Ok(new
            {
                success = true
            });
        }

        public class CreateBody
        {
            [JsonPropertyName("position")] public string Position { get; set; }
            [JsonPropertyName("salary")] public long Salary { get; set; }
            [JsonPropertyName("city_id")] public Guid CityId { get; set; }
            [JsonPropertyName("schedule")] public string Schedule { get; set; }
            [JsonPropertyName("description")] public string Description { get; set; }
        }

        public class EditBody
        {
            [JsonPropertyName("position")] public string Position { get; set; }
            [JsonPropertyName("salary")] public long Salary { get; set; }
            [JsonPropertyName("city_id")] public Guid CityId { get; set; }
            [JsonPropertyName("schedule")] public string Schedule { get; set; }
            [JsonPropertyName("description")] public string Description { get; set; }
        }

        public class GetAllQuery
        {
            [FromQuery(Name = "user_id")] public Guid UserId { get; set; }
            [FromQuery(Name = "offset")] public int Offset { get; set; } = 0;
            [FromQuery(Name = "count")] public int Count { get; set; } = 100;
        }
    }
}

/*resumes = await SisbiContext.Connection.QueryAsync<Resume, PlaceOfWork, Resume>(
    "SELECT * FROM resume r LEFT JOIN place_of_work pow ON pow.resume_id = r.id",
    //WHERE user_id = {User.Id()} LIMIT {query.Count} OFFSET {query.Offset}
    (resume, placesOfWork) =>
    {
        resume.PlacesOfWork = placesOfWork;
        return resume;
    }, splitOn: "id,id");

var qq = resumes.GroupBy(r => r.Id).Select(r => new
{
    resume = new
    {
        id = r.Key,
        r.First().Position,
        r.First().Salary,
        r.First().CityId,
        r.First().Schedule,
        r.First().Description,
        r.First().UserId
    },
    places_of_work = r.Select(p => p.PlacesOfWork)
});

return Ok(qq);*/