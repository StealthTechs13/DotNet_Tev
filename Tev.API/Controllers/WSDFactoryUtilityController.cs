using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Tev.API.Models;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;

namespace Tev.API.Controllers
{
    /// <summary>
    /// APIs for device management
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class WSDFactoryUtilityController : TevControllerBase
    {
        private readonly ILogger<WSDFactoryUtilityController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepo<DeviceFactoryData> _deviceFactoryDataRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public WSDFactoryUtilityController(IConfiguration configuration,ILogger<WSDFactoryUtilityController> logger, IGenericRepo<DeviceFactoryData> deviceFactoryDataRepo, IUnitOfWork unitOfWork, IWebHostEnvironment env)
        {
            _logger = logger;
            _deviceFactoryDataRepo = deviceFactoryDataRepo;
            _unitOfWork = unitOfWork;
            _env = env;
            _configuration = configuration;
        }

        /// <summary>
        /// Adds a device factory data to the database for reporting
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("addDeviceFactoryData")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddDeviceFactoryData([FromBody] AddDeviceFactoryDataRequest reqBody)
        {
            try
            {
                if (reqBody == null)
                {
                    return BadRequest();
                }
                var WSDUtilityUsers = _configuration.GetSection("WSDUtilityUsers").Value.Split(',');// exceptionEmails are the list of sales/quality people emails
                if(!WSDUtilityUsers.Contains(UserEmail))
                {
                    return Forbid();
                }
                using (var transaction = _unitOfWork.BeginTransaction())
                    {
                        try
                        {
                            var deviceFactoryData = new DeviceFactoryData
                            {
                                CreatedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds(),
                                DeviceName = reqBody.DeviceName,
                                Result = reqBody.Result
                            };
                            await _deviceFactoryDataRepo.AddAsync(deviceFactoryData);
                        _deviceFactoryDataRepo.SaveChanges();
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError("Error Occured while adding Factory Device Data  on Sql {exception}", ex);
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                    }
                
                return Ok(new MMSHttpReponse { SuccessMessage = "Factory Device Data added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while adding Factory Device Data  {exception}", ex);
                return BadRequest(new MMSHttpReponse { ErrorMessage = ex.Message + " " + ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Gets passed device factory data on basis of device fromSrNo and toSrNo
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetDeviceFactoryData")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public ActionResult<string> GetDeviceFactoryData([FromQuery] string fromSrNo, string toSrNo , string fromDate , string toDate)
        {
            try
            {

                DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                var fromDateE = new DateTimeOffset(DateTime.Today).ToUnixTimeSeconds();
                var toDateE = new DateTimeOffset(DateTime.Today.AddDays(1)).ToUnixTimeSeconds();
              
                if (!string.IsNullOrEmpty(fromDate))
                {
                    DateTime dateTime = DateTime.ParseExact(fromDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                    fromDateE = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
                }

                if (!string.IsNullOrEmpty(toDate))
                {
                    DateTime dateTime = DateTime.ParseExact(toDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                    toDateE = new DateTimeOffset(dateTime.AddDays(1)).ToUnixTimeSeconds();
                }
                var result = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true && (start.AddSeconds(d.CreatedDate).AddHours(5).AddMinutes(30) > start.AddSeconds(fromDateE).AddHours(5).AddMinutes(30)) && (start.AddSeconds(d.CreatedDate).AddHours(5).AddMinutes(30) < start.AddSeconds(toDateE).AddHours(5).AddMinutes(30))).ToList().OrderBy(x => x.CreatedDate).ToList();
                
                if(result.Count < 1)
                {
                    return Ok(new MMSHttpReponse<string> { ResponseBody = "< p > Total Passed Devices in Date Range " + fromDate + " - " + toDate + " is " + result.Count() });
                    
                }

                int count = Convert.ToInt32(toSrNo) < result.Count() ? Convert.ToInt32(toSrNo) : result.Count();
                var subResult = result.GetRange(Convert.ToInt32(fromSrNo)-1, count);
                //var result = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true && (start.AddSeconds(d.CreatedDate).AddHours(5).AddMinutes(30) > start.AddSeconds(fromDateE).AddHours(5).AddMinutes(30)) && (start.AddSeconds(d.CreatedDate).AddHours(5).AddMinutes(30) < start.AddSeconds(toDateE).AddHours(5).AddMinutes(30))).ToList().OrderByDescending(x => x.CreatedDate).Take(deviceCountN);
                //var result = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true && d.Id >= Convert.ToInt32(fromSrNo) && d.Id <= Convert.ToInt32(toSrNo)) ;
                StringBuilder tbl = new StringBuilder(string.Empty);
                var columnName = string.Empty;
                var report = string.Empty;
                tbl.Append("<div>");
                tbl.Append("<table>");
                tbl.Append("<tr>");
                tbl.Append("<th>Sr No</th>");
                tbl.Append("<th>Device Name</th>");
                tbl.Append("<th>Created Date</th>");
               
                
               var srno = 0;
                foreach(var r in subResult)
                {
                    srno = srno + 1;
                    tbl.Append("<tr>");
                    tbl.Append("<th>" + srno + "</th>");
                    tbl.Append("<th>" + r.DeviceName + "</th>");
                    tbl.Append("<th>" + start.AddSeconds(r.CreatedDate).AddHours(5).AddMinutes(30) + "</th>");
                    tbl.Append("</tr>");
                }
                tbl.Append("</tr>");
                tbl.Append("</table>");
                tbl.Append("<p> Total Passed Devices in Date Range " + fromDate + "-" + toDate + " are " + result.Count());
                tbl.Append("</div>");
               
                if (System.IO.File.Exists(System.IO.Path.Combine(_env.WebRootPath, "WsdFactoryData.html")))
                {
                     report = System.IO.File.ReadAllText(System.IO.Path.Combine(_env.WebRootPath, "WsdFactoryData.html"));
                    report = report.Replace("{{tbl}}", tbl.ToString());
                }
                
                //return report;
                return Ok(new MMSHttpReponse<string> { ResponseBody = report });

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while retrieving device for User {UserEmail} and application {CurrentApplications}  Exception :- {exception}", UserEmail, CurrentApplications, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// Gets passed device factory data on basis of device count, device creation from and to date
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetDevicePassFailCount")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<DevicePassFailResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevicePassFailCount()
        {
            try
            {
                var today = new DateTimeOffset(DateTime.Today).ToUnixTimeSeconds();
                var todayData = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true && d.CreatedDate > today);
                var TodayFirstSrNo = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true && d.CreatedDate > today).OrderBy(d => d.Id).FirstOrDefault();
                var TodayLastSrNo = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true && d.CreatedDate > today).OrderByDescending(d => d.Id).FirstOrDefault();
                var result = new DevicePassFailResponse()
                {
                    TodayPass = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true && d.CreatedDate > today).Count(),
                    TodayFail = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == false && d.CreatedDate > today).Count(),
                    TotalPass = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == true).Count(),
                    TotalFail = _deviceFactoryDataRepo.GetAll().Where(d => d.Result == false).Count(),
                    TodayFirstSrNo = TodayFirstSrNo?.Id == null ? "NA": TodayFirstSrNo?.Id.ToString(),
                    TodayLastSrNo = TodayLastSrNo?.Id == null ? "NA" : TodayLastSrNo?.Id.ToString(),
                };

                return Ok(new MMSHttpReponse<DevicePassFailResponse> { ResponseBody = result });

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while retrieving device for User {UserEmail} and application {CurrentApplications}  Exception :- {exception}", UserEmail, CurrentApplications, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }
    }

   
}

    

