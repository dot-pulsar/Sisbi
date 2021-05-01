using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;
using Models.Enums;
using Models.Requests;
using Sisbi.Extensions;

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

        #region Resumes

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAllResumes([FromQuery] GetAllQueryResume query)
        {
            var userId = Guid.Empty;
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Worker"))
            {
                userId = User.Id();
            }

            IQueryable<Resume> resumes = _context.Resumes;

            /*if (!string.IsNullOrEmpty(query.Position))
            {
                var words = query.Position.Split(" ");

                var sql = new StringBuilder("SELECT * FROM resume WHERE ");

                foreach (var word in words)
                {
                    if (!string.IsNullOrEmpty(word))
                    {
                        sql.Append($"position ILIKE '%{word}%' ");

                        if (word != words.Last())
                        {
                            sql.Append("OR ");
                        }
                    }
                }

                sql.Append("OR ");

                foreach (var word in words)
                {
                    if (!string.IsNullOrEmpty(word))
                    {
                        sql.Append($"description ILIKE '%{word}%' ");

                        if (word != words.Last())
                        {
                            sql.Append("OR ");
                        }
                    }
                }

                sql.Append("ORDER BY ");

                foreach (var word in words)
                {
                    if (!string.IsNullOrEmpty(word))
                    {
                        sql.Append($"CASE WHEN position ILIKE '%{word}%' THEN 2 ELSE 0 END ");


                        sql.Append("+ ");
                    }
                }

                foreach (var word in words)
                {
                    if (!string.IsNullOrEmpty(word))
                    {
                        sql.Append($"CASE WHEN description ILIKE '%{word}%' THEN 1 ELSE 0 END ");

                        if (word != words.Last())
                        {
                            sql.Append("+ ");
                        }
                    }
                }

                sql.Append("DESC");

                var all = _context.Resumes.FromSqlRaw(sql.ToString());

                Console.WriteLine(sql.ToString());
                Console.WriteLine(all.Count());

                resumes = all;

                //resumes = _context.Resumes.Where(r => words.Any(w=>r.Position.Contains(w)));
            }
            else
            {
                resumes = _context.Resumes;
            }*/

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

            if (query.Cities != null && query.Cities.Any())
            {
                resumes = resumes.Where(r => query.Cities.Contains(r.CityId));
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

                resumes = resumes.Where(r => r.WorkExperience >= query.MinWorkExperience);
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

                resumes = resumes.Where(r => r.WorkExperience <= query.MaxWorkExperience);
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

            var total = await resumes.CountAsync();

            resumes = resumes.Skip(query.Page * query.Limit - query.Limit).Take(query.Limit);

            var perPage = await resumes.CountAsync();
            var currentPage = query.Page; //Math.Ceiling((double) (total - (total - query.Page)) / perPage);
            var lastPage = Math.Ceiling((double) total / query.Limit);

            switch (query.SortBy)
            {
                case AdSortBy.DateAsc:
                    resumes = resumes.OrderBy(r => r.DateOfChange);
                    break;
                case AdSortBy.DateDesc:
                    resumes = resumes.OrderByDescending(r => r.DateOfChange);
                    break;
                case AdSortBy.SalaryAsc:
                    resumes = resumes.OrderBy(r => r.Salary);
                    break;
                case AdSortBy.SalaryDesc:
                    resumes = resumes.OrderByDescending(r => r.Salary);
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

                    resumes = resumes.OrderByDescending(r => r.DateOfChange);
                    break;
            }

            return Ok(new
            {
                success = true,
                total = total,
                per_page = perPage,
                current_page = currentPage,
                last_page = lastPage,
                data = await resumes
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
                    .ToListAsync()
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
                work_experience = resume.WorkExperience,
                places_of_work = resume.PlacesOfWork
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
                    .OrderBy(pow => ParseDateTime(pow.start_date)),
                date_of_creation = resume.DateOfCreation,
                date_of_change = resume.DateOfChange,
                user_id = resume.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpPost]
        public async Task<IActionResult> CreateResume([FromBody] CreateBodyResume body)
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
                DateOfCreation = DateTime.UtcNow.ToUnixTime(),
                DateOfChange = DateTime.UtcNow.ToUnixTime(),
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
                work_experience = resume.WorkExperience,
                places_of_work = resume.PlacesOfWork,
                date_of_creation = resume.DateOfCreation,
                date_of_change = resume.DateOfChange,
                user_id = resume.UserId
            });
        }

        [Authorize(Roles = "Worker"), HttpPut("{id}")]
        public async Task<IActionResult> EditResume([FromRoute] Guid id, [FromBody] EditBodyResume body)
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
            resume.DateOfChange = DateTime.UtcNow.ToUnixTime();

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
                work_experience = resume.WorkExperience,
                places_of_work = resume.PlacesOfWork.Select(pow => new
                {
                    id = pow.Id,
                    position = pow.Position,
                    company = pow.Company,
                    description = pow.Description,
                    start_date = pow.StartDate,
                    end_date = pow.EndDate,
                    resume_id = pow.ResumeId
                }),
                date_of_creation = resume.DateOfCreation,
                date_of_change = resume.DateOfChange,
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

        #endregion

        #region PlacesOfWork

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
            }).OrderBy(pow => ParseDateTime(pow.start_date));

            return Ok(new
            {
                success = true,
                data = powResult
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

            if (ParseDateTime(body.StartDate) > ParseDateTime(body.EndDate) && body.EndDate != "now")
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

            if (resumeId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume_id field is required."
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

            var newPlaceOfWork = new PlaceOfWork
            {
                Position = body.Position,
                Company = body.Company,
                Description = body.Description,
                StartDate = body.StartDate,
                EndDate = body.EndDate,
                ResumeId = resumeId,
            };

            resume.PlacesOfWork.Add(newPlaceOfWork);

            resume.WorkExperience = GetWorkExperience(resume.PlacesOfWork);
            resume.DateOfChange = DateTime.UtcNow.ToUnixTime();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = newPlaceOfWork.Id,
                position = newPlaceOfWork.Position,
                company = newPlaceOfWork.Company,
                description = newPlaceOfWork.Description,
                start_date = newPlaceOfWork.StartDate,
                end_date = newPlaceOfWork.EndDate,
                resume_id = newPlaceOfWork.ResumeId
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

            if (ParseDateTime(body.StartDate) > ParseDateTime(body.EndDate) && body.EndDate != "now")
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

            if (resumeId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume_id field is required."
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

            resume.WorkExperience = GetWorkExperience(resume.PlacesOfWork);
            resume.DateOfChange = DateTime.UtcNow.ToUnixTime();

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
            if (resumeId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "resume_id field is required."
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

            resume.PlacesOfWork.Remove(pow);

            resume.WorkExperience = GetWorkExperience(resume.PlacesOfWork);
            resume.DateOfChange = DateTime.UtcNow.ToUnixTime();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        #endregion

        #region Video

        [Authorize(Roles = "Worker"), HttpPost("{resumeId}/video")]
        public async Task<IActionResult> UploadVideo([FromRoute] Guid resumeId, [FromForm] FormVideo data)
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

        #endregion

        [NonAction]
        private static bool IsDateValid(string date)
        {
            const string format = "MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;

            return DateTime.TryParseExact(date, format, provider, style, out var temp);
        }

        [NonAction]
        private static DateTime ParseDateTime(string date)
        {
            const string format = "MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;
            if (date == "now" || date == "Now")
            {
                return DateTime.Today;
            }

            DateTime.TryParseExact(date, format, provider, style, out var result);

            return result;
        }

        [NonAction]
        private static int GetWorkExperience(IEnumerable<PlaceOfWork> placesOfWork)
        {
            var workExperience = TimeSpan.Zero;
            foreach (var pow in placesOfWork)
            {
                var ts = ParseDateTime(pow.EndDate) - ParseDateTime(pow.StartDate);
                Console.WriteLine(
                    $"Start: {pow.StartDate}, End: {pow.EndDate}, TS: {ts}, Days: {ts.Days}, Years: {ts.Days / 365}");
                workExperience += ParseDateTime(pow.EndDate) - ParseDateTime(pow.StartDate);
            }

            Console.WriteLine($"TOTAL: {workExperience.Days / 365}");
            return workExperience.Days / 365;
        }
    }
}

//profile/resumes (фильтры на status) + vacancions