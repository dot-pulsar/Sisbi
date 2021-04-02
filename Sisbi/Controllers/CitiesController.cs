using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.Entities;

namespace Sisbi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        //TODO: методы для администратора

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cities = await SisbiContext.Connection.QueryAsync<City>(
                $"SELECT * FROM city");

            return Ok(new
            {
                success = true,
                cities
            });
        }

        [AllowAnonymous, HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var city = await SisbiContext.Connection.QuerySingleOrDefaultAsync<City>(
                $"SELECT * FROM city WHERE id = '{id}'");

            if (city != null)
            {
                return Ok(new
                {
                    success = true,
                    id = city.Id,
                    name = city.Name
                });
            }

            return BadRequest(new
            {
                success = false,
                description = "City not fond"
            });
        }

        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> Create(City body)
        {
            if (string.IsNullOrEmpty(body.Name))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Name field is required."
                });
            }

            var city = await SisbiContext.CreateAsync<City>(new
            {
                name = body.Name
            }, returning: true);

            return Ok(new
            {
                success = true,
                id = city.Id,
                name = city.Name
            });
        }

        [AllowAnonymous, HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromBody] City body)
        {
            if (string.IsNullOrEmpty(body.Name))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Name field is required."
                });
            }

            var city = await SisbiContext.GetAsync<City>(id);

            if (city != null)
            {
                await SisbiContext.UpdateAsync<City>(id, new
                {
                    name = body.Name
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    description = "City not found."
                });
            }

            return Ok(new
            {
                success = true,
                id,
                name = body.Name
            });
        }

        [AllowAnonymous, HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var city = await SisbiContext.GetAsync<City>(id);

            if (city != null)
            {
                await SisbiContext.DeleteAsync<City>(id);
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    description = "City not found."
                });
            }

            return Ok(new
            {
                success = true
            });
        }
    }
}