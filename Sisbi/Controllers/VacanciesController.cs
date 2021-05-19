using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;
using Models.Enums;
using NinjaNye.SearchExtensions;
using Shell.NET;
using Sisbi.Extensions;

namespace Sisbi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class VacanciesController : ControllerBase
    {
        private readonly SisbiContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public VacanciesController(IWebHostEnvironment hostingEnvironment, SisbiContext context)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        #region Vacancies

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllQuery query)
        {
            var employerId = Guid.Empty;
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Employer"))
            {
                employerId = User.Id();
            }

            var workerId = Guid.Empty;
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Worker"))
            {
                workerId = User.Id();
            }

            var vacancies = _context.Vacancies.Where(v => v.Videos.Any());

            if (!string.IsNullOrEmpty(query.Position))
            {
                var words = query.Position.ToLower().Split(" ");
                Console.WriteLine(string.Join(" ", words));

                vacancies = vacancies.Search(x => x.Position.ToLower()).Containing(words);
            }

            if (!query.All)
            {
                if (employerId == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = "Вы не можете получить все вакансии, пока вы не авторизованы, как employer."
                    });
                }

                vacancies = vacancies.Where(r => r.UserId != employerId);
            }

            if (query.Cities != null && query.Cities.Any())
            {
                vacancies = vacancies.Where(r => query.Cities.Contains(r.CityId));
            }

            if (query.MinWorkExperience != 0)
            {
                if (query.MinWorkExperience < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = $"min_work_exp не может быть меньше 0!"
                    });
                }

                vacancies = vacancies.Where(r => r.WorkExperience >= query.MinWorkExperience);
            }

            if (query.MaxWorkExperience != 0)
            {
                if (query.MaxSalary < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = $"max_work_exp не может быть меньше 0!"
                    });
                }

                vacancies = vacancies.Where(r => r.WorkExperience <= query.MaxWorkExperience);
            }

            if (query.MinSalary != 0)
            {
                if (query.MinSalary < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = $"min_salary не может быть меньше 0!"
                    });
                }

                vacancies = vacancies.Where(r => r.Salary >= query.MinSalary);
            }

            if (query.MaxSalary != 0)
            {
                if (query.MaxSalary < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = $"max_salary не может быть меньше 0!"
                    });
                }

                vacancies = vacancies.Where(r => r.Salary <= query.MaxSalary);
            }

            switch (query.Limit)
            {
                case > 100:
                    return BadRequest(new
                    {
                        success = false,
                        description = $"Limit не может быть больше 100!"
                    });
                case < 0:
                    return BadRequest(new
                    {
                        success = false,
                        description = $"Limit не может быть меньше 0!"
                    });
                case 0:
                    return BadRequest(new
                    {
                        success = false,
                        description = "Limit не может быть равен 0!"
                    });
            }

            switch (query.Page)
            {
                case < 0:
                    return BadRequest(new
                    {
                        success = false,
                        description = $"Page не может быть меньше 0!"
                    });
                case 0:
                    return BadRequest(new
                    {
                        success = false,
                        description = "Page не может быть равен 0!"
                    });
            }

            var total = await vacancies.CountAsync();

            vacancies = vacancies.Skip(query.Page * query.Limit - query.Limit).Take(query.Limit);

            var perPage = await vacancies.CountAsync();
            var currentPage = query.Page;
            var lastPage = Math.Ceiling((double) total / query.Limit);

            switch (query.SortBy)
            {
                case AdSortBy.DateAsc:
                    vacancies = vacancies.OrderBy(r => r.DateOfChange);
                    break;
                case AdSortBy.DateDesc:
                    vacancies = vacancies.OrderByDescending(r => r.DateOfChange);
                    break;
                case AdSortBy.SalaryAsc:
                    vacancies = vacancies.OrderBy(r => r.Salary);
                    break;
                case AdSortBy.SalaryDesc:
                    vacancies = vacancies.OrderByDescending(r => r.Salary);
                    break;
                default:
                    if (!string.IsNullOrEmpty(query.SortBy))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            description = $"Нет такой сортрировки ({query.SortBy})"
                        });
                    }

                    vacancies = vacancies.OrderByDescending(r => r.DateOfChange);
                    break;
            }

            return Ok(new
            {
                success = true,
                total = total,
                per_page = perPage,
                current_page = currentPage,
                last_page = lastPage,
                data = await vacancies
                    .Select(r => new
                    {
                        id = r.Id,
                        company = r.User.Company,
                        position = r.Position,
                        salary = r.Salary,
                        city = new
                        {
                            id = r.City.Id,
                            name = r.City.Name
                        },
                        schedule = r.Schedule,
                        description = r.Description,
                        work_experience = r.WorkExperience,
                        videos = r.Videos.Select(v => new
                        {
                            name = v.Name,
                            format = v.Format,
                            path = v.Urn
                        }),
                        poster = r.Posters.Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            format = p.Format,
                            type = p.Type,
                            selected = p.Selected,
                            number = p.Number,
                            path = p.Urn
                        }).SingleOrDefault(p => p.selected),
                        date_of_creation = r.DateOfCreation,
                        date_of_change = r.DateOfChange,
                        status = r.Status,
                        responses = r.Responses
                            .Where(resp => resp.Resume.UserId == workerId)
                            .Select(resp => new
                            {
                                id = resp.Id,
                                resume = new
                                {
                                    id = resp.Resume.Id,
                                    position = resp.Resume.Position,
                                    salary = resp.Resume.Salary,
                                    city = new
                                    {
                                        id = resp.Resume.City.Id,
                                        name = resp.Resume.City.Name
                                    },
                                    schedule = resp.Resume.Schedule,
                                    description = resp.Resume.Description,
                                    work_experience = resp.Resume.WorkExperience,
                                    places_of_work = resp.Resume.PlacesOfWork
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
                                    videos = resp.Resume.Videos.Select(v => new
                                    {
                                        name = v.Name,
                                        format = v.Format,
                                        path = v.Urn
                                    }),
                                    poster = resp.Resume.Posters.Select(p => new
                                    {
                                        id = p.Id,
                                        name = p.Name,
                                        format = p.Format,
                                        type = p.Type,
                                        selected = p.Selected,
                                        number = p.Number,
                                        path = p.Urn
                                    }).SingleOrDefault(p => p.selected),
                                    date_of_creation = resp.Resume.DateOfCreation,
                                    date_of_change = resp.Resume.DateOfChange,
                                    status = resp.Resume.Status,
                                    user_id = resp.Resume.UserId
                                },
                                sender = resp.Sender,
                                status = resp.Status
                            }),
                        user_id = r.UserId
                    })
                    .ToListAsync()
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
                company = vacancy.User.Company,
                position = vacancy.Position,
                salary = vacancy.Salary,
                city = new
                {
                    id = vacancy.City.Id,
                    name = vacancy.City.Name
                },
                schedule = vacancy.Schedule,
                description = vacancy.Description,
                work_experience = vacancy.WorkExperience,
                videos = vacancy.Videos?.Select(v => new
                {
                    name = v.Name,
                    format = v.Format,
                    path = v.Urn
                }),
                poster = vacancy.Posters?.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    format = p.Format,
                    type = p.Type,
                    selected = p.Selected,
                    number = p.Number,
                    path = p.Urn
                }).SingleOrDefault(p => p.selected),
                date_of_creation = vacancy.DateOfCreation,
                date_of_change = vacancy.DateOfChange,
                status = vacancy.Status,
                user_id = vacancy.UserId
            });
        }

        [Authorize(Roles = "Employer"), HttpPost]
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
                WorkExperience = body.WorkExperience,
                Phone = body.Phone,
                Email = body.Email,
                UserId = userId,
                DateOfCreation = DateTime.UtcNow.ToUnixTime(),
                DateOfChange = DateTime.UtcNow.ToUnixTime(),
                Status = AdStatus.Created.ToString()
            };

            await _context.Vacancies.AddAsync(vacancy);
            await _context.SaveChangesAsync();

            var city = await _context.Cities.FindAsync(vacancy.CityId);

            return Ok(new
            {
                success = true,
                id = vacancy.Id,
                company = vacancy.User.Company,
                position = vacancy.Position,
                salary = vacancy.Salary,
                city = new
                {
                    id = city.Id,
                    name = city.Name
                },
                schedule = vacancy.Schedule,
                description = vacancy.Description,
                work_experience = vacancy.WorkExperience,
                phone = vacancy.Phone,
                email = vacancy.Email,
                videos = vacancy.Videos?.Select(v => new
                {
                    name = v.Name,
                    format = v.Format,
                    path = v.Urn
                }),
                poster = vacancy.Posters?.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    format = p.Format,
                    type = p.Type,
                    selected = p.Selected,
                    number = p.Number,
                    path = p.Urn
                }).SingleOrDefault(p => p.selected),
                date_of_creation = vacancy.DateOfCreation,
                date_of_change = vacancy.DateOfChange,
                status = vacancy.Status,
                user_id = vacancy.UserId
            });
        }

        [Authorize(Roles = "Employer"), HttpPut("{id}")]
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
            vacancy.WorkExperience = body.WorkExperience;
            vacancy.Description = body.Description;
            vacancy.Phone = body.Phone;
            vacancy.Email = body.Email;
            vacancy.DateOfChange = DateTime.UtcNow.ToUnixTime();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = vacancy.Id,
                company = vacancy.User.Company,
                position = vacancy.Position,
                salary = vacancy.Salary,
                city = new
                {
                    id = vacancy.City.Id,
                    name = vacancy.City.Name
                },
                schedule = vacancy.Schedule,
                description = vacancy.Description,
                work_experience = vacancy.WorkExperience,
                videos = vacancy.Videos?.Select(v => new
                {
                    name = v.Name,
                    format = v.Format,
                    path = v.Urn
                }),
                poster = vacancy.Posters?.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    format = p.Format,
                    type = p.Type,
                    selected = p.Selected,
                    number = p.Number,
                    path = p.Urn
                }).SingleOrDefault(p => p.selected),
                date_of_creation = vacancy.DateOfCreation,
                date_of_change = vacancy.DateOfChange,
                status = vacancy.Status,
                user_id = vacancy.UserId
            });
        }

        [Authorize(Roles = "Employer"), HttpDelete("{id}")]
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

        #endregion

        #region Contacts

        [Authorize(Roles = "Worker"), HttpGet("{vacancyId}/contacts")]
        public async Task<IActionResult> GetContacts([FromRoute] Guid vacancyId)
        {
            var vacancy = await _context.Vacancies.FindAsync(vacancyId);

            if (vacancy == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Vacancy not found!"
                });
            }

            return Ok(new
            {
                success = true,
                phone = vacancy.Phone,
                email = vacancy.Email
            });
        }

        #endregion

        #region Videos

        [Authorize(Roles = "Employer"), HttpGet("{vacancyId}/videos")]
        public async Task<IActionResult> GetVideo([FromRoute] Guid vacancyId)
        {
            var userId = User.Id();

            var vacancy = await _context.Vacancies.FindAsync(vacancyId);

            if (vacancy == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Vacancy not found."
                });
            }

            if (vacancy.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this vacancy_id"
                });
            }

            return Ok(new
            {
                success = true,
                data = vacancy.Videos.Select(v => new
                {
                    name = v.Name,
                    format = v.Format,
                    path = v.Urn
                })
            });
        }


        [Authorize(Roles = "Employer"), HttpPost("{vacancyId}/videos")]
        public async Task<IActionResult> UploadVideo([FromRoute] Guid vacancyId, [FromForm] FormVideo data)
        {
            var userId = User.Id();

            var vacancy = await _context.Vacancies.FindAsync(vacancyId);

            if (vacancy == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Vacancy not found."
                });
            }

            if (vacancy.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this vacancy_id"
                });
            }

            if (data.Video == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Video is equal to null!"
                });
            }

            const string videosDir = "videos";
            const string postersDir = "images";
            var videos = Path.Combine(_hostingEnvironment.WebRootPath, videosDir);
            var posters = Path.Combine(_hostingEnvironment.WebRootPath, postersDir);

            var newGuid = Guid.NewGuid();
            var videoName = $"{newGuid}.{data.Format}";
            var videoPath = Path.Combine(videos, videoName);

            await using Stream fileStream = new FileStream(videoPath, FileMode.Create);
            await data.Video.CopyToAsync(fileStream);

            var bash = new Bash();
            var frameCountCommand =
                bash.Command(
                    $"ffprobe -v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of csv=p=0 {videoPath}");

            if (!int.TryParse(frameCountCommand.Output, out var frameCount))
            {
                Console.WriteLine(videoPath);
                Console.WriteLine(frameCountCommand.ErrorMsg);
            }

            var frames = new List<int> {0, frameCount / 2, frameCount - 1};

            var oldPosters = vacancy.Posters.Where(p => p.Type == "system").ToList();

            if (oldPosters.Any())
            {
                oldPosters.ForEach(p => { System.IO.File.Delete(Path.Combine(posters, $"{p.Name}.{p.Format}")); });
                _context.RemoveRange(oldPosters);
            }

            var number = 0;
            var postersPath = new List<string>();
            foreach (var frame in frames)
            {
                var newPosterName = Guid.NewGuid().ToString();
                const string newPosterFormat = "jpg";
                var newPosterFullName = $"{newPosterName}.{newPosterFormat}";
                var newPosterPath = Path.Combine(posters, newPosterFullName);
                postersPath.Add(newPosterPath);

                $"ffmpeg -i {videoPath} -frames:v 1 -vf \"select=eq(n\\,{frame})\" -q:v 0 {newPosterPath}".Bash();

                vacancy.Posters.Add(new VacancyPoster
                {
                    Name = newPosterName,
                    Format = newPosterFormat,
                    Type = "system",
                    Number = ++number,
                    Selected = number == 1 && vacancy.Posters.All(p => p.Type == "system")
                });
            }

            if (vacancy.Videos.Any())
            {
                foreach (var video in vacancy.Videos)
                {
                    System.IO.File.Delete(Path.Combine(videos, $"{video.Name}.{video.Format}"));
                }

                vacancy.Videos.Clear();
            }

            vacancy.Videos.Add(new VacancyVideo
            {
                Name = newGuid.ToString(),
                Format = data.Format,
            });

            await _context.SaveChangesAsync();

            while (!postersPath.All(System.IO.File.Exists))
            {
                await Task.Delay(500);
            }

            return Ok(new
            {
                success = true,
                videos = vacancy.Videos.Select(v => new
                {
                    name = v.Name,
                    format = v.Format,
                    path = v.Urn
                }),
                posters = vacancy.Posters.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    format = p.Format,
                    type = p.Type,
                    selected = p.Selected,
                    number = p.Number,
                    path = p.Urn
                })
            });
        }

        #endregion

        #region Posters

        [Authorize(Roles = "Employer"), HttpGet("{vacancyId}/posters")]
        public async Task<IActionResult> GetPosters([FromRoute] Guid vacancyId)
        {
            var userId = User.Id();

            var vacancy = await _context.Vacancies.FindAsync(vacancyId);

            if (vacancy == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Vacancy not found."
                });
            }

            if (vacancy.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this vacancy_id"
                });
            }

            return Ok(new
            {
                success = true,
                data = vacancy.Posters.Select(v => new
                {
                    id = v.Id,
                    name = v.Name,
                    format = v.Format,
                    type = v.Type,
                    selected = v.Selected,
                    number = v.Number,
                    path = v.Urn
                })
            });
        }

        [Authorize(Roles = "Employer"), HttpPost("{vacancyId}/posters")]
        public async Task<IActionResult> UploadPoster([FromRoute] Guid vacancyId, [FromForm] FormPoster data)
        {
            var userId = User.Id();

            var vacancy = await _context.Vacancies.FindAsync(vacancyId);

            if (vacancy == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Vacancy not found."
                });
            }

            if (vacancy.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this vacancy_id"
                });
            }

            if (data.Poster == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Poster is equal to null!"
                });
            }

            const string dir = "images";
            var images = Path.Combine(_hostingEnvironment.WebRootPath, dir);

            var newGuid = Guid.NewGuid();
            var posterName = $"{newGuid}.{data.Format}";
            var posterPath = Path.Combine(images, posterName);

            await using Stream fileStream = new FileStream(posterPath, FileMode.Create);
            await data.Poster.CopyToAsync(fileStream);

            var oldPoster = vacancy.Posters.SingleOrDefault(p => p.Type == "custom");
            if (oldPoster != null)
            {
                System.IO.File.Delete(Path.Combine(images, $"{oldPoster.Name}.{oldPoster.Format}"));
                vacancy.Posters.Remove(oldPoster);
            }

            foreach (var poster in vacancy.Posters)
            {
                poster.Selected = false;
            }

            vacancy.Posters.Add(new VacancyPoster
            {
                Name = newGuid.ToString(),
                Format = data.Format,
                Selected = true,
                Number = 4,
                Type = "custom"
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = vacancy.Posters.Select(v => new
                {
                    id = v.Id,
                    name = v.Name,
                    format = v.Format,
                    type = v.Type,
                    selected = v.Selected,
                    number = v.Number,
                    path = v.Urn
                })
            });
        }

        [Authorize(Roles = "Employer"), HttpPost("{vacancyId}/posters/{posterId}/select")]
        public async Task<IActionResult> SelectPoster([FromRoute] Guid vacancyId, [FromRoute] Guid posterId)
        {
            var userId = User.Id();

            var vacancy = await _context.Vacancies.SingleOrDefaultAsync(p => p.Id == vacancyId);

            if (vacancy == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Vacancy not found."
                });
            }

            if (vacancy.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this vacancy_id"
                });
            }

            var poster = vacancy.Posters.SingleOrDefault(r => r.Id == posterId);
            if (poster == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Poster not found."
                });
            }

            foreach (var rPoster in vacancy.Posters)
            {
                rPoster.Selected = false;
            }

            poster.Selected = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        #endregion

        public class VacancyRequest
        {
            [JsonPropertyName("position")] public string Position { get; set; }
            [JsonPropertyName("salary")] public long Salary { get; set; }
            [JsonPropertyName("city_id")] public Guid CityId { get; set; }
            [JsonPropertyName("schedule")] public string Schedule { get; set; }
            [JsonPropertyName("work_experience")] public int WorkExperience { get; set; }
            [JsonPropertyName("description")] public string Description { get; set; }
            [JsonPropertyName("email")] public string Email { get; set; }
            [JsonPropertyName("phone")] public string Phone { get; set; }
        }

        public class GetAllQuery
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
}