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
    public class FavoritesController : ControllerBase
    {
        private readonly SisbiContext _context;

        public FavoritesController(SisbiContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Worker,Employer"), HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.Id();
            object result;
            if (User.IsInRole("Worker"))
            {
                result = _context
                    .FavoriteVacancies
                    .Where(fv => fv.UserId == userId)
                    .Select(fv => new
                    {
                        id = fv.Id,
                        vacancy = new
                        {
                            id = fv.VacancyId,
                            position = fv.Vacancy.Position,
                            salary = fv.Vacancy.Salary,
                            city = new
                            {
                                id = fv.Vacancy.City.Id,
                                name = fv.Vacancy.City.Name
                            },
                            schedule = fv.Vacancy.Schedule,
                            description = fv.Vacancy.Description,
                            user_id = fv.Vacancy.UserId
                        }
                    });
            }
            else
            {
                result = _context
                    .FavoriteResumes
                    .Where(fr => fr.UserId == userId)
                    .Select(fr => new
                    {
                        id = fr.Id,
                        resume = new
                        {
                            id = fr.ResumeId,
                            position = fr.Resume.Position,
                            salary = fr.Resume.Salary,
                            city = new
                            {
                                id = fr.Resume.City.Id,
                                name = fr.Resume.City.Name
                            },
                            schedule = fr.Resume.Schedule,
                            description = fr.Resume.Description,
                            places_of_work = fr
                                .Resume
                                .PlacesOfWork
                                .Select(pow => new
                                {
                                    id = pow.Id,
                                    position = pow.Position,
                                    company = pow.Company,
                                    description = pow.Description,
                                    start_date = pow.StartDate,
                                    end_date = pow.EndDate,
                                    resume_id = pow.ResumeId
                                }),
                            user_id = fr.Resume.UserId
                        }
                    });
            }

            return Ok(new
            {
                success = true,
                favorites = result
            });
        }

        [AllowAnonymous, HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            var userId = User.Id();
            object result = null;

            if (User.IsInRole("Worker"))
            {
                var fv = await _context.FavoriteVacancies.FindAsync(id);
                
                if (fv.UserId == userId)
                {
                    result = new
                    {
                        id = fv.Id,
                        vacancy = new
                        {
                            id = fv.VacancyId,
                            position = fv.Vacancy.Position,
                            salary = fv.Vacancy.Salary,
                            city = new
                            {
                                id = fv.Vacancy.City.Id,
                                name = fv.Vacancy.City.Name
                            },
                            schedule = fv.Vacancy.Schedule,
                            description = fv.Vacancy.Description,
                            user_id = fv.Vacancy.UserId
                        }
                    };
                }
            }
            else
            {
                var fr = await _context.FavoriteResumes.FindAsync(id);
                
                if (fr.UserId == userId)
                {
                    result = new
                    {
                        id = fr.Id,
                        vacancy = new
                        {
                            id = fr.ResumeId,
                            position = fr.Resume.Position,
                            salary = fr.Resume.Salary,
                            city = new
                            {
                                id = fr.Resume.City.Id,
                                name = fr.Resume.City.Name
                            },
                            schedule = fr.Resume.Schedule,
                            description = fr.Resume.Description,
                            user_id = fr.Resume.UserId
                        }
                    };
                }
            }


            if (result == null)
            {
                return BadRequest("Избранное с таким id не найдено");
            }

            return Ok(new
            {
                success = true,
                favorite = result
            });
        }

        // [Authorize(Roles = "Worker,Employer"), HttpPost]
        // public async Task<IActionResult> Create([FromBody] FavoriteRequest body)
        // {
        //     var userId = User.Id();
        //     var user = await _context.Users.FindAsync(userId);
        //
        //     if (string.IsNullOrEmpty(user.FirstName) ||
        //         string.IsNullOrEmpty(user.SecondName) ||
        //         string.IsNullOrEmpty(user.BDate) ||
        //         user.Gender == Gender.Unknown)
        //     {
        //         return BadRequest("Профиль не заполнен");
        //     }
        //
        //     var vacancy = new Vacancy
        //     {
        //         Position = body.Position,
        //         Salary = body.Salary,
        //         CityId = body.CityId,
        //         Schedule = body.Schedule,
        //         Description = body.Description,
        //         Phone = body.Phone,
        //         Email = body.Email,
        //         UserId = userId
        //     };
        //
        //     await _context.Vacancies.AddAsync(vacancy);
        //     await _context.SaveChangesAsync();
        //
        //     var city = await _context.Cities.FindAsync(vacancy.CityId);
        //
        //     return Ok(new
        //     {
        //         success = true,
        //         id = vacancy.Id,
        //         position = vacancy.Position,
        //         salary = vacancy.Salary,
        //         city = new
        //         {
        //             id = city.Id,
        //             name = city.Name
        //         },
        //         schedule = vacancy.Schedule,
        //         description = vacancy.Description,
        //         phone = vacancy.Phone,
        //         email = vacancy.Email,
        //         user_id = vacancy.UserId
        //     });
        // }
        //
        // [Authorize(Roles = "Worker"), HttpPut("{id}")]
        // public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] FavoriteRequest body)
        // {
        //     //TODO: добавить проверку полей в body
        //
        //     var vacancy = await _context.Vacancies.FindAsync(id);
        //
        //     if (vacancy == null)
        //     {
        //         return BadRequest("Такой вакансии не найдено!");
        //     }
        //
        //     if (vacancy.UserId != User.Id())
        //     {
        //         return BadRequest("У вас нет прав на изменение этой вакансии!");
        //     }
        //
        //     vacancy.Position = body.Position;
        //     vacancy.Salary = body.Salary;
        //     vacancy.CityId = body.CityId;
        //     vacancy.Schedule = body.Schedule;
        //     vacancy.Description = body.Description;
        //     vacancy.Phone = body.Phone;
        //     vacancy.Email = body.Email;
        //
        //     await _context.SaveChangesAsync();
        //
        //     return Ok(new
        //     {
        //         success = true,
        //         id = vacancy.Id,
        //         position = vacancy.Position,
        //         salary = vacancy.Salary,
        //         city = new
        //         {
        //             id = vacancy.City.Id,
        //             name = vacancy.City.Name
        //         },
        //         schedule = vacancy.Schedule,
        //         description = vacancy.Description,
        //         phone = vacancy.Phone,
        //         email = vacancy.Email,
        //         user_id = vacancy.UserId
        //     });
        // }

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

        public class FavoriteRequest
        {
            [JsonPropertyName("resume_id")] public string Resume { get; set; }
            [JsonPropertyName("vacancy_id")] public string Vacancy { get; set; }
        }
    }
}