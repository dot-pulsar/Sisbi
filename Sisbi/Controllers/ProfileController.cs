using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;
using Models.Enums;
using Newtonsoft.Json;
using Sisbi.Extensions;

namespace Sisbi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly SisbiContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ProfileController(IWebHostEnvironment hostingEnvironment, SisbiContext context)
        {
            _hostingEnvironment = hostingEnvironment;
            _context = context;
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = User.Id();
            var user = await _context.Users.FindAsync(userId);

            return user.Role switch
            {
                Role.Worker => Ok(new
                {
                    success = true,
                    id = user.Id,
                    role = user.Role,
                    first_name = user.FirstName,
                    second_name = user.SecondName,
                    avatar = user.Avatar,
                    gender = user.Gender,
                    bdate = user.BDate,
                    address = user.Address,
                    email = user.Email,
                    email_confirmed = user.EmailConfirmed,
                    phone = user.Phone,
                    phone_confirmed = user.PhoneConfirmed,
                    registration_date = user.RegistrationDate
                }),
                Role.Employer => Ok(new
                {
                    success = true,
                    id = user.Id,
                    role = user.Role,
                    first_name = user.FirstName,
                    second_name = user.SecondName,
                    company = user.Company,
                    avatar = user.Avatar,
                    gender = user.Gender,
                    bdate = user.BDate,
                    address = user.Address,
                    email = user.Email,
                    email_confirmed = user.EmailConfirmed,
                    phone = user.Phone,
                    phone_confirmed = user.PhoneConfirmed,
                    registration_date = user.RegistrationDate
                }),
                _ => Ok(new
                {
                    success = true,
                    id = user.Id,
                    role = user.Role,
                    first_name = user.FirstName,
                    second_name = user.SecondName,
                    avatar = user.Avatar,
                    gender = user.Gender,
                    bdate = user.BDate,
                    address = user.Address,
                    email = user.Email,
                    email_confirmed = user.EmailConfirmed,
                    phone = user.Phone,
                    phone_confirmed = user.PhoneConfirmed,
                    registration_date = user.RegistrationDate
                })
            };
        }

        [Authorize, HttpPut]
        public async Task<IActionResult> Edit([FromBody] EditBody body)
        {
            var format = "dd-MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;

            if (!string.IsNullOrEmpty(body.BDate) &&
                !DateTime.TryParseExact(body.BDate, format, provider, style, out var temp))
            {
                return BadRequest("Неверная дата");
            }

            var userId = User.Id();
            var user = await _context.Users.FindAsync(userId);

            user.FirstName = body.FirstName;
            user.SecondName = body.SecondName;
            user.Company = body.Company;
            user.Gender = body.Gender;
            user.BDate = body.BDate;
            user.Address = body.Address;

            await _context.SaveChangesAsync();

            return user.Role switch
            {
                Role.Worker => Ok(new
                {
                    success = true,
                    id = user.Id,
                    role = user.Role,
                    first_name = user.FirstName,
                    second_name = user.SecondName,
                    avatar = user.Avatar,
                    gender = user.Gender,
                    bdate = user.BDate,
                    address = user.Address,
                    email = user.Email,
                    email_confirmed = user.EmailConfirmed,
                    phone = user.Phone,
                    phone_confirmed = user.PhoneConfirmed,
                    registration_date = user.RegistrationDate
                }),
                Role.Employer => Ok(new
                {
                    success = true,
                    id = user.Id,
                    role = user.Role,
                    first_name = user.FirstName,
                    second_name = user.SecondName,
                    avatar = user.Avatar,
                    company = user.Company,
                    gender = user.Gender,
                    bdate = user.BDate,
                    address = user.Address,
                    email = user.Email,
                    email_confirmed = user.EmailConfirmed,
                    phone = user.Phone,
                    phone_confirmed = user.PhoneConfirmed,
                    registration_date = user.RegistrationDate
                }),
                _ => Ok(new
                {
                    success = true,
                    id = user.Id,
                    role = user.Role,
                    first_name = user.FirstName,
                    second_name = user.SecondName,
                    avatar = user.Avatar,
                    gender = user.Gender,
                    bdate = user.BDate,
                    address = user.Address,
                    email = user.Email,
                    email_confirmed = user.EmailConfirmed,
                    phone = user.Phone,
                    phone_confirmed = user.PhoneConfirmed,
                    registration_date = user.RegistrationDate
                })
            };
        }

        [Authorize(Roles = "Worker"), HttpGet("resumes")]
        public async Task<IActionResult> GetResumes()
        {
            var userId = User.Id();
            var resumes = await _context.Resumes.Where(r => r.UserId == userId).ToListAsync();
            return Ok(new
            {
                success = true,
                data = resumes
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
                        status = r.Status,
                        videos = r.Videos.Select(v => new
                        {
                            name = v.Name,
                            format = v.Format,
                            urn = v.Urn
                        }),
                        work_experience = r.WorkExperience,
                        places_of_work = r.PlacesOfWork
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
                        date_of_creation = r.DateOfCreation,
                        date_of_change = r.DateOfChange,
                        user_id = r.UserId
                    })
            });
        }

        [Authorize(Roles = "Employer"), HttpGet("vacancies")]
        public async Task<IActionResult> GetVacancies()
        {
            var userId = User.Id();
            var vacancies = await _context.Vacancies.Where(r => r.UserId == userId).ToListAsync();
            return Ok(new
            {
                success = true,
                data = vacancies.Select(r => new
                {
                    id = r.Id,
                    position = r.Position,
                    salary = r.Salary,
                    work_experience = r.WorkExperience,
                    city = new
                    {
                        id = r.City.Id,
                        name = r.City.Name
                    },
                    schedule = r.Schedule,
                    description = r.Description,
                    user_id = r.UserId
                })
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpGet("responses")]
        public async Task<IActionResult> GetResponses([FromQuery] GetAllQueryById query)
        {
            var userId = User.Id();
            IEnumerable<Response> data;

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

            if (query.VacancyId != Guid.Empty || query.ResumeId != Guid.Empty)
            {
                if (User.IsInRole("Worker"))
                {
                    data = await _context.Responses
                        .Where(r => r.ResumeId == query.ResumeId && r.Resume.UserId == userId).ToListAsync();
                }
                else
                {
                    data = await _context.Responses
                        .Where(r => r.VacancyId == query.VacancyId && r.Vacancy.UserId == userId).ToListAsync();
                }

                var total = data.Count();

                data = data.Skip(query.Page * query.Limit - query.Limit).Take(query.Limit);

                var perPage = data.Count();
                var currentPage = query.Page;
                var lastPage = Math.Ceiling((double) total / query.Limit);

                return Ok(new
                {
                    success = true,
                    total = total,
                    per_page = perPage,
                    current_page = currentPage,
                    last_page = lastPage,
                    data = data
                        .Select(r => new
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
                            status = r.Resume.Status,
                            videos = r.Resume.Videos.Select(v => new
                            {
                                name = v.Name,
                                format = v.Format,
                                urn = v.Urn
                            }),
                            work_experience = r.Resume.WorkExperience,
                            places_of_work = r.Resume.PlacesOfWork
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
                            date_of_creation = r.Resume.DateOfCreation,
                            date_of_change = r.Resume.DateOfChange,
                            user_id = r.Resume.UserId
                        })
                });
            }
            else
            {
                if (User.IsInRole("Worker"))
                {
                    data = await _context.Responses.Where(r => r.Resume.UserId == userId).ToListAsync();
                }
                else
                {
                    data = await _context.Responses.Where(r => r.Vacancy.UserId == userId).ToListAsync();
                }

                var total = data.Count();

                data = data.Skip(query.Page * query.Limit - query.Limit).Take(query.Limit);

                var perPage = data.Count();
                var currentPage = query.Page;
                var lastPage = Math.Ceiling((double) total / query.Limit);

                return Ok(new
                {
                    success = true,
                    total = total,
                    per_page = perPage,
                    current_page = currentPage,
                    last_page = lastPage,
                    data = data
                        .Select(r => new
                        {
                            id = r.Id,
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
                                status = r.Resume.Status,
                                videos = r.Resume.Videos.Select(v => new
                                {
                                    name = v.Name,
                                    format = v.Format,
                                    urn = v.Urn
                                }),
                                work_experience = r.Resume.WorkExperience,
                                places_of_work = r.Resume.PlacesOfWork
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
                                date_of_creation = r.Resume.DateOfCreation,
                                date_of_change = r.Resume.DateOfChange,
                                user_id = r.Resume.UserId
                            },
                            vacancy = new
                            {
                                id = r.Vacancy.Id,
                                position = r.Vacancy.Position,
                                salary = r.Vacancy.Salary,
                                work_experience = r.Vacancy.WorkExperience,
                                city = new
                                {
                                    id = r.Vacancy.City.Id,
                                    name = r.Vacancy.City.Name
                                },
                                schedule = r.Vacancy.Schedule,
                                description = r.Vacancy.Description,
                                date_of_creation = r.Vacancy.DateOfCreation,
                                date_of_change = r.Vacancy.DateOfChange,
                                user_id = r.Vacancy.UserId
                            },
                            sender = r.Sender,
                            status = r.Status
                        })
                });
            }
        }

        [Authorize(Roles = "Worker,Employer"), HttpGet("responses/{responseId}")]
        public async Task<IActionResult> GetResponse([FromRoute] Guid responseId)
        {
            var userId = User.Id();
            var response = await _context.Responses.FindAsync(responseId);

            if (response == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Response not found!"
                });
            }

            if (User.IsInRole("Worker"))
            {
                if (response.Resume.UserId != userId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = "У вас нет прав на просмотр этого отклика"
                    });
                }
            }
            else
            {
                if (response.Vacancy.UserId != userId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = "У вас нет прав на просмотр этого отклика"
                    });
                }
            }

            return Ok(new
            {
                success = true,
                id = response.Id,
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
                        urn = v.Urn
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
                    id = response.Vacancy.Id,
                    position = response.Vacancy.Position,
                    salary = response.Vacancy.Salary,
                    work_experience = response.Vacancy.WorkExperience,
                    city = new
                    {
                        id = response.Vacancy.City.Id,
                        name = response.Vacancy.City.Name
                    },
                    schedule = response.Vacancy.Schedule,
                    description = response.Vacancy.Description,
                    date_of_creation = response.Vacancy.DateOfCreation,
                    date_of_change = response.Vacancy.DateOfChange,
                    user_id = response.Vacancy.UserId
                },
                sender = response.Sender,
                status = response.Status
            });
        }

        [Authorize, HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] FormImage data)
        {
            var userId = User.Id();

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Profile not found."
                });
            }

            const string dir = "images";
            var images = Path.Combine(_hostingEnvironment.WebRootPath, dir);

            var newGuid = Guid.NewGuid();
            var imageName = $"{newGuid}.{data.Format}";
            var imagePath = Path.Combine(images, imageName);

            if (!string.IsNullOrEmpty(user.Avatar))
            {
                var oldAvatarName = user.Avatar.Split("/").Last();
                Console.WriteLine(oldAvatarName);
                System.IO.File.Delete(Path.Combine(images, oldAvatarName));
            }

            await using Stream fileStream = new FileStream(imagePath, FileMode.Create);
            await data.Image.CopyToAsync(fileStream);

            user.Avatar = $"{dir}/{imageName}";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                avatar = user.Avatar
            });
        }

        public class EditBody
        {
            [JsonPropertyName("first_name")] public string FirstName { get; set; }
            [JsonPropertyName("second_name")] public string SecondName { get; set; }
            [JsonPropertyName("company")] public string Company { get; set; }
            [JsonPropertyName("gender")] public Gender Gender { get; set; }
            [JsonPropertyName("bdate")] public string BDate { get; set; }
            [JsonPropertyName("address")] public string Address { get; set; }
        }

        public class Profile
        {
            [JsonPropertyName("id")] public Guid Id { get; set; }
            [JsonPropertyName("role")] public Role Role { get; set; }
            [JsonPropertyName("first_name")] public string FirstName { get; set; }
            [JsonPropertyName("second_name")] public string SecondName { get; set; }
            [JsonPropertyName("gender")] public Gender Gender { get; set; }
            [JsonPropertyName("bdate")] public string BDate { get; set; }
            [JsonPropertyName("address")] public string Address { get; set; }
            [JsonPropertyName("phone")] public string Phone { get; set; }
            [JsonPropertyName("phone_confirmed")] public bool PhoneConfirmed { get; set; }
            [JsonPropertyName("email")] public string Email { get; set; }
            [JsonPropertyName("email_confirmed")] public bool EmailConfirmed { get; set; }

            [JsonPropertyName("registration_date")]
            public long RegistrationDate { get; set; }
        }

        public class GetAllQueryById
        {
            [FromQuery(Name = "vacancy_id")] public Guid VacancyId { get; set; }
            [FromQuery(Name = "resume_id")] public Guid ResumeId { get; set; }
            [FromQuery(Name = "page")] public int Page { get; set; } = 1;
            [FromQuery(Name = "limit")] public int Limit { get; set; } = 20;
        }
    }
}