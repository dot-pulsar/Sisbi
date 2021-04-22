using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Entities;

namespace Sisbi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        private readonly SisbiContext _context;

        public CitiesController(SisbiContext context)
        {
            _context = context;
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cities = await _context.Cities.ToListAsync();

            return Ok(new
            {
                success = true,
                cities
            });
        }

        [AllowAnonymous, HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var city = await _context.Cities.FindAsync(id);

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

        [Authorize(Roles = "Administrator"), HttpPost]
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

            var city = new City
            {
                Name = body.Name
            };

            await _context.Cities.AddAsync(city);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = city.Id,
                name = city.Name
            });
        }

        [Authorize(Roles = "Administrator"), HttpPut("{id}")]
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

            var city = await _context.Cities.FindAsync(id);

            if (city != null)
            {
                city.Name = body.Name;
                await _context.SaveChangesAsync();
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

        [Authorize(Roles = "Administrator"), HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var city = await _context.Cities.FindAsync(id);

            if (city != null)
            {
                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();
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