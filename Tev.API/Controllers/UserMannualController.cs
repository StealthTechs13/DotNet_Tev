using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Models;

namespace Tev.API.Controllers
{


    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class UserManualController : TevControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserManualController> _logger;

        public UserManualController(IConfiguration configuration, ILogger<UserManualController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gets User Manual PDF of the Device Type Passed to the API.
        /// </summary>
        /// <param name="deviceType"></param>
        /// <returns></returns>
         [HttpGet("userManual")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserManual(string deviceType, string viewType = null)
        {
            try
            {
                var client = new BlobContainerClient(_configuration.GetSection("blob").GetSection("ConnectionString").Value,
                   _configuration.GetSection("blob").GetSection("ContainerName").Value);
                var absolureUri = Helpers.GetServiceSasUriForContainer(client).AbsoluteUri;
                var wsdHtmlUrl = _configuration.GetSection("wsdHtmlUrl").Value;
                var Tev1HtmlUrl = _configuration.GetSection("Tev1HtmlUrl").Value;
                var Tev2HtmlUrl = _configuration.GetSection("Tev2HtmlUrl").Value;
                var wsdPdfManualName = _configuration.GetSection("wsdPdfManualName").Value;
                var Tev1PdfManualName = _configuration.GetSection("Tev1PdfManualName").Value;
                var Tev2PdfManualName = _configuration.GetSection("Tev2PdfManualName").Value;
                string sas = "";
                if (string.IsNullOrEmpty(absolureUri))
                {
                    _logger.LogError("Unable to generate sas token for alert blob");
                    return Forbid();
                }
                else
                {
                    sas = absolureUri.Split("?")[1];
                }
                string manualUrl = "";
                string baseBlobUrl = this._configuration.GetSection("blob").GetSection("alertblob").Value;
                if (deviceType == nameof(Applications.WSD))
                {
                    if (viewType == null)
                    {
                        manualUrl = baseBlobUrl + $"{wsdPdfManualName}?{sas}";
                    }
                    else
                    {
                        if (viewType.ToLower() == "pdf")
                        {
                            manualUrl = baseBlobUrl + $"{wsdPdfManualName}?{sas}";
                        }
                        else if (viewType.ToLower() == "html")
                        {
                            manualUrl = wsdHtmlUrl;
                        }
                        else
                        {
                            manualUrl = baseBlobUrl + $"{wsdPdfManualName}?{sas}";
                        }
                    }
                }
                else if (deviceType == nameof(Applications.TEV))
                {
                    if (viewType == null)
                    {
                        manualUrl = baseBlobUrl + $"{Tev1PdfManualName}?{sas}";
                    }
                    else
                    {
                        if (viewType.ToLower() == "pdf")
                        {
                            manualUrl = baseBlobUrl + $"{Tev1PdfManualName}?{sas}";
                        }
                        else if (viewType.ToLower() == "html")
                        {
                            manualUrl = Tev1HtmlUrl;
                        }
                        else
                        {
                            manualUrl = baseBlobUrl + $"{Tev1PdfManualName}?{sas}";
                        }
                    }
                }
                else if (deviceType == nameof(Applications.TEV2))
                {
                    if (viewType == null)
                    {
                        manualUrl = baseBlobUrl + $"{Tev2PdfManualName}?{sas}";
                    }
                    else
                    {
                        if (viewType.ToLower() == "pdf")
                        {
                            manualUrl = baseBlobUrl + $"{Tev2PdfManualName}?{sas}";
                        }
                        else if (viewType.ToLower() == "html")
                        {
                            manualUrl = Tev2HtmlUrl;
                        }
                        else
                        {
                            manualUrl = baseBlobUrl + $"{Tev2PdfManualName}?{sas}";
                        }
                    }
                }
                else
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = $"Manual not avilable for device type {deviceType}" });
                }

                return Ok(new MMSHttpReponse<string> { ResponseBody = manualUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while getting user Mannaul  {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
