/* Not in use kept for future use.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MMSConstants;
using Tev.API.Models;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.HelperService;
using Tev.DAL.RepoContract;
using Tev.IotHub;

namespace Tev.API.Controllers
{
    /// <summary>
    /// APIs for Emergency Call History
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]

    public class EmergencyCallHistoryController : TevControllerBase
    {
        private readonly IGenericRepo<EmergencyCallHistory> _emergencyCallHistoryRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITevIoTRegistry _tevIoTRegistry;
        private readonly ILogger<EmergencyCallHistoryController> _logger;
        private readonly IUserDevicePermissionService _devicePermissionService;

        public EmergencyCallHistoryController(IGenericRepo<EmergencyCallHistory> emergencyCallHistoryRepo, IUserDevicePermissionService devicePermissionService,
            IUnitOfWork unitOfWork, ITevIoTRegistry tevIoTRegistry, ILogger<EmergencyCallHistoryController> logger)
        {
            _emergencyCallHistoryRepo = emergencyCallHistoryRepo;
            _unitOfWork = unitOfWork;
            _tevIoTRegistry = tevIoTRegistry;
            _logger = logger;
            _devicePermissionService = devicePermissionService;
            
        }

        /// <summary>
        /// create emergency call history post method
        /// </summary>
        /// <param name="model">model contains emergencycallhistoryId,number,time,calling purpose,deviceId.</param>
        /// <returns>it return success message </returns>
        [NonAction]
        [HttpPost("createEmergencyCallHistory")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult CreateEmergencyCallHistory([FromBody] EmergencyCallHistoryModel model)
        {
            try
            {   
              if(model != null)
                {
                    if ((IsOrgAdmin(CurrentApplications) || _devicePermissionService.GetDeviceIdForOwner(UserEmail).Contains(model.DeviceId) ||
                   _devicePermissionService.GetDeviceIdForEditor(UserEmail).Contains(model.DeviceId)))
                    {
                        using (var transaction = _unitOfWork.BeginTransaction())
                        {
                            try
                            {
                                var resp = new EmergencyCallHistory()
                                {
                                    EmergencyCallHistoryId = Guid.NewGuid().ToString(),
                                    Number = model.Number,
                                    Time = model.Time,
                                    CallingPurpose = model.CallingPurpose,
                                    DeviceId = model.DeviceId,
                                    CreatedBy = UserEmail,
                                    ModifiedBy = UserEmail
                                };

                                _emergencyCallHistoryRepo.Add(resp);
                                _emergencyCallHistoryRepo.SaveChanges();

                                transaction.Commit();
                                _logger.LogInformation("Emergency call history successfully Added on sql");
                                return Ok(new MMSHttpReponse<string>() { ResponseBody = resp.EmergencyCallHistoryId, SuccessMessage = "Emergency Call History Successfully Created." });
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                _logger.LogError("Error Occured on Create Emergency Call history on sql {exception}", e);
                                return StatusCode(StatusCodes.Status500InternalServerError);
                            }
                        }
                    }
                    else
                    {
                        return Forbid();
                    }
                }
              else
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Create Emergency Call history on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// update emergency call history data post method
        /// </summary>
        /// <param name="model">model contains emergencycallhistoryId,number,time,calling purpose.</param>
        /// <returns>it return success message</returns>
        [HttpPost("updateEmergencyCallHistory")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult UpdateEmergencyCallHistory([FromBody] EmergencyCallHistoryModel model)
        {
            try
            {
                var emergencyCallHistory = _emergencyCallHistoryRepo.Query(z => z.EmergencyCallHistoryId == model.EmergencyCallHistoryId).FirstOrDefault();

                if (emergencyCallHistory == null)
                {
                    return NotFound(new MMSHttpReponse<string>() { ErrorMessage = "Requested Emergency Call History not found"});
                }

                
                // (device should exist and user is org admin ) or (user is site admin and Device belongs to that site)
                if ((IsOrgAdmin(CurrentApplications) || _devicePermissionService.GetDeviceIdForOwner(UserEmail).Contains(model.DeviceId) ||
                    _devicePermissionService.GetDeviceIdForEditor(UserEmail).Contains(model.DeviceId)))
                {


                    using (var transaction = _unitOfWork.BeginTransaction())
                    {
                        try
                        {
                            emergencyCallHistory.Number = model.Number;
                            emergencyCallHistory.Time = model.Time;
                            emergencyCallHistory.CallingPurpose = model.CallingPurpose;
                            emergencyCallHistory.ModifiedBy = UserEmail;
                            emergencyCallHistory.DeviceId = model.DeviceId;
                            _emergencyCallHistoryRepo.Update(emergencyCallHistory);
                            _emergencyCallHistoryRepo.SaveChanges();


                            transaction.Commit();
                            _logger.LogInformation("emergency call history successfully updated");
                            return Ok(new MMSHttpReponse<string>() { ResponseBody = "Update Emergency Call History Successfully",
                                SuccessMessage = "Emergency Call History Successfully Update." });
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            _logger.LogError("Error Occured on Update Emergency Call history on sql {exception}", e);
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                    }
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Update Emergency Call history on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// Get emergency call history by emergencycallhistoryId
        /// </summary>
        /// <param name="emergencyCallHistoryId"> emergencycallhistoryId</param>
        /// <returns>it returns single emergency call history details.</returns>
        [HttpGet("getEmergencyCallHistoryById")]
        [ProducesResponseType(typeof(MMSHttpReponse<EmergencyCallHistoryModel>), StatusCodes.Status200OK)]
        public IActionResult GetEmergencyCallHistoryById([FromQuery] string emergencyCallHistoryId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emergencyCallHistoryId))
                {
                    return BadRequest(new MMSHttpReponse<EmergencyCallHistoryModel>() { ErrorMessage = "Invalid Emergency Call HistoryId"});
                }

                var emergencyCallHistory = _emergencyCallHistoryRepo.Query(z => z.EmergencyCallHistoryId == emergencyCallHistoryId).FirstOrDefault();

                if (emergencyCallHistory == null)
                {
                    return NotFound(new MMSHttpReponse<EmergencyCallHistoryModel>() { ErrorMessage = "Requested Emergency Call History not found"});
                }
                
                // (device should exist and user is org admin ) or (user is site admin and Device belongs to that site)
                if ((IsOrgAdmin(CurrentApplications) || _devicePermissionService.GetDeviceIdForViewer(UserEmail).Contains(emergencyCallHistory.DeviceId)))
                {
                    var resp = new MMSHttpReponse<EmergencyCallHistoryModel>()
                    {
                        ResponseBody = new EmergencyCallHistoryModel()
                        {
                            EmergencyCallHistoryId = emergencyCallHistory.EmergencyCallHistoryId,
                            CallingPurpose = emergencyCallHistory.CallingPurpose,
                            Number = emergencyCallHistory.Number,
                            Time = emergencyCallHistory.Time,
                            DeviceId = emergencyCallHistory.DeviceId
                        }
                    };

                    return Ok(resp);
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Emergency Call history by id on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// delete emergency call history by Id
        /// </summary>
        /// <param name="emergencyCallHistoryId">emergencycallhistoryId</param>
        /// <returns>it return success message</returns>
        [HttpDelete("deleteEmergencyCallHistory")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult DeleteEmergencyCallHistory([FromQuery] string emergencyCallHistoryId)
        {
            try
            {
                var emergencyCallHistory = _emergencyCallHistoryRepo.Query(z => z.EmergencyCallHistoryId == emergencyCallHistoryId).FirstOrDefault();

                if (emergencyCallHistory == null)
                {
                    return NotFound(new MMSHttpReponse<string>() { ErrorMessage = "Requested Emergency Call History not found"});
                }
                
                // (device should exist and user is org admin ) or (user is site admin and Device belongs to that site)
                if ((IsOrgAdmin(CurrentApplications) || _devicePermissionService.GetDeviceIdForOwner(UserEmail).Contains(emergencyCallHistory.DeviceId) 
                    || _devicePermissionService.GetDeviceIdForEditor(UserEmail).Contains(emergencyCallHistory.DeviceId)))
                {
                    using (var transaction = _unitOfWork.BeginTransaction())
                    {
                        try
                        {
                            _emergencyCallHistoryRepo.Remove(emergencyCallHistory);
                            _emergencyCallHistoryRepo.SaveChanges();
                            transaction.Commit();
                            _logger.LogInformation("Emergency Call history successfully deleted");
                            return Ok(new MMSHttpReponse<string>() { ResponseBody = "Delete Emergency Call History Successfully Deleted",
                                SuccessMessage = "Emergency Call History Successfully Deleted." });
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            _logger.LogError("Error Occured on Delete Emergency Call history on sql {exception}", e);
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                    }
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Delete Emergency Call history on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// Get all emergency call history paginated
        /// </summary>
        /// <param name="req">request contains pageNo,pagesize,sortBy,sortOrder,search</param>
        /// <returns>it returns all emergency call history paginated response</returns>
        [HttpPost("getAllEmergencyCallHistoryPaginated")]
        [ProducesResponseType(typeof(MMSHttpReponse<GetAllPaginateResponse<EmergencyCallHistoryModel>>), StatusCodes.Status200OK)]
        public IActionResult GetAllEmergencyCallHistoryPaginated([FromBody] GetAllTechnicianPaginateReq req)
        {
            try
            {

                var devices = _devicePermissionService.GetDeviceIdForViewer(UserEmail);
                var emergencyCallHistory = _emergencyCallHistoryRepo.Query(q => devices.Contains(q.DeviceId));

                if (!string.IsNullOrWhiteSpace(req.Search))
                {
                    emergencyCallHistory = emergencyCallHistory.Where(z => z.Number.Contains(req.Search)
                                                                        || z.CallingPurpose.Contains(req.Search));
                }

                if (string.IsNullOrWhiteSpace(req.SortBy))
                {
                    req.SortBy = "ModifiedDate";
                }

                var allCount = emergencyCallHistory.Count();

                switch (req.SortBy.ToUpper())
                {
                    case "NUMBER":
                        {
                            emergencyCallHistory = req.SortOrder == "asc" ? emergencyCallHistory.OrderBy(z => z.Number) : emergencyCallHistory.OrderByDescending(z => z.Number);
                            break;
                        }

                    case "TIME":
                        {
                            emergencyCallHistory = req.SortOrder == "asc" ? emergencyCallHistory.OrderBy(z => z.Time) : emergencyCallHistory.OrderByDescending(z => z.Time);
                            break;
                        }

                    default:
                        {
                            emergencyCallHistory = emergencyCallHistory.OrderByDescending(z => z.ModifiedDate);
                            break;
                        }
                }

                var data = emergencyCallHistory.Skip(((req.PageNo - 1) * req.PageSize)).Take(req.PageSize).Select(e => new EmergencyCallHistoryModel()
                {
                    EmergencyCallHistoryId = e.EmergencyCallHistoryId,
                    Number = e.Number,
                    Time = e.Time,
                    CallingPurpose = e.CallingPurpose,
                    DeviceId = e.DeviceId
                }).ToList();

                var resp = new MMSHttpReponse<GetAllPaginateResponse<EmergencyCallHistoryModel>>()
                {
                    ResponseBody = new GetAllPaginateResponse<EmergencyCallHistoryModel>()
                    {
                        AllRecordCount = allCount,
                        Data = data,
                        PageNo = req.PageNo,
                        PageSize = req.PageSize,
                        SortBy = req.SortBy,
                        SortOrder = req.SortOrder,
                    }
                };


                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Emergency Call history paginated on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
*/
