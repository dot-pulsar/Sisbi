using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.Entities;
using Models.Enums;
using Newtonsoft.Json;
using Sisbi.Extensions;

namespace Sisbi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProfileController : ControllerBase
    {
        //TODO:// Добавить маппинг

        [Authorize, HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = User.Id();
            var user = await SisbiContext.GetAsync<User>(userId);
            return Ok(new Profile
            {
                Id = user.Id,
                Role = user.Role,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Gender = user.Gender,
                BDate = user.BDate,
                Address = user.Address,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Phone = user.Phone,
                PhoneConfirmed = user.PhoneConfirmed,
                RegistrationDate = user.RegistrationDate
            });
        }

        [Authorize, HttpPut]
        public async Task<IActionResult> Edit([FromBody] EditBody body)
        {
            var format = "dd-MM-yyyy";
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.None;

            if (!DateTime.TryParseExact(body.BDate, format, provider, style, out var temp))
            {
                return BadRequest("Неверная дата");
            }

            var userId = User.Id();

            var user = await SisbiContext.UpdateAsync<User>(userId, new
            {
                first_name = body.FirstName,
                second_name = body.SecondName,
                gender = body.Gender,
                bdate = body.BDate,
                address = body.Address
            }, returning: true);

            return Ok(new Profile
            {
                Id = user.Id,
                Role = user.Role,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Gender = user.Gender,
                BDate = user.BDate,
                Address = user.Address,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Phone = user.Phone,
                PhoneConfirmed = user.PhoneConfirmed,
                RegistrationDate = user.RegistrationDate
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