using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;
using Models.Requests;
using Sisbi.Extensions;

namespace Sisbi.Controllers
{
    [Route("places_of_work")]
    [ApiController]
    public class PlacesOfWorkController : ControllerBase
    {
        private readonly SisbiContext _context;

        public PlacesOfWorkController(SisbiContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Worker"), HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.Id();

            var pow = await _context.PlacesOfWork
                .Where(p => p.Resume.UserId == userId)
                .ToListAsync();

            var powResult = pow.Select(place => new
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

        [AllowAnonymous, HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var pow = await _context.PlacesOfWork.FindAsync(id);

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

        [Authorize(Roles = "Worker"), HttpPost]
        public async Task<IActionResult> Create([FromBody] PlaceOfWorkRequest body)
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

            if (body.ResumeId == Guid.Empty)
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

            var resume = await _context.Resumes.FindAsync(body.ResumeId);
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
                ResumeId = body.ResumeId
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

        [Authorize(Roles = "Worker"), HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] PlaceOfWorkRequest body)
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

            if (body.ResumeId == Guid.Empty)
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

            var resume = await _context.Resumes.FindAsync(body.ResumeId);
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
            pow.ResumeId = body.ResumeId;

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

        [Authorize(Roles = "Worker"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var userId = User.Id();

            var pow = await _context.PlacesOfWork.FindAsync(id);
            
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
    }
}