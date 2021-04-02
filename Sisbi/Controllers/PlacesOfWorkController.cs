using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize(Roles = "Worker"), HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.Id();

            var pow = await SisbiContext.Connection.QueryAsync<PlaceOfWork>(
                $"SELECT pow.* FROM place_of_work pow LEFT JOIN resume r on pow.resume_id = r.id WHERE r.user_id = '{userId}'");

            var powResult = pow.Select(place => new
                {
                    Id = place.Id,
                    Position = place.Position,
                    Company = place.Company,
                    Description = place.Description,
                    StartDate = place.StartDate,
                    EndDate = place.EndDate,
                    ResumeId = place.ResumeId
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
            var pow = await SisbiContext.Connection.QuerySingleOrDefaultAsync<PlaceOfWork>(
                $"SELECT * FROM place_of_work WHERE id = '{id}'");

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

            var resume = await SisbiContext.GetAsync<Resume>(body.ResumeId);
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

            var pow = await SisbiContext.CreateAsync<PlaceOfWork>(new
            {
                position = body.Position,
                company = body.Company,
                description = body.Description,
                start_date = body.StartDate,
                end_date = body.EndDate,
                resume_id = body.ResumeId
            }, returning: true);

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

            var pow = await SisbiContext.GetAsync<PlaceOfWork>(id);

            if (pow == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "place_of_work not found."
                });
            }

            var resume = await SisbiContext.GetAsync<Resume>(body.ResumeId);
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

            pow = await SisbiContext.UpdateAsync<PlaceOfWork>(id, new
            {
                position = body.Position,
                company = body.Company,
                description = body.Description,
                start_date = body.StartDate,
                end_date = body.EndDate,
                resume_id = body.ResumeId
            }, returning: true);

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

            var pow = await SisbiContext.Connection.QuerySingleOrDefaultAsync(
                $"SELECT * FROM place_of_work pow LEFT JOIN resume r on pow.resume_id = r.id WHERE pow.id = '{id}' AND r.user_id = '{userId}'");

            if (pow != null)
            {
                await SisbiContext.DeleteAsync<PlaceOfWork>(id);
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Place not found."
                });
            }

            return Ok(new
            {
                success = true
            });
        }
        
        private static bool IsDateValid(string date)
        {
            var format = "MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;

            return DateTime.TryParseExact(date, format, provider, style, out var temp);
        }
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