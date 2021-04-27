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
    [ApiController]
    [Route("api/v1/places_of_work")]
    public class PlacesOfWorkController : ControllerBase
    {
        private readonly SisbiContext _context;

        public PlacesOfWorkController(SisbiContext context)
        {
            _context = context;
        }

        
    }
}