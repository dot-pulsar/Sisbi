using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;
using Models.Enums;
using Sisbi.Extensions;

namespace Sisbi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class VacanciesController : ControllerBase
    {
        private readonly SisbiContext _context;

        public VacanciesController(SisbiContext context)
        {
            _context = context;
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllQuery query)
        {
            var userId = Guid.Empty;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                userId = User.Id();
                Console.WriteLine(userId);
            }
            var result = await _context.Vacancies
                .Skip(query.Offset)
                .Take(query.Count)
                .Where(r => query.UserId == Guid.Empty ? r.UserId != Guid.Empty : r.UserId == query.UserId)
                .Where(r => query.All ? r.UserId != Guid.Empty : r.UserId != userId)
                .Select(r => new
                {
                    id = r.Id,
                    position = r.Position,
                    salary = r.Salary,
                    city = new
                    {
                        id = r.City.Id,
                        name = r.City.Name
                    },
                    schedule = r.Schedule,
                    description = r.Description,
                    user_id = r.UserId
                })
                .ToListAsync();


            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [AllowAnonymous, HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            var vacancy = await _context.Vacancies.FindAsync(id);

            if (vacancy == null)
            {
                return BadRequest("Такой вакансии не найдено!");
            }

            return Ok(new
            {
                success = true,
                id = vacancy.Id,
                position = vacancy.Position,
                salary = vacancy.Salary,
                city = new
                {
                    id = vacancy.City.Id,
                    name = vacancy.City.Name
                },
                schedule = vacancy.Schedule,
                description = vacancy.Description,
                user_id = vacancy.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpPost]
        public async Task<IActionResult> Create([FromBody] VacancyRequest body)
        {
            //TODO: добавить проверку полей в body

            var userId = User.Id();
            var user = await _context.Users.FindAsync(userId);

            if (string.IsNullOrEmpty(user.FirstName) ||
                string.IsNullOrEmpty(user.SecondName) ||
                string.IsNullOrEmpty(user.BDate) ||
                user.Gender == Gender.Unknown)
            {
                return BadRequest("Профиль не заполнен");
            }

            var vacancy = new Vacancy
            {
                Position = body.Position,
                Salary = body.Salary,
                CityId = body.CityId,
                Schedule = body.Schedule,
                Description = body.Description,
                Phone = body.Phone,
                Email = body.Email,
                UserId = userId,
                Status = AdStatus.Created.ToString()
            };

            await _context.Vacancies.AddAsync(vacancy);
            await _context.SaveChangesAsync();

            var city = await _context.Cities.FindAsync(vacancy.CityId);

            return Ok(new
            {
                success = true,
                id = vacancy.Id,
                position = vacancy.Position,
                salary = vacancy.Salary,
                city = new
                {
                    id = city.Id,
                    name = city.Name
                },
                schedule = vacancy.Schedule,
                description = vacancy.Description,
                phone = vacancy.Phone,
                email = vacancy.Email,
                status = vacancy.Status,
                user_id = vacancy.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] VacancyRequest body)
        {
            //TODO: добавить проверку полей в body

            var vacancy = await _context.Vacancies.FindAsync(id);

            if (vacancy == null)
            {
                return BadRequest("Такой вакансии не найдено!");
            }

            if (vacancy.UserId != User.Id())
            {
                return BadRequest("У вас нет прав на изменение этой вакансии!");
            }

            vacancy.Position = body.Position;
            vacancy.Salary = body.Salary;
            vacancy.CityId = body.CityId;
            vacancy.Schedule = body.Schedule;
            vacancy.Description = body.Description;
            vacancy.Phone = body.Phone;
            vacancy.Email = body.Email;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = vacancy.Id,
                position = vacancy.Position,
                salary = vacancy.Salary,
                city = new
                {
                    id = vacancy.City.Id,
                    name = vacancy.City.Name
                },
                schedule = vacancy.Schedule,
                description = vacancy.Description,
                phone = vacancy.Phone,
                email = vacancy.Email,
                user_id = vacancy.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var vacancy = await _context.Vacancies.FindAsync(id);

            if (vacancy == null)
            {
                return BadRequest("Такой вакансии не найдено!");
            }

            if (vacancy.UserId != User.Id())
            {
                return BadRequest("У вас нет прав на удаление этой вакансии!");
            }

            _context.Vacancies.Remove(vacancy);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        public class VacancyRequest
        {
            [JsonPropertyName("position")] public string Position { get; set; }
            [JsonPropertyName("salary")] public long Salary { get; set; }
            [JsonPropertyName("city_id")] public Guid CityId { get; set; }
            [JsonPropertyName("schedule")] public string Schedule { get; set; }
            [JsonPropertyName("description")] public string Description { get; set; }
            [JsonPropertyName("email")] public string Email { get; set; }
            [JsonPropertyName("phone")] public string Phone { get; set; }
        }

        public class GetAllQuery
        {
            [FromQuery(Name = "user_id")] public Guid UserId { get; set; }
            [FromQuery(Name = "offset")] public int Offset { get; set; } = 0;
            [FromQuery(Name = "count")] public int Count { get; set; } = 20;
            [FromQuery(Name = "all")] public bool All { get; set; } = true;
        }
    }
}