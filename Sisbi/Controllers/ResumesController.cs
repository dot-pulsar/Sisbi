using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;
using Models.Enums;
using Models.Requests;
using Sisbi.Extensions;
using Twilio.Rest.Chat.V1.Service;
using Twilio.Rest.Trusthub.V1;

namespace Sisbi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ResumesController : ControllerBase
    {
        private readonly SisbiContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ResumesController(IWebHostEnvironment hostEnvironment, SisbiContext context)
        {
            _context = context;
            _hostingEnvironment = hostEnvironment;
        }

        private WorkExperiences getWorkExperiences(ICollection<PlaceOfWork> placesOfWork)
        {
            return WorkExperiences.Default;
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAllResumes([FromQuery] GetAllQuery query)
        {
            var userId = Guid.Empty;
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Worker"))
            {
                userId = User.Id();
            }

            IQueryable<Resume> resumes = _context.Resumes;

            if (!query.All)
            {
                if (userId == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        success = false,
                        description = "Вы не можете получить все резюме, пока вы не авторизованы, как worker."
                    });
                }

                resumes = resumes.Where(r => r.UserId != userId);
            }

            if (!string.IsNullOrEmpty(query.Cities))
            {
                var cities = new List<Guid>();

                var guidCities = query.Cities.Split(',');
                foreach (var uuidCity in guidCities)
                {
                    if (Guid.TryParse(uuidCity, out var guid))
                    {
                        cities.Add(guid);
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            description = $"UUID ({uuidCity}) не валиден!"
                        });
                    }
                }

                resumes = resumes.Where(r => cities.Contains(r.CityId));
            }


            /*var tempStartTime = ParseDateTime(placeOfWork.StartDate);

            if (tempStartTime < startTime)
            {
                startTime = tempStartTime;
            }

            var tempEndTime = ParseDateTime(placeOfWork.EndDate);
            if (tempEndTime != null)
            {
                    
            }*/
            
            if (query.WorkExperience != WorkExperiences.Default)
            {
                if (query.WorkExperience == WorkExperiences.NotExperience)
                {
                }
                else if (query.WorkExperience == WorkExperiences.FromOneToThreeYears)
                {
                    var resume = await resumes.Where(r =>
                        getWorkExperiences(r.PlaceOfWorks) == WorkExperiences.FromOneToThreeYears).ToListAsync();
                }
                else if (query.WorkExperience == WorkExperiences.FromThreeToSixYears)
                {
                    var resume = resumes.Where(r =>
                        getWorkExperiences(r.PlaceOfWorks.ToList()) == WorkExperiences.FromThreeToSixYears).ToList();
                }
                else if (query.WorkExperience == WorkExperiences.OverSixYears)
                {
                    var resume = resumes.Where(r => getWorkExperiences(r.PlaceOfWorks.ToList()) == WorkExperiences.OverSixYears).ToList();
                }
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

                resumes = resumes.Where(r => r.Salary >= query.MinSalary);
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

                resumes = resumes.Where(r => r.Salary <= query.MaxSalary);
            }

            /*var result = await _context.Resumes
                .Where(r => query.UserId == Guid.Empty ? r.UserId != Guid.Empty : r.UserId == query.UserId)
                .Where(r => query.All ? r.UserId != Guid.Empty : r.UserId != userId)
                .Where(r => query.MinSalary == 0 ? r.Salary >= 0 : r.Salary >= query.MinSalary)
                .Where(r => query.MaxSalary == 0 ? r.Salary >= 0 : r.Salary <= query.MaxSalary)
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
                    status = r.Status,
                    video = r.Video,
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
                }).ToListAsync();*/

            return Ok(new
            {
                success = true,
                data = await resumes.ToListAsync()
            });
        }

        [AllowAnonymous, HttpGet("{id}")]
        public async Task<IActionResult> GetResume([FromRoute] Guid id)
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
                status = resume.Status,
                video = resume.Video,
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
        public async Task<IActionResult> CreateResume([FromBody] CreateBody body)
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
                UserId = userId,
                Status = AdStatus.Created.ToString()
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
                status = resume.Status,
                video = resume.Video,
                user_id = resume.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpPut("{id}")]
        public async Task<IActionResult> EditResume([FromRoute] Guid id, [FromBody] EditBody body)
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
                status = resume.Status,
                video = resume.Video,
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
        public async Task<IActionResult> DeleteResume([FromRoute] Guid id)
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

        [Authorize(Roles = "Worker"), HttpGet("{resumeId}/places_of_work")]
        public async Task<IActionResult> GetAll([FromRoute] Guid resumeId)
        {
            var pows = await _context.PlacesOfWork
                .Where(p => p.ResumeId == resumeId)
                .ToListAsync();

            var powResult = pows.Select(place => new
                {
                    id = place.Id,
                    position = place.Position,
                    company = place.Company,
                    description = place.Description,
                    start_date = place.StartDate,
                    end_date = place.EndDate,
                    resume_id = place.ResumeId
                })
                .ToList();

            return Ok(new
            {
                success = true,
                places_of_work = powResult
            });
        }

        [AllowAnonymous, HttpGet("{resumeId}/places_of_work/{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid resumeId, Guid id)
        {
            var pow = await _context.PlacesOfWork.SingleOrDefaultAsync(p => p.Id == id && p.ResumeId == resumeId);

            if (pow != null)
            {
                return Ok(new
                {
                    success = true,
                    id = pow.Id,
                    position = pow.Position,
                    company = pow.Company,
                    description = pow.Description,
                    start_date = pow.StartDate,
                    end_date = pow.EndDate,
                    resume_id = pow.ResumeId
                });
            }

            return BadRequest(new
            {
                success = false,
                description = "Place not fond"
            });
        }

        [Authorize(Roles = "Worker"), HttpPost("{resumeId}/places_of_work")]
        public async Task<IActionResult> Create([FromRoute] Guid resumeId, [FromBody] PlaceOfWorkRequest body)
        {
            #region Validate

            if (string.IsNullOrEmpty(body.Position))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "position field is required."
                });
            }

            if (string.IsNullOrEmpty(body.Company))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "company field is required."
                });
            }

            if (string.IsNullOrEmpty(body.Description))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "description field is required."
                });
            }

            if (string.IsNullOrEmpty(body.StartDate))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "start_date field is required."
                });
            }

            if (string.IsNullOrEmpty(body.EndDate))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "end_date field is required."
                });
            }

            if (resumeId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume_id field is required."
                });
            }

            if (!IsDateValid(body.StartDate))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "start_date not valid."
                });
            }

            if (!IsDateValid(body.EndDate) && body.EndDate != "now")
            {
                return BadRequest(new
                {
                    success = false,
                    description = "end_date not valid."
                });
            }

            if (ParseDateTime(body.StartDate) >= ParseDateTime(body.EndDate) && body.EndDate != "now")
            {
                return BadRequest(new
                {
                    success = false,
                    description = "start_date must not be less than or equal to end_date."
                });
            }

            if (ParseDateTime(body.EndDate) > DateTime.Now)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "end_date should not be more than today's month."
                });
            }

            var resume = await _context.Resumes.FindAsync(resumeId);
            if (resume == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume_id not found."
                });
            }

            if (resume.UserId != User.Id())
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this resume_id"
                });
            }

            #endregion

            var pow = new PlaceOfWork
            {
                Position = body.Position,
                Company = body.Company,
                Description = body.Description,
                StartDate = body.StartDate,
                EndDate = body.EndDate,
                ResumeId = resumeId
            };

            await _context.PlacesOfWork.AddAsync(pow);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = pow.Id,
                position = pow.Position,
                company = pow.Company,
                description = pow.Description,
                start_date = pow.StartDate,
                end_date = pow.EndDate,
                resume_id = pow.ResumeId
            });
        }

        [Authorize(Roles = "Worker"), HttpPut("{resumeId}/places_of_work/{id}")]
        public async Task<IActionResult> Edit([FromRoute] Guid resumeId, [FromRoute] Guid id,
            [FromBody] PlaceOfWorkRequest body)
        {
            #region Validate

            if (string.IsNullOrEmpty(body.Position))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "position field is required."
                });
            }

            if (string.IsNullOrEmpty(body.Company))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "company field is required."
                });
            }

            if (string.IsNullOrEmpty(body.Description))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "description field is required."
                });
            }

            if (string.IsNullOrEmpty(body.StartDate))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "start_date field is required."
                });
            }

            if (string.IsNullOrEmpty(body.EndDate))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "end_date field is required."
                });
            }

            if (resumeId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume_id field is required."
                });
            }

            if (!IsDateValid(body.StartDate))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "start_date not valid."
                });
            }

            if (!IsDateValid(body.EndDate) && body.EndDate != "now")
            {
                return BadRequest(new
                {
                    success = false,
                    description = "end_date not valid."
                });
            }

            if (ParseDateTime(body.StartDate) >= ParseDateTime(body.EndDate))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "start_date must not be less than or equal to end_date."
                });
            }

            if (ParseDateTime(body.EndDate) > DateTime.Now)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "end_date should not be more than today's month."
                });
            }

            var pow = await _context.PlacesOfWork.FindAsync(id);

            if (pow == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "place_of_work not found."
                });
            }

            var resume = await _context.Resumes.FindAsync(resumeId);
            if (resume == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume_id not found."
                });
            }

            if (resume.UserId != User.Id())
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this resume_id"
                });
            }

            #endregion

            pow.Position = body.Position;
            pow.Company = body.Company;
            pow.Description = body.Description;
            pow.StartDate = body.StartDate;
            pow.EndDate = body.EndDate;
            pow.ResumeId = resumeId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = pow.Id,
                position = pow.Position,
                company = pow.Company,
                description = pow.Description,
                start_date = pow.StartDate,
                end_date = pow.EndDate,
                resume_id = pow.ResumeId
            });
        }

        [Authorize(Roles = "Worker"), HttpDelete("{resumeId}/places_of_work/{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid resumeId, [FromRoute] Guid id)
        {
            var userId = User.Id();

            var pow = await _context.PlacesOfWork.SingleOrDefaultAsync(p => p.Id == id && p.ResumeId == resumeId);

            if (pow == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Place not found."
                });
            }

            if (pow.Resume.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this place_of_work_id"
                });
            }

            _context.PlacesOfWork.Remove(pow);

            return Ok(new
            {
                success = true
            });
        }


        public class Data
        {
            [FromForm(Name = "video")] public IFormFile Video { get; set; }
            [FromForm(Name = "format")] public string Format { get; set; }
        }

        [Authorize(Roles = "Worker"), HttpPost("{resumeId}/video")]
        public async Task<IActionResult> UploadVideo([FromRoute] Guid resumeId, [FromForm] Data data)
        {
            var userId = User.Id();

            var resume = await _context.Resumes.SingleOrDefaultAsync(p => p.Id == resumeId);

            if (resume == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Resume not found."
                });
            }

            /*if (resume.UserId != userId)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "you are not authorized to use this resume_id"
                });
            }*/

            const string videosDir = "videos";
            var videos = Path.Combine(_hostingEnvironment.WebRootPath, videosDir);

            var newGuid = Guid.NewGuid();
            var videoName = $"{newGuid}.{data.Format}";
            var videoPath = Path.Combine(videos, videoName);

            if (!string.IsNullOrEmpty(resume.Video))
            {
                var oldVideoName = resume.Video.Split("/").Last();
                Console.WriteLine(oldVideoName);
                System.IO.File.Delete(Path.Combine(videos, oldVideoName));
            }

            await using Stream fileStream = new FileStream(videoPath, FileMode.Create);
            await data.Video.CopyToAsync(fileStream);

            resume.Video = $"{Request.Scheme}://{Request.Host}/{videosDir}/{videoName}";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        [NonAction]
        private static bool IsDateValid(string date)
        {
            var format = "MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;

            return DateTime.TryParseExact(date, format, provider, style, out var temp);
        }

        [NonAction]
        private static DateTime ParseDateTime(string date)
        {
            var format = "MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;
            DateTime.TryParseExact(date, format, provider, style, out var result);

            return result;
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
            [FromQuery(Name = "count")] public int Count { get; set; } = 20;
            [FromQuery(Name = "all")] public bool All { get; set; } = true;
            [FromQuery(Name = "cities")] public string Cities { get; set; }
            [FromQuery(Name = "work_experience")] public WorkExperiences WorkExperience { get; set; }
            [FromQuery(Name = "min_salary")] public long MinSalary { get; set; }
            [FromQuery(Name = "max_salary")] public long MaxSalary { get; set; }
            [FromQuery(Name = "schedule")] public string Schedule { get; set; }
            [FromQuery(Name = "employment_type")] public string EmploymentType { get; set; }
            [FromQuery(Name = "sort_by")] public string SortBy { get; set; }
        }
    }
}

//profile/resumes (фильтры на status) + vacancions