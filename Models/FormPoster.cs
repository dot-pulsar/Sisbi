using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Models
{
    public class FormPoster
    {
        [FromForm(Name = "poster")] public IFormFile Poster { get; set; }
        [FromForm(Name = "format")] public string Format { get; set; }
    }
}