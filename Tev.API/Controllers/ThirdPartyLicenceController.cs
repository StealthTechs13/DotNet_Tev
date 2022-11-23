using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ThirdPartyLicenceController : TevControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public ThirdPartyLicenceController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult GetThirdPartyLicence()
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "thirdPartyLicence", "licence.html")))
            {
                var licenceBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(_env.WebRootPath, "thirdPartyLicence", "licence.html"));
                return File(licenceBytes, "text/html");
            }
            else 
            {
                return BadRequest("Licence not found");
            }
        }
    }
}
