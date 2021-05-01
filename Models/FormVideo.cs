using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sisbi.Controllers
{
    public class FormVideo
    {
        [FromForm(Name = "video")] public IFormFile Video { get; set; }
        [FromForm(Name = "format")] public string Format { get; set; }
    }
}