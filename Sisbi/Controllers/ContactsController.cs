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
    [Route("[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        [Authorize(Roles = "Worker,Employer"), HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.Id();

            var contacts = await SisbiContext.Connection.QueryAsync<Contact>(
                $"SELECT c.* FROM contact c LEFT JOIN resume r on c.resume_id = r.id WHERE r.user_id = '{userId}'");

            var contactsResult = contacts.Select(c => new
                {
                    id = c.Id,
                    phone = c.Phone,
                    email = c.Email,
                    resume_id = c.ResumeId
                })
                .ToList();

            return Ok(new
            {
                success = true,
                contacts = contactsResult
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var contact = await SisbiContext.Connection.QuerySingleOrDefaultAsync<Contact>(
                $"SELECT * FROM contact WHERE id = '{id}'");

            if (contact != null)
            {
                return Ok(new
                {
                    id = contact.Id,
                    phone = contact.Phone,
                    email = contact.Email,
                    resume_id = contact.ResumeId
                });
            }

            return BadRequest(new
            {
                success = false,
                description = "Contact not fond"
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactRequest body)
        {
            #region Validate

            if (string.IsNullOrEmpty(body.Phone))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "phone field is required."
                });
            }

            if (string.IsNullOrEmpty(body.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "email field is required."
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

            var contact = await SisbiContext.CreateAsync<Contact>(new
            {
                phone = body.Phone,
                email = body.Email,
                resume_id = body.ResumeId
            }, returning: true);

            return Ok(new
            {
                success = true,
                id = contact.Id,
                phone = contact.Phone,
                email = contact.Email,
                resume_id = contact.ResumeId
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] ContactRequest body)
        {
            #region Validate

            if (string.IsNullOrEmpty(body.Phone))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "phone field is required."
                });
            }

            if (string.IsNullOrEmpty(body.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "email field is required."
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

            var contact = await SisbiContext.GetAsync<Contact>(id);

            if (contact == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "contact not found."
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

            contact = await SisbiContext.UpdateAsync<Contact>(id, new
            {
                phone = body.Phone,
                email = body.Email,
                resume_id = body.ResumeId
            }, returning: true);

            return Ok(new
            {
                success = true,
                id = contact.Id,
                phone = contact.Phone,
                email = contact.Email,
                resume_id = contact.ResumeId
            });
        }

        [Authorize(Roles = "Worker,Employer"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var userId = User.Id();

            var contact = await SisbiContext.Connection.QuerySingleOrDefaultAsync<Contact>(
                $"SELECT * FROM contact c LEFT JOIN resume r on c.resume_id = r.id WHERE c.id = '{id}' AND r.user_id = '{userId}'");

            if (contact != null)
            {
                await SisbiContext.DeleteAsync<Contact>(id);
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Contact not found."
                });
            }

            return Ok(new
            {
                success = true
            });
        }
    }
}