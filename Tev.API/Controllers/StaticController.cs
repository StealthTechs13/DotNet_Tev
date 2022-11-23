using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tev.API.Models;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class StaticController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public StaticController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [AllowAnonymous]
        [HttpGet("TermsAndCondition")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult TnC()
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "TnC.html")))
            {
                var licenceBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(_env.WebRootPath, "TnC.html"));
                return File(licenceBytes, "text/html");
            }
            else
            {
                return BadRequest("TnC not found");
            }
        }
        
        [AllowAnonymous]
        [HttpGet("EULA")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult EULA()
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "EULA.html")))
            {
                var licenceBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(_env.WebRootPath, "EULA.html"));
                return File(licenceBytes, "text/html");
            }
            else
            {
                return BadRequest("EULA not found");
            }
        }

        [AllowAnonymous]
        [HttpGet("PrivacyPolicy")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult PrivacyPolicy()
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "PrivacyPolicy.html")))
            {
                var licenceBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(_env.WebRootPath, "PrivacyPolicy.html"));
                return File(licenceBytes, "text/html");
            }
            else
            {
                return BadRequest("Privacy Policy not found");
            }
        }

        [HttpGet("FAQ")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult FAQ()
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "FAQ.html")))
            {
                var licenceBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(_env.WebRootPath, "FAQ.html"));
                return File(licenceBytes, "text/html");
            }
            else
            {
                return BadRequest("FAQ not found");
            }
        }

        [HttpGet("support")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult Support()
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "ContactSupport.html")))
            {
                var licenceBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(_env.WebRootPath, "ContactSupport.html"));
                return File(licenceBytes, "text/html");
            }
            else
            {
                return BadRequest("ContactSupport not found");
            }
        }

        [HttpGet("aboutUs")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult AboutUs()
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "AboutUs.html")))
            {
                var licenceBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(_env.WebRootPath, "AboutUs.html"));
                return File(licenceBytes, "text/html");
            }
            else
            {
                return BadRequest("AboutUs.html not found");
            }
        }
    }
}
