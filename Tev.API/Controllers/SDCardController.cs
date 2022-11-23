using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.DAL.RepoContract;
using Tev.DAL.Entities;
using Tev.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tev.Cosmos.IRepository;
using Tev.IotHub;
using Microsoft.Azure.Devices;
using Tev.Cosmos;
using Microsoft.Azure.Devices.Client.Exceptions;
using System.Globalization;
using Newtonsoft.Json;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class SDCardController : TevControllerBase
    {
        // private readonly RecordingController _recordingController;
        private readonly ILogger<SDCardController> _logger;
        private readonly IDeviceRepo _deviceRepo;
        private readonly IAlertRepo _alertRepo;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly ITevIoTRegistry _iotHub;

        public SDCardController(IAlertRepo alertRepo, ITevIoTRegistry iotHub, ILogger<SDCardController> logger, IDeviceRepo deviceRepo, IGenericRepo<UserDevicePermission> userDevicePermissionRepo)
        {
            _iotHub = iotHub;
            _logger = logger;
            _deviceRepo = deviceRepo;
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _alertRepo = alertRepo;
            //_recordingController = recordingController;
        }

        /// <summary>
        /// Format the SD card
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="formatType"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("FormatSD_Card")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<CardResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FormatSD_Card(string device_id, int formatType, DateTime startDate, DateTime endDate)
        {
            CardResponse cardObj = new CardResponse();
            string sDate = "";
            string eDate = "";
            CloudToDeviceMethodResult cloudToDeviceMethodResult = new CloudToDeviceMethodResult();
            if (!string.IsNullOrEmpty(device_id))
            {
                var data = await _deviceRepo.GetDevice(device_id, OrgId);
                if (data == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                if (!data.SdCardAvilable)
                {
                    cardObj.Status = false;
                    cardObj.Message = "SD Card is not plugged-In";
                    return Ok(new MMSHttpReponse<CardResponse> { ResponseBody = cardObj });
                }
                if (formatType == 0 || formatType == 1)
                {
                    if (formatType == 1 && startDate != null && endDate != null)
                    {
                        if (startDate >= endDate)
                        {
                            cardObj.Status = false;
                            cardObj.Message = "Start date and time should be less than end date and time";
                            return Ok(new MMSHttpReponse<CardResponse> { ResponseBody = cardObj });
                        }
                        if (startDate >= DateTime.UtcNow.Date)
                        {
                            cardObj.Status = false;
                            cardObj.Message = "Start date and time should be less than current date and time";
                            return Ok(new MMSHttpReponse<CardResponse> { ResponseBody = cardObj });
                        }
                        if (endDate >= DateTime.UtcNow.Date)
                        {
                            cardObj.Status = false;
                            cardObj.Message = "End date should be less than current date";
                            return Ok(new MMSHttpReponse<CardResponse> { ResponseBody = cardObj });
                        }
                        sDate = startDate.ToString("yyyy-MM-ddTHH:mm:") + "00";
                        eDate = endDate.ToString("yyyy-MM-ddTHH:mm:") + "00";
                    }
                    try
                    {
                        //Payload
                        var jsonMethodParam = new
                        {
                            formatType = formatType,
                            startdateTstartTime = sDate,
                            enddateTendtime = eDate,
                        };
                        var res = await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "format_sdcard", 10, _logger, JsonConvert.SerializeObject(jsonMethodParam));
                        if (res.Status == 200)
                        {
                            if (formatType == 0)
                            {
                                await ClearRecordSettings(device_id);
                            }
                            cardObj.Status = true;
                            cardObj.Message = "SD card formatted successfully";
                        }
                    }
                    catch (DeviceNotFoundException ex)
                    {
                        _logger.LogError($" Device not found for deviceid {device_id} Exception :- {ex}");
                        cardObj.Status = false;
                        cardObj.Message = $" Device not found for deviceid {device_id}";
                    }
                    catch (TimeoutException ex)
                    {
                        _logger.LogError($" Request time out for deviceid {device_id} Exception :-  {ex}");
                        cardObj.Status = false;
                        cardObj.Message = $" Request time out {ex}";
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains(":404103,"))
                        {
                            data.Online = false;
                            await _deviceRepo.UpdateDevice(OrgId, data);
                            _logger.LogError($"Device is not online for deviceid {device_id} Exception :- {ex}");
                            cardObj.Status = false;
                            cardObj.Message = $"Device is not online for deviceid {device_id}";
                        }
                        if (ex.Message.Contains(":504101,"))
                        {
                            _logger.LogError($"Request time out for deviceid {device_id} Exception :- {ex}");
                            cardObj.Status = false;
                            cardObj.Message = $"Request time out {ex}";
                        }
                        if (ex.Message.Contains(":404001,"))
                        {
                            _logger.LogError($"Device not found for deviceid {device_id} Exception :- {ex}");
                            cardObj.Status = false;
                            cardObj.Message = $"Device not found for deviceid {device_id}";
                        }

                        _logger.LogError($"Exception in DeviceValidationCheck for deviceid {device_id} Exception :- {ex}");
                        cardObj.Status = false;
                        cardObj.Message = $"Exception in DeviceValidationCheck for deviceid {device_id}";
                    }
                  
                }
                else
                {
                    cardObj.Status = false;
                    cardObj.Message = "Format type must be 0(fully format) or 1(range format)";
                }
            }
            else
            {
                cardObj.Status = false;
                cardObj.Message = "Invalid deviceId";
            }
            return Ok(new MMSHttpReponse<CardResponse> { ResponseBody = cardObj });

        }

        /// <summary>
        /// SD Card Details
        /// </summary>
        /// <param name="device_id"></param>
        /// <returns></returns>
        [HttpGet("SD_Card_Detail")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<CardResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SD_Card_Detail(string device_id)
        {
            try
            {
                CardResponse cardObj = new CardResponse();

                if (!string.IsNullOrEmpty(device_id))
                {
                    var data = await _deviceRepo.GetDevice(device_id, OrgId);
                    if (data == null)
                    {
                        return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                    }

                    if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                    {
                        return Forbid();
                    }
                    string resolution = "480";
                    if (data.FeatureConfig != null && data.FeatureConfig.VideoResolution != null)
                    {
                        resolution = data.recordSetting == null ? "480" : data.recordSetting.eventRecording ? "1080" : data.FeatureConfig.VideoResolution.sdCard == null ? "480" : data.FeatureConfig.VideoResolution.sdCard;
                    }
                    string cardStatus = "Plugged-In";
                    if (!data.SdCardAvilable)
                    {
                        cardStatus = "Ejected";
                    }
                    else if (data.SdCardCorrupted)
                    {
                        cardStatus = "Corrupted";
                    }
                    cardObj.Status = true;
                    cardObj.Message = "";
                    int recordingType = data.recordSetting != null ? data.recordSetting.scheduleRecording == true ? 1 : data.recordSetting.manualRecording == "start" ? 2 : data.recordSetting.offlineRecording == true ? 3 : data.recordSetting.eventRecording == true ? 4 : 0 : 0;

                    cardObj.data = new data
                    {
                        record_status = data.SdCardRecordingStatus == null ? "Recording Stop" : data.SdCardRecordingStatus,
                        card_status = cardStatus,
                        total_space = cardStatus == "Plugged-In" ? Math.Round(Convert.ToDouble(data.SdCardTotalSpace) / (1024 * 1024 * 1024), 2).ToString() + "GB" : "0.00GB",
                        space_left = cardStatus == "Plugged-In" ? Math.Round(Convert.ToDouble(data.SdCardSpaceLeft) / (1024 * 1024 * 1024), 2).ToString() + "GB" : "0.00GB",
                        fifo = data.recordSetting != null ? data.recordSetting.fifo : false,
                        resolution = resolution,
                        eventRecording = data.recordSetting != null ? data.recordSetting.eventRecording : false,
                        EnabledRecordingType = recordingType,
                        encryption=data.Encryption,
                        
                    };
                }
                else
                {
                    cardObj.Status = false;
                    cardObj.Message = "Invalid deviceId";
                }

                return Ok(new MMSHttpReponse<CardResponse> { ResponseBody = cardObj });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while retrieving and formatting SDCard", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Clear Record Settings
        /// </summary>
        /// <param name="device_id"></param>
        /// <returns></returns>
        [HttpGet("ClearRecordSettings")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<string>>), StatusCodes.Status200OK)]
        public async Task<string> ClearRecordSettings(string device_id)
        {
            try
            {
                var date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Split(" ");
                RecordSettingsResponse recordSettingsResponse = new RecordSettingsResponse();
                var data = await _deviceRepo.GetDevice(device_id, OrgId);

                if (data.recordSetting != null)
                {

                    if (data.recHistories != null)
                    {
                        var d = DateTime.UtcNow;
                        var advSch = data.recHistories.Where(x => Convert.ToDateTime(x.date + " " + x.starttime) >= DateTime.UtcNow).ToList();
                        if (advSch.Count > 0)
                        {
                            foreach (var item in advSch)
                            {
                                data.recHistories.Remove(item);
                            }

                        }
                        data.recHistories[data.recHistories.Count - 1].endtime = date[1];
                    }

                    data.recordSetting.scheduleRecording = false;
                    data.recordSetting.eventRecording = false;
                    data.recordSetting.offlineRecording = false;
                    data.recordSetting.manualRecording = "stop";
                    data.SdCardRecordingStatus = "Recording Stop";
                    data.recordSetting.loiter = false;
                    data.recordSetting.trespassing = false;
                    data.recordSetting.crowd = false;
                    //data.recordSetting.fifo = false;
                    if (data.recordSchedules != null)
                    {
                        data.recordSchedules = null;
                    }
                    try
                    {
                        await _iotHub.UpdateSDCardRecordSettings(device_id, "clearCameraSettings", false, false, false, false, false, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception in Configuring Iot for deviceid {device_id} Exception :- {ex}");
                        return ex.Message;
                    }
                    await _deviceRepo.UpdateDevice(OrgId, data);
                }
                return "Record settings are cleared successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to clear record settings", ex);
                return ex.Message;
            }
        }
    }
}
