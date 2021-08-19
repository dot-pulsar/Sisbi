using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Models
{
    public class FormImage
    {
        [FromForm(Name = "image")] public IFormFile Image { get; set; }
        [FromForm(Name = "format")] public string Format { get; set; }
    }
}