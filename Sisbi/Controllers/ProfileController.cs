using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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

        public ProfileController(SisbiContext context)
        {
            _context = context;
        }
        
        [Authorize, HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = User.Id();
            var user = await _context.Users.FindAsync(userId);
            
            return Ok(new
            {
                success = true,
                id = user.Id,
                role = user.Role,
                first_name = user.FirstName,
                second_name = user.SecondName,
                gender = user.Gender,
                bdate = user.BDate,
                address = user.Address,
                email = user.Email,
                email_confirmed = user.EmailConfirmed,
                phone = user.Phone,
                phone_confirmed = user.PhoneConfirmed,
                registration_date = user.RegistrationDate
            });
        }

        [Authorize, HttpPut]
        public async Task<IActionResult> Edit([FromBody] EditBody body)
        {
            var format = "dd-MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;

            if (!string.IsNullOrEmpty(body.BDate) && !DateTime.TryParseExact(body.BDate, format, provider, style, out var temp))
            {
                return BadRequest("Неверная дата");
            }

            var userId = User.Id();
            var user = await _context.Users.FindAsync(userId);

            user.FirstName = body.FirstName;
            user.SecondName = body.SecondName;
            user.Gender = body.Gender;
            user.BDate = body.BDate;
            user.Address = body.Address;

            await _context.SaveChangesAsync();
            
            return Ok(new
            {
                success = true,
                id = user.Id,
                role = user.Role,
                first_name = user.FirstName,
                second_name = user.SecondName,
                gender = user.Gender,
                bdate = user.BDate,
                address = user.Address,
                email = user.Email,
                email_confirmed = user.EmailConfirmed,
                phone = user.Phone,
                phone_confirmed = user.PhoneConfirmed,
                registration_date = user.RegistrationDate
            });
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
                data = vacancies
            });
        }

        public class EditBody
        {
            [JsonPropertyName("first_name")] public string FirstName { get; set; }
            [JsonPropertyName("second_name")] public string SecondName { get; set; }
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
    }
}