using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
        private readonly SisbiContext _context;

        public ResumesController(SisbiContext context)
        {
            _context = context;
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllQuery query)
        {
            object result;
            if (query.UserId == Guid.Empty)
            {
                result = _context.Resumes
                    .Skip(query.Offset)
                    .Take(query.Count)
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
                        user_id = r.UserId,
                        places_of_work = r.PlaceOfWorks.Select(pow => new
                        {
                            id = pow.Id,
                            position = pow.Position,
                            company = pow.Company,
                            description = pow.Description,
                            start_date = pow.StartDate,
                            end_date = pow.EndDate,
                            resume_id = pow.ResumeId
                        })
                    });
            }
            else
            {
                result = _context.Resumes
                    .Where(r => r.UserId == query.UserId)
                    .Skip(query.Offset)
                    .Take(query.Count)
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
                        user_id = r.UserId,
                        places_of_work = r.PlaceOfWorks.Select(pow => new
                        {
                            id = pow.Id,
                            position = pow.Position,
                            company = pow.Company,
                            description = pow.Description,
                            start_date = pow.StartDate,
                            end_date = pow.EndDate,
                            resume_id = pow.ResumeId
                        })
                    });
            }

            return Ok(new
            {
                success = true,
                resumes = result
            });
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
            var resume = await _context.Resumes.FindAsync(id);

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
                city = new
                {
                    id = resume.City.Id,
                    name = resume.City.Name
                },
                schedule = resume.Schedule,
                description = resume.Description,
                user_id = resume.UserId,
                places_of_work = resume.PlaceOfWorks.Select(pow => new
                {
                    id = pow.Id,
                    position = pow.Position,
                    company = pow.Company,
                    description = pow.Description,
                    start_date = pow.StartDate,
                    end_date = pow.EndDate,
                    resume_id = pow.ResumeId
                })
            });
        }

        [Authorize(Roles = "Worker"), HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBody body)
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

            var resume = new Resume
            {
                Position = body.Position,
                Salary = body.Salary,
                CityId = body.CityId,
                Schedule = body.Schedule,
                Description = body.Description,
                Phone = body.Phone,
                Email = body.Email,
                UserId = userId
            };

            await _context.Resumes.AddAsync(resume);
            await _context.SaveChangesAsync();

            var city = await _context.Cities.FindAsync(resume.CityId);

            return Ok(new
            {
                success = true,
                id = resume.Id,
                position = resume.Position,
                salary = resume.Salary,
                city = new
                {
                    id = city.Id,
                    name = city.Name
                },
                schedule = resume.Schedule,
                description = resume.Description,
                phone = resume.Phone,
                email = resume.Email,
                user_id = resume.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] EditBody body)
        {
            //TODO: добавить проверку полей в body
            
            var resume = await _context.Resumes.FindAsync(id);

            if (resume == null)
            {
                return BadRequest("Такого резюме не найдено!");
            }

            if (resume.UserId != User.Id())
            {
                return BadRequest("У вас нет прав на изменение этого резюме!");
            }

            resume.Position = body.Position;
            resume.Salary = body.Salary;
            resume.CityId = body.CityId;
            resume.Schedule = body.Schedule;
            resume.Description = body.Description;
            resume.Phone = body.Phone;
            resume.Email = body.Email;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = resume.Id,
                position = resume.Position,
                salary = resume.Salary,
                city = new
                {
                    id = resume.City.Id,
                    name = resume.City.Name
                },
                schedule = resume.Schedule,
                description = resume.Description,
                phone = resume.Phone,
                email = resume.Email,
                places_of_work = resume.PlaceOfWorks.Select(pow => new
                {
                    id = pow.Id,
                    position = pow.Position,
                    company = pow.Company,
                    description = pow.Description,
                    start_date = pow.StartDate,
                    end_date = pow.EndDate,
                    resume_id = pow.ResumeId
                }),
                user_id = resume.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var resume = await _context.Resumes.FindAsync(id);

            if (resume == null)
            {
                return BadRequest("Такого резюме не найдено!");
            }

            if (resume.UserId != User.Id())
            {
                return BadRequest("У вас нет прав на удаление этого резюме!");
            }

            _context.Resumes.Remove(resume);
            await _context.SaveChangesAsync();

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
            [JsonPropertyName("email")] public string Email { get; set; }
            [JsonPropertyName("phone")] public string Phone { get; set; }
        }

        public class EditBody
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

/*var resumes = _context.Resumes
    .Skip(query.Offset)
    .Take(query.Count)
    .Select(r => new
    {
        id = r.Id,
        position = r.Position,
        salary = r.Salary,
        city_id = r.CityId,
        schedule = r.Schedule,
        description = r.Description,
        user_id = r.UserId,
        contact = new
        {
            id = r.Contact.Id,
            phone = r.Contact.Phone,
            email = r.Contact.Email,
            resume_id = r.Contact.ResumeId
        },
        places_of_work = r.PlaceOfWorks.Select(pow => new
        {
            id = pow.Id,
            position = pow.Position,
            company = pow.Company,
            description = pow.Description,
            start_date = pow.StartDate,
            end_date = pow.EndDate,
            resume_id = pow.ResumeId
        })
    });

return Ok(new
{
    success = true,
    resumes
});*/