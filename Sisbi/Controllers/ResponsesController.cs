using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;
using Sisbi.Extensions;
using Sisbi.Hubs;

namespace Sisbi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ResponsesController : ControllerBase
    {
        private readonly SisbiContext _context;
        private readonly IHubContext<ResponseHub> _responseHub;

        public ResponsesController(IHubContext<ResponseHub> responseHub, SisbiContext context)
        {
            _context = context;
            _responseHub = responseHub;
        }

        [Authorize(Roles = "Worker,Employer"), HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllQuery query)
        {
            var result = await _context.Responses
                .Skip(query.Offset)
                .Take(query.Count)
                .Where(r => User.IsInRole("Worker")
                    ? r.Resume.UserId == User.Id()
                    : r.Vacancy.UserId == User.Id())
                .Select(r => new
                {
                    id = r.Id,
                    sender = r.Sender.ToString(),
                    resume = new
                    {
                        id = r.Resume.Id,
                        position = r.Resume.Position,
                        salary = r.Resume.Salary,
                        city = new
                        {
                            id = r.Resume.City.Id,
                            name = r.Resume.City.Name
                        },
                        schedule = r.Resume.Schedule,
                        description = r.Resume.Description,
                        user_id = r.Resume.UserId,
                        places_of_work = r.Resume.PlacesOfWork.Select(pow => new
                        {
                            id = pow.Id,
                            position = pow.Position,
                            company = pow.Company,
                            description = pow.Description,
                            start_date = pow.StartDate,
                            end_date = pow.EndDate,
                            resume_id = pow.ResumeId
                        })
                    },
                    vacancy = new
                    {
                        id = r.Vacancy.Id,
                        position = r.Vacancy.Position,
                        salary = r.Vacancy.Salary,
                        city = new
                        {
                            id = r.Vacancy.City.Id,
                            name = r.Vacancy.City.Name
                        },
                        schedule = r.Vacancy.Schedule,
                        description = r.Vacancy.Description,
                        user_id = r.Vacancy.UserId
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                succses = true,
                data = result
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpGet("{id}")]
        public async Task<IActionResult> Get([FromQuery] Guid id)
        {
            var response = await _context.Responses.FindAsync(id);

            if (response == null)
            {
                return BadRequest(new
                {
                    succses = false,
                    description = "Отклика с таким id не найдено!"
                });
            }

            if (User.IsInRole("Worker")
                ? User.Id() != response.Resume.UserId
                : User.Id() != response.Vacancy.UserId)
            {
                return BadRequest(new
                {
                    succses = false,
                    description = "У вас нет доступа к этому отклику!"
                });
            }

            return Ok(new
            {
                succses = true,
                id = response.Id,
                sender = response.Sender,
                status = response.Status,
                resume = new
                {
                    id = response.Resume.Id,
                    position = response.Resume.Position,
                    salary = response.Resume.Salary,
                    city = new
                    {
                        id = response.Resume.City.Id,
                        name = response.Resume.City.Name
                    },
                    schedule = response.Resume.Schedule,
                    description = response.Resume.Description,
                    status = response.Resume.Status,
                    videos = response.Resume.Videos.Select(v => new
                    {
                        name = v.Name,
                        format = v.Format,
                        path = v.Urn
                    }),
                    work_experience = response.Resume.WorkExperience,
                    places_of_work = response.Resume.PlacesOfWork
                        .Select(pow => new
                        {
                            id = pow.Id,
                            position = pow.Position,
                            company = pow.Company,
                            description = pow.Description,
                            start_date = pow.StartDate,
                            end_date = pow.EndDate,
                            resume_id = pow.ResumeId
                        })
                        .ToList(),
                    date_of_creation = response.Resume.DateOfCreation,
                    date_of_change = response.Resume.DateOfChange,
                    user_id = response.Resume.UserId
                },
                vacancy = new
                {
                    id = response.Id,
                    position = response.Vacancy.Position,
                    salary = response.Vacancy.Salary,
                    city = new
                    {
                        id = response.Vacancy.City.Id,
                        name = response.Vacancy.City.Name
                    },
                    schedule = response.Vacancy.Schedule,
                    description = response.Vacancy.Description,
                    work_experience = response.Vacancy.WorkExperience,
                    videos = response.Vacancy.Videos.Select(v => new
                    {
                        name = v.Name,
                        format = v.Format,
                        path = v.Urn
                    }),
                    poster = response.Vacancy.Posters.Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        format = p.Format,
                        type = p.Type,
                        selected = p.Selected,
                        number = p.Number,
                        path = p.Urn
                    }).SingleOrDefault(p => p.selected),
                    date_of_creation = response.Vacancy.DateOfCreation,
                    date_of_change = response.Vacancy.DateOfChange,
                    status = response.Vacancy.Status,
                    user_id = response.Vacancy.UserId
                }
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBody body)
        {
            var userId = User.Id();

            var resume = await _context.Resumes.FindAsync(body.ResumeId);
            var vacancy = await _context.Vacancies.FindAsync(body.VacancyId);

            if (resume == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume not found!"
                });
            }

            if (vacancy == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "vacancy not found!"
                });
            }

            if (User.IsInRole("Worker") && resume.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "у вас нет доступа к этому резюме!"
                });
            }

            if (User.IsInRole("Employer") && vacancy.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "у вас нет доступа к этой вакансии!"
                });
            }

            if (await _context.Responses.AnyAsync(r => r.ResumeId == resume.Id && r.VacancyId == vacancy.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Такой отклик уже существует!"
                });
            }
            
            const string status = "sended";
            var sender = User.IsInRole("Worker") ? "Worker" : "Employer";
            var recipientUserId = sender == "Worker" ? vacancy.UserId : resume.UserId;

            await _responseHub.Clients.All.SendAsync("Notification", new
            {
                resume_id = resume.Id,
                vacancy_id = vacancy.Id,
                sender = sender,
                status = status,
                recipient_user_id = recipientUserId
            });

            var response = new Response
            {
                ResumeId = resume.Id,
                VacancyId = vacancy.Id,
                Sender = sender,
                Status = "sended"
            };

            await _context.Responses.AddAsync(response);
            await _context.SaveChangesAsync();

            var result = new
            {
                id = response.Id,
                sender = response.Sender,
                status = response.Status,
                resume = new
                {
                    id = response.Resume.Id,
                    position = response.Resume.Position,
                    salary = response.Resume.Salary,
                    city = new
                    {
                        id = response.Resume.City.Id,
                        name = response.Resume.City.Name
                    },
                    schedule = response.Resume.Schedule,
                    description = response.Resume.Description,
                    user_id = response.Resume.UserId,
                    places_of_work = response.Resume.PlacesOfWork.Select(pow => new
                    {
                        id = pow.Id,
                        position = pow.Position,
                        company = pow.Company,
                        description = pow.Description,
                        start_date = pow.StartDate,
                        end_date = pow.EndDate,
                        resume_id = pow.ResumeId
                    })
                },
                vacancy = new
                {
                    id = response.Vacancy.Id,
                    company = response.Vacancy.User.Company,
                    position = response.Vacancy.Position,
                    salary = response.Vacancy.Salary,
                    city = new
                    {
                        id = response.Vacancy.City.Id,
                        name = response.Vacancy.City.Name
                    },
                    schedule = response.Vacancy.Schedule,
                    description = response.Vacancy.Description,
                    user_id = response.Vacancy.UserId
                }
            };

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var userId = User.Id();
            var response = await _context.Responses.FindAsync(id);

            if (response == null)
            {
                return BadRequest("Такого отклика не найдено!");
            }

            if (User.IsInRole("Worker") ? response.Resume.UserId != userId : response.Vacancy.UserId != userId)
            {
                return BadRequest("У вас нет прав на удаление этого отклика!");
            }

            //_context.Responses.Remove(response);

            response.Status = "Deleted";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        public class CreateBody
        {
            [JsonPropertyName("resume_id")] public Guid ResumeId { get; set; }
            [JsonPropertyName("vacancy_id")] public Guid VacancyId { get; set; }
        }

        public class GetAllQuery
        {
            [FromQuery(Name = "offset")] public int Offset { get; set; } = 0;
            [FromQuery(Name = "count")] public int Count { get; set; } = 100;
        }
    }
}