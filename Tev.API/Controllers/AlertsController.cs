using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMSConstants;
using Tev.API.Enums;
using Tev.API.Mocks;
using Tev.API.Models;
using Tev.Cosmos;
using Tev.Cosmos.Entity;
using Tev.DAL.HelperService;
using Tev.IotHub;

namespace Tev.API.Controllers
{
    /// <summary>
    /// APIs for alert management
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class AlertsController : TevControllerBase
    {
        private readonly IAlertRepo _alertRepo;
        private readonly IUserDevicePermissionService _userDevicePermissionChanges;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AlertsController> _logger;
       

        public AlertsController(IAlertRepo alertRepo, IUserDevicePermissionService userDevicePermissionChanges, IConfiguration configuration,ILogger<AlertsController> logger)
        {
            _alertRepo = alertRepo;
            _userDevicePermissionChanges = userDevicePermissionChanges;
            _configuration = configuration;
            _logger = logger;
        }

        
        /// <summary>
        /// Gets all the alerts across all location and devices and all acknowledgement status
        /// </summary>
        /// <param name = "reqBody" ></param>
        /// <returns></returns>
        [HttpPost]
       [ProducesResponseType(typeof(MMSHttpReponse<AlertResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromBody] GetAlertsRequest reqBody)
        {
            try
            {
                var alertTypeIds = new List<int>(); 
                var client = new BlobContainerClient(_configuration.GetSection("blob").GetSection("ConnectionString").Value,
                    _configuration.GetSection("blob").GetSection("ContainerName").Value);
                var absolureUri = Helpers.GetServiceSasUriForContainer(client).AbsoluteUri;
                
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
                if(reqBody != null)
                {
                    if (reqBody.AlertType == null)
                        alertTypeIds = null;
                    else
                        alertTypeIds.AddRange(reqBody.AlertType);

                    if (reqBody.AlertType != null && reqBody.AlertType.Contains((int)AlertType.FireOfflineIncident))
                    {
                        if(!alertTypeIds.Contains((int)AlertType.Fire))
                            alertTypeIds.Add((int)AlertType.Fire);
                        if(!alertTypeIds.Contains((int)AlertType.FireTest))
                            alertTypeIds.Add((int)AlertType.FireTest);
                    }
                    
                    var alertList = new List<Alert>();
                    switch (Helper.AlertFilterHelper(reqBody))
                    {
                        case AlertFilter.Device:
                            {
                                var alertByDevice = await _alertRepo.GetAlertsByDevice(new List<string> { reqBody.DeviceId },
                                OrgId, reqBody.Take, reqBody.Skip, alertTypeIds, reqBody.Acknowledged, reqBody.IsBookMarked,
                                reqBody.IsCorrect, reqBody.StartDate, reqBody.EndDate,null).ConfigureAwait(false);

                                //Filter for OfflineIncident
                                alertList = OfflineIncidentFilter(alertByDevice, reqBody.AlertType);

                                var retAlertByDevice = MapDTO(alertList, sas);
                                return Ok(new MMSHttpReponse<List<AlertResponse>> { ResponseBody = retAlertByDevice, SuccessMessage = reqBody.DeviceId });

                            }
                        case AlertFilter.Location:
                            {
                                var alertByLocation = await _alertRepo.GetAlertsByLocation(new List<string> { reqBody.LocationId },
                                    OrgId, reqBody.Take, reqBody.Skip, alertTypeIds, reqBody.Acknowledged, reqBody.IsBookMarked,
                                    reqBody.IsCorrect, reqBody.StartDate, reqBody.EndDate).ConfigureAwait(false);

                                //Filter for OfflineIncident
                                alertList = OfflineIncidentFilter(alertByLocation, reqBody.AlertType);

                                var retAlertByLocation = MapDTO(alertList, sas);
                                return Ok(new MMSHttpReponse<List<AlertResponse>> { ResponseBody = retAlertByLocation });

                            }
                        default:
                            {
                                string deviceType = null;
                                if (reqBody.Device.HasValue)
                                {
                                    if (reqBody.Device == MMSConstants.Applications.TEV)
                                    {
                                        deviceType = "TEV";
                                    }
                                    if (reqBody.Device == MMSConstants.Applications.WSD)
                                    {
                                        deviceType = "WSD";
                                    }
                                    if (reqBody.Device == MMSConstants.Applications.TEV2)
                                    {
                                        deviceType = "TEV2";
                                    }
                                }
                                if (IsOrgAdmin(CurrentApplications))
                                {
                                    var allAlerts = await _alertRepo.GetAlertsByOrg(
                                        OrgId, reqBody.Take, reqBody.Skip, alertTypeIds, reqBody.Acknowledged,
                                        reqBody.IsBookMarked, reqBody.IsCorrect, reqBody.StartDate, reqBody.EndDate, deviceType
                                        ).ConfigureAwait(false);

                                    //Filter for OfflineIncident
                                    alertList = OfflineIncidentFilter(allAlerts, reqBody.AlertType);

                                    var retAllAlerts = MapDTO(alertList, sas);
                                    return Ok(new MMSHttpReponse<List<AlertResponse>> { ResponseBody = retAllAlerts });

                                }

                                var allPermittedDeviceIds = _userDevicePermissionChanges.GetDeviceIdForViewer(UserEmail);
                                var result = await _alertRepo.GetAlertsByDevice(
                                    allPermittedDeviceIds, OrgId, reqBody.Take, reqBody.Skip, alertTypeIds,
                                    reqBody.Acknowledged, reqBody.IsBookMarked, reqBody.IsCorrect, reqBody.StartDate, reqBody.EndDate, deviceType
                                    ).ConfigureAwait(false);

                                //Filter for OfflineIncident
                                alertList = OfflineIncidentFilter(result, reqBody.AlertType);
                                var ret = MapDTO(alertList, sas);
                                return Ok(new MMSHttpReponse<List<AlertResponse>> { ResponseBody = ret });
                            }

                    };
                }
                
                return BadRequest();
            }
            catch (Exception ex)
            {

                _logger.LogError("Exception occured while getting alerts {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            
        }
        /// <summary>
        /// Acknowledge or un-acknowledge an alert
        /// </summary>
        /// <param name="alertId"></param>
        /// <param name="acknowledge"></param>
        /// <returns></returns>
        [HttpGet("acknowledge/{alertId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Acknowledge(string alertId, bool acknowledge=true)
        {
            try
            {
                if(!IsOrgAdmin(CurrentApplications))
                {
                    var deviceId = _alertRepo.GetDeviceIdOfAlert(alertId, OrgId);
                    var permittedDevices = _userDevicePermissionChanges.GetDeviceIdForViewer(UserEmail);
                    if (!permittedDevices.Contains(deviceId.Result.ToString()))
                    {
                        return Forbid();
                    }
                }
                
                await _alertRepo.AcknowledgeAlert(alertId, OrgId,acknowledge).ConfigureAwait(false);
                return Ok(new MMSHttpReponse { SuccessMessage = "Alert acknowledged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while getting alerts {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Bookmark or un-Bookmark an alert
        /// </summary>
        /// <param name="alertId"></param>
        /// <param name="bookmark"></param>
        /// <returns></returns>
        [HttpGet("bookmark/{alertId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BookMark(string alertId, bool bookmark=true)
        {
            try
            {
                if (!IsOrgAdmin(CurrentApplications))
                {
                    var deviceId = _alertRepo.GetDeviceIdOfAlert(alertId, OrgId);
                    var permittedDevices = _userDevicePermissionChanges.GetDeviceIdForViewer(UserEmail);
                    if (!permittedDevices.Contains(deviceId.Result.ToString()))
                    {
                        return Forbid();
                    }
                }
                await _alertRepo.Bookmark(alertId, OrgId,bookmark);
                return Ok(new MMSHttpReponse { SuccessMessage = "Alert bookmarked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while getting alerts {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Report an alert as incorrect classification
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("reportIncorrect")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReportIncorrect([FromBody] IncorrectAlertRequest reqBody)
        {
            try
            {
                if (!IsOrgAdmin(CurrentApplications))
                {
                    var deviceId = _alertRepo.GetDeviceIdOfAlert(reqBody.AlertId, OrgId);
                    var permittedDevices = _userDevicePermissionChanges.GetDeviceIdForViewer(UserEmail);
                    if (!permittedDevices.Contains(deviceId.Result.ToString()))
                    {
                        return Forbid();
                    }
                }
                if (reqBody == null || string.IsNullOrEmpty(reqBody.AlertId))
                {
                    return BadRequest( new MMSHttpReponse { ErrorMessage = "Alert id cannot be null or empty"});
                }

                await _alertRepo.ReportIncorrect(reqBody.AlertId, reqBody.Comment, OrgId);
                return Ok(new MMSHttpReponse { SuccessMessage = "Alert reported as incorrect" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while getting alerts {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Get the type of alerts
        /// </summary>
        /// <param name="devices">comma separated value of devices or a single device type examples separated by pipe TEV| TEV2 | TEV,TEV2,WSD | WSD </param>
        /// <returns></returns>
        [HttpGet("types")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<TextValueResponse<int>>>), StatusCodes.Status200OK)]
        public IActionResult GetAlertTypes(string devices) 
        {
            try
            {
                var deviceList = new List<string>();
                var ret = new List<TextValueResponse<int>>();
                if (string.IsNullOrEmpty(devices))
                {
                    deviceList.Add("tev");
                }
                else
                {
                    deviceList = devices.ToLower().Split(',').ToList();
                }
                var alertsEnums = Enum.GetNames(typeof(AlertType)).ToList();
                foreach (string device in deviceList)
                {
                    switch (device)
                    {
                        case "tev":
                          
                            for (int i = 0; i < alertsEnums.Count; i++)
                            {
                                // Fire is a type of WSD alert, do not return this alert type in TEV alerts. This API is used only in TEV alert filter
                                if (alertsEnums[i].Contains("Fire"))
                                {
                                    continue;
                                }
                                // DeviceReplacedAlert is a type of WSD alert, do not return this alert type in TEV alerts. This API is used only in TEV alert filter
                                if (alertsEnums[i].Contains("DeviceReplacedAlert"))
                                {
                                    continue;
                                }
                                if (alertsEnums[i].ToLower() == "nomask")
                                {
                                    alertsEnums[i] = "No Mask";
                                }

                                switch (alertsEnums[i])
                                {
                                    case nameof(AlertType.SmartAICameraOnline):
                                        ret.Add(new TextValueResponse<int> { Text = "Smart AI Supervision Online", Value = 51 });
                                        break;
                                    case nameof(AlertType.SmartAICameraOffline):
                                        ret.Add(new TextValueResponse<int> { Text = "Smart AI Supervision Offline", Value = 50 });
                                        break;
                                    default:
                                        ret.Add(new TextValueResponse<int> { Text = alertsEnums[i], Value = i + 1 });
                                        break;
                                }

                            }
                            break;
                        case "tev2":

                            for (int i = 0; i < alertsEnums.Count; i++)
                            {
                                // Fire is a type of WSD alert, do not return this alert type in TEV alerts. This API is used only in TEV alert filter
                                if (alertsEnums[i].Contains("Fire"))
                                {
                                    continue;
                                }
                                if (alertsEnums[i].ToLower() == "nomask")
                                {
                                    alertsEnums[i] = "No Mask";
                                }

                                switch (alertsEnums[i])
                                {
                                    case nameof(AlertType.SmartAICameraOnline):
                                        ret.Add(new TextValueResponse<int> { Text = "Smart AI Camera Online", Value = 51 });
                                        break;
                                    case nameof(AlertType.SmartAICameraOffline):
                                        ret.Add(new TextValueResponse<int> { Text = "Smart AI Camera Offline", Value = 50 });
                                        break;
                                    default:
                                        ret.Add(new TextValueResponse<int> { Text = alertsEnums[i], Value = i + 1 });
                                        break;
                                }

                            }
                            break;
                        case "wsd":
                          
                            for (int i = 0; i < alertsEnums.Count; i++)
                            {
                                switch (alertsEnums[i])
                                {
                                    case "Fire":
                                        ret.Add(new TextValueResponse<int> { Text = "Smoke Detected", Value = 100 });
                                        break;
                                    case "FireTest":
                                        ret.Add(new TextValueResponse<int> { Text = "Test Smoke Detected", Value = 101 });
                                        break;
                                    case "FireOffline":
                                        ret.Add(new TextValueResponse<int> { Text = "Smoke Detector Offline", Value = 102 });
                                        break;
                                    case "FireOnline":
                                        ret.Add(new TextValueResponse<int> { Text = "Smoke Detector Online", Value = 103 });
                                        break;
                                    case "FireOfflineIncident":
                                        ret.Add(new TextValueResponse<int> { Text = "Offline Incident Alert", Value = 104 });
                                        break;
                                    case "DeviceReplacedAlert":
                                        ret.Add(new TextValueResponse<int> { Text = "Device name replaced", Value = 105 });
                                        break;


                                }
                            }
                            break;

                    }

                }
                ret.RemoveAll(x => x.Text.Contains("Last7daysCloudStorageForAlerts"));
                ret.RemoveAll(x => x.Text.Contains("Helmet"));
                ret.RemoveAll(x => x.Text.Contains("No Mask"));
                ret.RemoveAll(x => x.Text.Contains("Mask"));

                if(devices != "wsd")
                {
                    ret.RemoveAll(x => x.Text.Contains("DeviceReplacedAlert"));
                }

                var res = ret.GroupBy(r => r.Text)
                             .Select(grp => grp.First())
                             .ToList();

                return Ok(new MMSHttpReponse<List<TextValueResponse<int>>> { ResponseBody = res, SuccessMessage = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while getting alerts {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);

            }
        }

        /// <summary>
        /// Deletes the alert for an id
        /// </summary>
        /// <param name="alertId"></param>
        /// <returns></returns>
        [HttpDelete("Delete/{alertId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(string alertId)
        {
            try
            {
                if (!IsOrgAdmin(CurrentApplications))
                {
                    var deviceId = _alertRepo.GetDeviceIdOfAlert(alertId, OrgId);
                    var permittedDevices = _userDevicePermissionChanges.GetDeviceIdForViewer(UserEmail);
                    if (!permittedDevices.Contains(deviceId.Result.ToString()))
                    {
                        return Forbid();
                    }
                }
                await _alertRepo.Delete(alertId, OrgId);
                return Ok(new MMSHttpReponse { SuccessMessage = "Alert deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while deleting alerts {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Performs the requested action for the alert ids . for deletion Action == "Delete" 
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("ProccessSelectedAlerts")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProccessSelectedAlerts([FromBody] ProccessSelectedAlertsRequest reqBody)
        {
            try
            {
                string actionPerformed = "";
                if (!IsOrgAdmin(CurrentApplications))
                {
                    if(reqBody.AlertIds.Count() > 0)
                    {
                        var deviceId = _alertRepo.GetDeviceIdOfAlert(reqBody.AlertIds.FirstOrDefault(), OrgId);
                        var permittedDevices = _userDevicePermissionChanges.GetDeviceIdForViewer(UserEmail);
                        if (!permittedDevices.Contains(deviceId.Result.ToString()))
                        {
                            return Forbid();
                        }
                        else
                        {
                           if(reqBody.Action == "Delete")
                            {
                                foreach (var a in reqBody.AlertIds)
                                {
                                    await _alertRepo.Delete(a, OrgId);
                                }
                                actionPerformed = "deleted";
                            }
                           else
                            {
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "No Action Specified" });
                            }
                        }
                    }
                    else
                    {
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "No Alert Selected" });
                    }
                   
                }
                else
                {
                    if(reqBody.AlertIds.Count() > 0)
                    {
                        if (reqBody.Action == "Delete")
                        {
                            foreach (var a in reqBody.AlertIds)
                            {
                                await _alertRepo.Delete(a, OrgId);
                            }
                            actionPerformed = "deleted";
                        }
                        else
                        {
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "No Action Specified" });
                        }
                    }
                    else
                    {
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "No Alert Selected" });
                    }
                }
                return Ok(new MMSHttpReponse { SuccessMessage = $"Alerts {actionPerformed} successfully" });
            }
            catch (NullReferenceException ex)
            {
                _logger.LogError("Exception occured while deleting alerts {ex}", ex);
                return BadRequest(new MMSHttpReponse { ErrorMessage = "alert passed does not " });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured while deleting alerts {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            
        }

        private List<AlertResponse> MapDTO(List<Alert> alerts, string sas)
        {
            int OfflineIncidentDiffTime = Convert.ToInt32(_configuration.GetSection("OfflineIncidentDiffTime").Value);
            string baseBlobUrl = this._configuration.GetSection("blob").GetSection("alertblob").Value;
            return alerts.Select(x =>
            {
                string deviceType = null;
                if(string.IsNullOrEmpty(x.DeviceType) || x.DeviceType == "TEV")
                {
                    deviceType = "TEV";
                }
                else
                {
                    deviceType = x.DeviceType;
                }

                // Make the imageUrl
                string imageUrl = "";
                string videoUrl = null;
                switch (deviceType)
                {
                    case nameof(Applications.TEV):
                        switch ((AlertType)x.AlertType)
                        {
                            case AlertType.SmartAICameraOffline:
                                imageUrl = baseBlobUrl + $"tev-offline.png?{sas}";
                                break;
                            case AlertType.SmartAICameraOnline:
                                imageUrl = baseBlobUrl + $"tev-online.png?{sas}";
                                break;
                            default:
                                imageUrl = baseBlobUrl + $"{x.Id}.jpg?{sas}";
                                videoUrl = baseBlobUrl + $"{x.Id}.mp4?{sas}";
                                break;
                        }
                        break;
                    case nameof(Applications.TEV2):
                        switch ((AlertType)x.AlertType)
                        {
                            case AlertType.SmartAICameraOffline:
                                imageUrl = baseBlobUrl + $"tev2-offline.png?{sas}";
                                break;
                            case AlertType.SmartAICameraOnline:
                                imageUrl = baseBlobUrl + $"tev2-online.png?{sas}";
                                break;
                            default:
                                imageUrl = baseBlobUrl + $"{x.Id}.jpg?{sas}";
                                videoUrl = baseBlobUrl + $"{x.Id}.mp4?{sas}";
                                break;
                        }
                        break;
                    case nameof(Applications.WSD):
                        switch ((AlertType)x.AlertType)
                        {
                            case AlertType.Fire:
                                imageUrl = baseBlobUrl + $"fire.gif?{sas}";
                                break;
                            case AlertType.FireOffline:
                                imageUrl = baseBlobUrl + $"fire-offline.png?{sas}";
                                break;
                            case AlertType.FireTest:
                                imageUrl = baseBlobUrl + $"fire-tested.png?{sas}";
                                break;
                            case AlertType.FireOnline:
                                imageUrl = baseBlobUrl + $"fire-online.png?{sas}";
                                break;
                            case AlertType.DeviceReplacedAlert:
                                imageUrl = baseBlobUrl + $"fire-replaced.png?{sas}";
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }

                return new AlertResponse
                {
                    AlertId = x.Id,
                    AlertType = ((AlertType)x.AlertType).GetDescription(),
                    OccurenceTimeStamp = x.OccurenceTimestamp,
                    DeviceName = x.DeviceName,
                    DeviceId = x.DeviceId,
                    LocationId = x.LocationId,
                    LocationName = x.LocationName,
                    Acknowledged = x.Acknowledged,
                    BookMarked = x.IsBookmarked,
                    ImageUrl = imageUrl,
                    Comment=x.Comment,
                    IsCorrect=x.IsCorrect,
                    VideoUrl = videoUrl,
                    SmokeValue=x.SmokeValue,
                    Device=deviceType,
                    AlertOccurred = x.EnqueuedTimestamp - x.OccurenceTimestamp > OfflineIncidentDiffTime ? _configuration.GetSection("AlertOccurredLabel").Value.ToString() : null,
                    AlertStatus = x.AlertStatus == "o" ? null : x.AlertStatus == "oo" ? "Offline" : x.AlertStatus == "ooo" ? "Offline (Indicative Time)" : null

                };
            }).ToList();
        }

        private List<Alert> OfflineIncidentFilter(List<Alert> alerts, List<int> alertType)
        {
            int OfflineIncidentDiffTime = Convert.ToInt32(_configuration.GetSection("OfflineIncidentDiffTime").Value);
            var alertList = new List<Alert>();

            if (alertType != null && alertType.Contains((int)AlertType.FireOfflineIncident))
            {
                alertList.AddRange(alerts.Where(x => x.EnqueuedTimestamp - x.OccurenceTimestamp > OfflineIncidentDiffTime).ToList());

                if (alertType.Contains((int)AlertType.FireOffline))
                {
                    alertList.AddRange(alerts.Where(x => x.AlertType == (int)AlertType.FireOffline).ToList());
                }
                if (alertType.Contains((int)AlertType.FireOnline))
                {
                    alertList.AddRange(alerts.Where(x => x.AlertType == (int)AlertType.FireOnline).ToList());
                }
                if (alertType.Contains((int)AlertType.Fire))
                {
                    alertList.AddRange(alerts.Where(x => x.AlertType == (int)AlertType.Fire).ToList());
                }
                if (alertType.Contains((int)AlertType.FireTest))
                {
                    alertList.AddRange(alerts.Where(x => x.AlertType == (int)AlertType.FireTest).ToList());
                }
                return alertList.OrderByDescending(x => x.EnqueuedTimestamp).ToList();
            }
            else
            {
                alertList = alerts;
            }
            return alertList.OrderByDescending(x => x.OccurenceTimestamp).ToList();
        }

        

    }
}
