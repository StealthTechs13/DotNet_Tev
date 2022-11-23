using Azure.Storage.Queues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Enums;
using Tev.API.Models;
using Tev.Cosmos;
using Tev.Cosmos.Entity;
using Tev.Cosmos.IRepository;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;
using time = Tev.API.Models.time;



namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class RecordingController : TevControllerBase
    {
        private readonly IGenericRepo<SDCardHistory> _cardHistoryRepo;
        private readonly ILogger<RecordingController> _logger;
        private readonly IGenericRepo<Location> _locationRepo;
        private readonly IDeviceRepo _deviceRepo;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly ITevIoTRegistry _iotHub;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public RecordingController(ITevIoTRegistry iotHub, ILogger<RecordingController> logger, IGenericRepo<Location> locationRepo, IDeviceRepo deviceRepo, IGenericRepo<UserDevicePermission> userDevicePermissionRepo, IGenericRepo<SDCardHistory> cardHistoryRepo, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _iotHub = iotHub;
            _logger = logger;
            _deviceRepo = deviceRepo;
            _locationRepo = locationRepo;
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _unitOfWork = unitOfWork;
            _cardHistoryRepo = cardHistoryRepo;
            _configuration = configuration;
        }

        /// <summary>
        /// Schedule Recording 
        /// </summary>
        /// <param name="recordingReq"></param>
        /// <returns></returns>
        [HttpPost("ScheduleRecording")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordingResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ScheduleRecording(RecordingRequest recordingReq)
        {
            try
            {
                RecordingResponse recordingObj = new RecordingResponse();
                int checkSch = 0;
                if (!string.IsNullOrEmpty(recordingReq.device_id))
                {
                    var data = await _deviceRepo.GetDevice(recordingReq.device_id, OrgId);
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
                        recordingObj.Status = false;
                        recordingObj.Message = "SD Card is not plugged-In";
                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                    }
                    if (recordingReq.enable)
                    {
                        if (data.recordSetting != null)
                        {
                            if (!data.recordSetting.fifo)
                            {
                                if (data.SdCardSpaceLeft != null)
                                {
                                    if (Math.Round(Convert.ToDouble(data.SdCardSpaceLeft)) == 0)
                                    {
                                        recordingObj.Status = false;
                                        recordingObj.Message = "Schedule recording enable is not possible: SD Card is full";
                                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                    }
                                }
                            }
                        }
                    }
                    if (recordingReq.scheduleObj.Count > 0)
                    {
                        List<IotHub.Models.Schedule> iot_Schedulelist = new List<IotHub.Models.Schedule>(7);
                        for (int k = 0; k < 7; k++)
                            iot_Schedulelist.Add(new IotHub.Models.Schedule());

                        List<RecordSchedules> list = new List<RecordSchedules>();
                        List<RecHistory> recHistories = new List<RecHistory>();
                        for (int i = 0; i < recordingReq.scheduleObj.Count; i++)
                        {

                            DateTime date = DateTime.UtcNow;
                            string day = date.DayOfWeek.ToString();
                            Console.Write(date.DayOfWeek);
                            int m = day == "Monday" ? 0 : (day == "Tuesday" ? 1 : (day == "Wednesday" ? 2 : (day == "Thursday" ? 3 : (day == "Friday" ? 4 : (day == "Saturday" ? 5 : 6)))));
                            date = i < m ? date.AddDays(7 - m + i) : (m < i ? date.AddDays(i - m) : date);
                            List<IotHub.Models.time> iotScheduletimeLst = new List<IotHub.Models.time>();
                            List<Scheduletime> ScheduletimeLst = new List<Scheduletime>();

                            bool Isfullday = false;
                            if (recordingReq.scheduleObj[i].fullday)
                            {
                                Isfullday = true;
                                if (data.recordSchedules != null && data.recordSchedules.Count > 0)
                                {
                                    data.recordSchedules[i].time = null;
                                }
                                string stTime = DateTime.UtcNow.ToString("HH:mm:ss");
                                if (day != recordingReq.scheduleObj[i].day && data.recHistories != null)
                                {
                                    stTime = "00:00:00";
                                    var chkDayHis = data.recHistories.Where(x => x.date == date.ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[0]).ToList();
                                    if (chkDayHis.Count > 0)
                                    {
                                        foreach (var item in chkDayHis)
                                        {
                                            data.recHistories.Remove(item);
                                        }
                                    }

                                }
                                else if (data.recHistories != null)
                                {
                                    var chkDayHis = data.recHistories.Where(x => x.date == date.ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[0]).ToList();
                                    if (chkDayHis.Count > 0)
                                    {
                                        foreach (var item in chkDayHis)
                                        {
                                            if (Convert.ToDateTime(item.date + " " + item.endtime) > Convert.ToDateTime(item.date + " " + stTime) && Convert.ToDateTime(item.date + " " + item.starttime) > Convert.ToDateTime(item.date + " " + stTime))
                                            {
                                                data.recHistories.Remove(item);

                                            }
                                            else if (Convert.ToDateTime(item.date + " " + item.endtime) > Convert.ToDateTime(item.date + " " + stTime))
                                            {
                                                item.endtime = stTime;
                                            }
                                        }
                                    }
                                }
                                RecHistory recHistory = new RecHistory()
                                {
                                    recType = 1,
                                    date = date.ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[0],
                                    starttime = stTime,
                                    endtime = "23:59:59",
                                };
                                recHistories.Add(recHistory);
                            }
                            else
                            {
                                if (recordingReq.scheduleObj[i].time.Count == 0 || recordingReq.scheduleObj[i].time == null)
                                {
                                    checkSch++;
                                    if (checkSch == 7)
                                    {
                                        recordingObj.Status = false;
                                        recordingObj.Message = $"Recording should not be scheduled without selecting any schedules";
                                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                    }
                                }
                                if (recordingReq.scheduleObj[i].time.Count > 3)
                                {
                                    recordingObj.Status = false;
                                    recordingObj.Message = $"Limit exceeded for day:Maximum 3 Schedule/day {recordingReq.scheduleObj[i].day}.";
                                    return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                }
                                if (recordingReq.scheduleObj[i].time.Count > 0 && recordingReq.scheduleObj[i].time.Count <= 3)
                                {
                                    time time1 = new time();
                                    if (recordingReq.scheduleObj[i].time.Count > 1)
                                    {
                                        for (int k = 0; k < recordingReq.scheduleObj[i].time.Count; ++k)
                                        {
                                            for (int l = k + 1; l < recordingReq.scheduleObj[i].time.Count; ++l)
                                            {
                                                if (Convert.ToDateTime(recordingReq.scheduleObj[i].time[k].starttime) > Convert.ToDateTime(recordingReq.scheduleObj[i].time[l].starttime))
                                                {

                                                    time1 = recordingReq.scheduleObj[i].time[k];
                                                    recordingReq.scheduleObj[i].time[k] = recordingReq.scheduleObj[i].time[l];
                                                    recordingReq.scheduleObj[i].time[l] = time1;
                                                }
                                            }
                                        }
                                    }
                                    foreach (var item in recordingReq.scheduleObj[i].time)
                                    {
                                        if (recordingReq.scheduleObj[i].time.Count == 2)
                                        {
                                            if (Convert.ToDateTime(recordingReq.scheduleObj[i].time[1].starttime) < Convert.ToDateTime(recordingReq.scheduleObj[i].time[0].endtime))
                                            {
                                                recordingObj.Status = false;
                                                recordingObj.Message = $"Can not schedule multiple recording with in same time span for day:{recordingReq.scheduleObj[i].day}.";
                                                return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                            }
                                        }
                                        else if (recordingReq.scheduleObj[i].time.Count == 3)
                                        {
                                            if (Convert.ToDateTime(recordingReq.scheduleObj[i].time[1].starttime) < Convert.ToDateTime(recordingReq.scheduleObj[i].time[0].endtime) || Convert.ToDateTime(recordingReq.scheduleObj[i].time[2].starttime) < Convert.ToDateTime(recordingReq.scheduleObj[i].time[1].endtime))
                                            {
                                                recordingObj.Status = false;
                                                recordingObj.Message = $"Can not schedule multiple recording with in same time span for day:{recordingReq.scheduleObj[i].day}.";
                                                return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                            }
                                        }
                                        if (Convert.ToDateTime(item.starttime) >= Convert.ToDateTime(item.endtime))
                                        {
                                            recordingObj.Status = false;
                                            recordingObj.Message = $"Can not schedule recording for day:{recordingReq.scheduleObj[i].day}. Start time must be less than end time. ";
                                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                        }

                                        Scheduletime scheduletime = new Scheduletime()
                                        {
                                            starttime = item.starttime,
                                            endtime = item.endtime
                                        };
                                        ScheduletimeLst.Add(scheduletime);
                                        RecHistory recHistory = new RecHistory()
                                        {
                                            recType = 1,
                                            date = date.ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[0],
                                            starttime = Convert.ToDateTime(item.starttime).ToString("HH:mm:ss"),
                                            endtime = Convert.ToDateTime(item.endtime).ToString("HH:mm:ss"),
                                        };
                                        recHistories.Add(recHistory);

                                        //for iothub
                                        IotHub.Models.time iotscheduletime = new IotHub.Models.time()
                                        {
                                            st = Convert.ToDateTime(item.starttime).ToString("HH:mm"),
                                            et = Convert.ToDateTime(item.endtime).ToString("HH:mm")
                                        };
                                        iotScheduletimeLst.Add(iotscheduletime);
                                    }
                                }
                            }
                            RecordSchedules recordSchedules = new RecordSchedules()
                            {
                                day = recordingReq.scheduleObj[i].day,
                                fullday = Isfullday,
                                time = ScheduletimeLst
                            };
                            list.Add(recordSchedules);
                            //for iothub
                            bool en = false;
                            if (Isfullday)
                            {
                                en = true;
                            }
                            else
                            {
                                if (iotScheduletimeLst.Count > 0 && iotScheduletimeLst != null)
                                {
                                    en = true;
                                }
                            }
                            IotHub.Models.Schedule iotrecordSchedules = new IotHub.Models.Schedule()
                            {
                                enabled = en,
                                fullday = Isfullday,
                                times = iotScheduletimeLst
                            };


                            int j = recordingReq.scheduleObj[i].day == "Monday" ? 0 : recordingReq.scheduleObj[i].day == "Tuesday" ? 1 : recordingReq.scheduleObj[i].day == "Wednesday" ? 2 : recordingReq.scheduleObj[i].day == "Thursday" ? 3 : recordingReq.scheduleObj[i].day == "Friday" ? 4 : recordingReq.scheduleObj[i].day == "Saturday" ? 5 : recordingReq.scheduleObj[i].day == "Sunday" ? 6 : -1;

                            if (j > -1)
                                iot_Schedulelist[j] = iotrecordSchedules;
                        }
                        if (list != null && iot_Schedulelist != null)
                        {
                            if (recHistories.Count > 0)
                            {
                                if (data.recHistories != null)
                                {
                                    foreach (var item in recHistories)
                                    {
                                        var chkRecHistory = data.recHistories.Where(x => x.date == item.date && Convert.ToDateTime(x.date + " " + x.endtime) > Convert.ToDateTime(item.date + " " + item.starttime)).FirstOrDefault();
                                        if (chkRecHistory != null)
                                        {
                                            chkRecHistory.endtime = item.starttime;
                                        }
                                        data.recHistories.Add(item);
                                    }
                                }
                                else
                                {
                                    List<RecHistory> recHisList = new List<RecHistory>();
                                    recHisList.AddRange(recHistories);
                                    data.recHistories = recHisList;
                                }
                            }
                        }
                        data.recordSchedules = list;
                        if (data.recordSetting != null)
                        {
                            data.recordSetting.scheduleRecording = recordingReq.enable;
                            data.recordSetting.eventRecording = false;
                            data.recordSetting.offlineRecording = false;
                            data.recordSetting.manualRecording = "stop";

                        }
                        else
                        {
                            RecordSetting recSetting = new RecordSetting()
                            {
                                scheduleRecording = recordingReq.enable,
                                eventRecording = false,
                                offlineRecording = false,
                                manualRecording = "stop",
                            };
                            data.recordSetting = recSetting;
                        }
                        try
                        {
                            await _iotHub.UpdateSDCardRecordSettings(recordingReq.device_id, "schedule", false, false, false, false, false, _logger, null, iot_Schedulelist);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Exception in Configuring Iot for deviceid {recordingReq.device_id} Exception :- {ex}");
                            recordingObj.Status = false;
                            recordingObj.Message = $"Exception in Configuring Iot for deviceid {recordingReq.device_id}";
                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                        }
                        await _deviceRepo.UpdateDevice(OrgId, data);
                        recordingObj.Status = true;
                        recordingObj.Message = "Recording Scheduled";
                    }
                    else
                    {
                        recordingObj.Status = false;
                        recordingObj.Message = "Please send scheduling data";
                    }
                }
                else
                {
                    recordingObj.Status = false;
                    recordingObj.Message = "Invalid deviceId";
                }
                return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while scheduling SD card recording", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// To Start/Stop the manual, offline and event recordings
        /// </summary>
        /// <param name="recordingReq"></param>
        /// <returns></returns>
        [HttpPost("Rec_Settings")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordingResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Rec_Settings(RecordingRequest recordingReq)
        {
            try
            {
                RecordingResponse recordingObj = new RecordingResponse();
                RecordSetting recordSetting = new RecordSetting();
                var date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Split(" ");
                List<RecHistory> recLst = new List<RecHistory>();
                if (!string.IsNullOrEmpty(recordingReq.device_id))
                {
                    var data = await _deviceRepo.GetDevice(recordingReq.device_id, OrgId);
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
                        recordingObj.Status = false;
                        recordingObj.Message = "SD Card is not plugged-In";
                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                    }

                    bool chkSech = data.recordSetting != null ? data.recordSetting.scheduleRecording : false;
                    switch (recordingReq.settingType)
                    {
                        case "event":
                            if (recordingReq.enable)
                            {
                                if (data.recordSetting != null && !data.recordSetting.fifo)
                                {
                                    if (data.SdCardSpaceLeft != null)
                                    {
                                        if (Math.Round(Convert.ToDouble(data.SdCardSpaceLeft)) == 0)
                                        {
                                            recordingObj.Status = false;
                                            recordingObj.Message = "Event recording enable is not possible: SD Card is full";
                                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.SdCardSpaceLeft != null)
                                    {
                                        if (Math.Round(Convert.ToDouble(data.SdCardSpaceLeft)) == 0)
                                        {
                                            recordingObj.Status = false;
                                            recordingObj.Message = "Schedule recording enable is not possible: SD Card is full";
                                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                        }
                                    }
                                }
                            }
                            if (data.Subscription != null)
                            {
                                var availFeatures = data.Subscription?.AvailableFeatures?.Select(x => Helper.Replace_withWhiteSpaceAndMakeTheStringIndexValueUpperCase(Convert.ToString((AlertType)x))).ToList();

                                if (recordingReq.eventType.Loiter)
                                {
                                    if (!availFeatures.Contains("Loiter"))
                                    {
                                        recordingObj.Status = false;
                                        recordingObj.Message = "No subscription for loiter";
                                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                    }
                                }
                                if (recordingReq.eventType.Trespassing)
                                {
                                    if (!availFeatures.Contains("Trespassing"))
                                    {
                                        recordingObj.Status = false;
                                        recordingObj.Message = "No subscription for Trespassing";
                                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                    }
                                }
                                if (recordingReq.eventType.Crowd)
                                {
                                    if (!availFeatures.Contains("Crowd"))
                                    {
                                        recordingObj.Status = false;
                                        recordingObj.Message = "No subscription for Crowd";
                                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                    }
                                }
                                if (recordingReq.eventType != null)
                                {
                                    if (data.recordSetting == null)
                                    {
                                        recordSetting.eventRecording = recordingReq.enable;
                                        recordSetting.loiter = recordingReq.eventType.Loiter;
                                        recordSetting.crowd = recordingReq.eventType.Crowd;
                                        recordSetting.trespassing = recordingReq.eventType.Trespassing;

                                        if (recordingReq.enable)
                                        {
                                            data.SdCardRecordingStatus = "Recording";
                                            recordSetting.scheduleRecording = false;
                                            data.recordSchedules = null;
                                            recordSetting.offlineRecording = false;
                                            recordSetting.manualRecording = "stop";
                                        }
                                        else
                                        {
                                            data.SdCardRecordingStatus = "Recording Stop";
                                        }
                                        data.recordSetting = recordSetting;
                                    }
                                    else
                                    {
                                        data.recordSetting.eventRecording = recordingReq.enable;
                                        data.recordSetting.trespassing = recordingReq.eventType.Trespassing;
                                        data.recordSetting.crowd = recordingReq.eventType.Crowd;
                                        data.recordSetting.loiter = recordingReq.eventType.Loiter;
                                        if (recordingReq.enable)
                                        {
                                            data.SdCardRecordingStatus = "Recording";
                                            data.recordSetting.scheduleRecording = false;
                                            data.recordSchedules = null;
                                            data.recordSetting.offlineRecording = false;
                                            data.recordSetting.manualRecording = "stop";
                                        }
                                        else
                                        {
                                            data.SdCardRecordingStatus = "Recording Stop";
                                        }
                                    }

                                    if (data.recHistories != null)
                                    {
                                        var advSch = data.recHistories.Where(x => Convert.ToDateTime(x.date + " " + x.starttime) >= DateTime.UtcNow).ToList();
                                        if (advSch.Count > 0)
                                        {
                                            foreach (var item in advSch)
                                            {
                                                data.recHistories.Remove(item);
                                            }
                                            if (Convert.ToDateTime(data.recHistories[data.recHistories.Count - 1].date + " " + data.recHistories[data.recHistories.Count - 1].endtime) >= DateTime.UtcNow)
                                            {
                                                data.recHistories[data.recHistories.Count - 1].endtime = date[1];
                                                data.recHistories[data.recHistories.Count - 1].edate = date[0];
                                            }
                                        }
                                        if (!chkSech)
                                        {
                                            var lastRec = data.recHistories.Where(x => x.endtime == "").FirstOrDefault();
                                            if (lastRec != null)
                                            {
                                                lastRec.endtime = date[1];
                                                lastRec.edate = date[0];
                                            }
                                        }
                                        if (recordingReq.enable)
                                        {
                                            RecHistory recHistory = new RecHistory()
                                            {
                                                recType = 2,
                                                date = date[0],
                                                starttime = date[1],
                                                endtime = "",
                                            };
                                            data.recHistories.Add(recHistory);
                                        }
                                    }
                                    else
                                    {
                                        RecHistory recHistory = new RecHistory()
                                        {
                                            recType = 2,
                                            date = date[0],
                                            starttime = date[1],
                                            endtime = "",
                                        };
                                        recLst.Add(recHistory);
                                        data.recHistories = recLst;
                                    }
                                    try
                                    {
                                        await _iotHub.UpdateSDCardRecordSettings(recordingReq.device_id, "event", data.recordSetting.eventRecording, data.recordSetting.trespassing, data.recordSetting.crowd, data.recordSetting.loiter, false, _logger);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError($"Exception in Configuring Iot for deviceid {recordingReq.device_id} Exception :- {ex}");
                                        recordingObj.Status = false;
                                        recordingObj.Message = $"Exception in Configuring Iot for deviceid {recordingReq.device_id}";
                                        return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                    }
                                    await _deviceRepo.UpdateDevice(OrgId, data);


                                    recordingObj.Status = true;
                                    recordingObj.Message = recordingReq.enable ? "Event recording enabled" : "Event recording disabled";
                                }
                                else
                                {
                                    recordingObj.Status = false;
                                    recordingObj.Message = "Please send EventType data";
                                }
                            }
                            else
                            {
                                recordingObj.Status = false;
                                recordingObj.Message = "You have no subscription for event Recording";
                                return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                            }

                            break;
                        case "offline":
                            if (recordingReq.enable)
                            {
                                if (data.recordSetting != null && !data.recordSetting.fifo)
                                {
                                    if (data.SdCardSpaceLeft != null)
                                    {
                                        if (Math.Round(Convert.ToDouble(data.SdCardSpaceLeft)) == 0)
                                        {
                                            recordingObj.Status = false;
                                            recordingObj.Message = "Offline recording enable is not possible: SD Card is full";
                                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.SdCardSpaceLeft != null)
                                    {
                                        if (Math.Round(Convert.ToDouble(data.SdCardSpaceLeft)) == 0)
                                        {
                                            recordingObj.Status = false;
                                            recordingObj.Message = "Offline recording enable is not possible: SD Card is full";
                                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                        }
                                    }
                                }
                            }
                            if (data.recordSetting == null)
                            {
                                recordSetting.offlineRecording = recordingReq.enable;

                                if (recordingReq.enable)
                                {
                                    data.SdCardRecordingStatus = "Recording";
                                    recordSetting.eventRecording = false;
                                    recordSetting.scheduleRecording = false;
                                    data.recordSchedules = null;
                                    recordSetting.manualRecording = "stop";
                                }
                                else
                                {
                                    data.SdCardRecordingStatus = "Recording Stop";
                                }

                                data.recordSetting = recordSetting;
                            }
                            else
                            {
                                data.recordSetting.offlineRecording = recordingReq.enable;
                                if (recordingReq.enable)
                                {
                                    data.SdCardRecordingStatus = "Recording";
                                    data.recordSetting.eventRecording = false;
                                    data.recordSetting.scheduleRecording = false;
                                    data.recordSchedules = null;
                                    data.recordSetting.manualRecording = "stop";
                                }
                                else
                                {
                                    data.SdCardRecordingStatus = "Recording Stop";
                                }

                            }
                            if (data.recHistories != null)
                            {
                                var advSch = data.recHistories.Where(x => Convert.ToDateTime(x.date + " " + x.starttime) >= DateTime.UtcNow).ToList();
                                if (advSch.Count > 0)
                                {
                                    foreach (var item in advSch)
                                    {
                                        data.recHistories.Remove(item);
                                    }
                                    if (Convert.ToDateTime(data.recHistories[data.recHistories.Count - 1].date + " " + data.recHistories[data.recHistories.Count - 1].endtime) >= DateTime.UtcNow)
                                    {
                                        data.recHistories[data.recHistories.Count - 1].endtime = date[1];
                                        data.recHistories[data.recHistories.Count - 1].edate = date[0];
                                    }
                                }
                                if (!chkSech)
                                {
                                    var lastRec = data.recHistories.Where(x => x.endtime == "").FirstOrDefault();
                                    if (lastRec != null)
                                    {
                                        lastRec.endtime = date[1];
                                        lastRec.edate = date[0];
                                    }
                                }
                                if (recordingReq.enable)
                                {
                                    RecHistory recHistory = new RecHistory()
                                    {
                                        recType = 4,
                                        date = date[0],
                                        starttime = date[1],
                                        endtime = "",
                                    };
                                    data.recHistories.Add(recHistory);
                                }
                            }
                            else
                            {
                                RecHistory recHistory = new RecHistory()
                                {
                                    recType = 4,
                                    date = date[0],
                                    starttime = date[1],
                                    endtime = "",
                                };
                                recLst.Add(recHistory);
                                data.recHistories = recLst;
                            }
                            try
                            {
                                await _iotHub.UpdateSDCardRecordSettings(recordingReq.device_id, "offline", data.recordSetting.offlineRecording, false, false, false, false, _logger);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Exception in Configuring Iot for deviceid {recordingReq.device_id} Exception :- {ex}");
                                recordingObj.Status = false;
                                recordingObj.Message = $"Exception in Configuring Iot for deviceid {recordingReq.device_id}";
                                return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                            }
                            await _deviceRepo.UpdateDevice(OrgId, data);
                            recordingObj.Status = true;
                            recordingObj.Message = recordingReq.enable ? "Offline recording enabled" : "Offline recording disabled ";

                            break;
                        case "manual":
                            if (recordingReq.record == "start")
                            {
                                if (data.recordSetting != null && !data.recordSetting.fifo)
                                {
                                    if (data.SdCardSpaceLeft != null)
                                    {
                                        if (Math.Round(Convert.ToDouble(data.SdCardSpaceLeft)) == 0)
                                        {
                                            recordingObj.Status = false;
                                            recordingObj.Message = "Manual recording enable is not possible: SD Card is full";
                                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                        }
                                    }
                                }
                                else
                                {
                                    if (data.SdCardSpaceLeft != null)
                                    {
                                        if (Math.Round(Convert.ToDouble(data.SdCardSpaceLeft)) == 0)
                                        {
                                            recordingObj.Status = false;
                                            recordingObj.Message = "Manual recording enable is not possible: SD Card is full";
                                            return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                        }
                                    }
                                }
                            }
                            if (recordingReq.record == "start" || recordingReq.record == "stop")
                            {
                                if (data.recordSetting == null)
                                {
                                    recordSetting.manualRecording = recordingReq.record;
                                    if (recordingReq.record == "start")
                                    {
                                        data.SdCardRecordingStatus = "Recording";
                                        recordSetting.eventRecording = false;
                                        recordSetting.scheduleRecording = false;
                                        data.recordSchedules = null;
                                        recordSetting.offlineRecording = false;
                                    }
                                    else
                                    {
                                        data.SdCardRecordingStatus = "Recording Stop";
                                    }
                                    data.recordSetting = recordSetting;
                                }
                                else
                                {

                                    if (recordingReq.record == "start")
                                    {
                                        data.SdCardRecordingStatus = "Recording";
                                        data.recordSetting.eventRecording = false;
                                        data.recordSetting.scheduleRecording = false;
                                        data.recordSchedules = null;
                                        data.recordSetting.offlineRecording = false;
                                    }
                                    else
                                    {
                                        data.SdCardRecordingStatus = "Recording Stop";
                                    }

                                }

                                if (data.recHistories != null)
                                {
                                    var advSch = data.recHistories.Where(x => Convert.ToDateTime(x.date + " " + x.starttime) >= DateTime.UtcNow).ToList();
                                    if (advSch.Count > 0)
                                    {
                                        foreach (var item in advSch)
                                        {
                                            data.recHistories.Remove(item);
                                        }
                                        if (Convert.ToDateTime(data.recHistories[data.recHistories.Count - 1].date + " " + data.recHistories[data.recHistories.Count - 1].endtime) >= DateTime.UtcNow)
                                        {
                                            data.recHistories[data.recHistories.Count - 1].endtime = date[1];
                                            data.recHistories[data.recHistories.Count - 1].edate = date[0];
                                        }
                                    }
                                    if (recordingReq.record == "start")
                                    {
                                        if (data.recordSetting.manualRecording == "stop")
                                        {
                                            if (!chkSech)
                                            {
                                                var lastRec = data.recHistories.Where(x => x.endtime == "").FirstOrDefault();
                                                if (lastRec != null)
                                                {
                                                    lastRec.endtime = date[1];
                                                    lastRec.edate = date[0];
                                                }
                                            }
                                            RecHistory recHistory = new RecHistory()
                                            {
                                                recType = 3,
                                                date = date[0],
                                                starttime = date[1],
                                                endtime = "",
                                            };
                                            data.recHistories.Add(recHistory);
                                            data.recordSetting.manualRecording = recordingReq.record;
                                        }
                                    }
                                    else
                                    {
                                        data.recordSetting.manualRecording = recordingReq.record;
                                        if (!chkSech)
                                        {
                                            var lastRec = data.recHistories.Where(x => x.endtime == "").FirstOrDefault();
                                            if (lastRec != null)
                                            {
                                                lastRec.endtime = date[1];
                                                lastRec.edate = date[0];
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    data.recordSetting.manualRecording = recordingReq.record;
                                    RecHistory recHistory = new RecHistory()
                                    {
                                        recType = 3,
                                        date = date[0],
                                        starttime = date[1],
                                        endtime = "",
                                    };
                                    recLst.Add(recHistory);
                                    data.recHistories = recLst;
                                }

                                try
                                {
                                    await _iotHub.UpdateSDCardRecordSettings(recordingReq.device_id, "manual", false, false, false, false, false, _logger, data.recordSetting.manualRecording);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Exception in Configuring Iot for deviceid {recordingReq.device_id} Exception :- {ex}");
                                    recordingObj.Status = false;
                                    recordingObj.Message = $"Exception in Configuring Iot for deviceid {recordingReq.device_id}";
                                    return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
                                }
                                await _deviceRepo.UpdateDevice(OrgId, data);
                                recordingObj.Status = true;
                                recordingObj.Message = recordingReq.record == "start" ? "Manual recording started" : "Manual recording stopped";
                            }
                            else
                            {
                                recordingObj.Status = false;
                                recordingObj.Message = "Please send valid parameter to start or stop the manual recording";
                            }
                            break;
                    }
                }
                else
                {
                    recordingObj.Status = false;
                    recordingObj.Message = "Invalid deviceId";
                }

                return Ok(new MMSHttpReponse<RecordingResponse> { ResponseBody = recordingObj });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while scheduling SD card recording", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        ///  To Get the Record Video Resolutions
        /// </summary>
        /// <param name="device_id"></param>
        /// <returns></returns>
        [HttpGet("GetRecordVedioResolution")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordVedioResolutionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecordVedioResolution(string device_id)
        {
            try
            {
                RecordVedioResolutionResponse resolutionObj = new RecordVedioResolutionResponse();

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
                    resolutionObj.Status = true;
                    resolutionObj.Message = "Resolution Fetched";
                    resolutionObj.Resolution = data.FeatureConfig.VideoResolution.sdCard;
                }
                else
                {
                    resolutionObj.Status = false;
                    resolutionObj.Message = "Invalid deviceId";
                }
                return Ok(new MMSHttpReponse<RecordVedioResolutionResponse> { ResponseBody = resolutionObj });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Get Record Vedio Resolution", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// To Get the Livestream video resolutions
        /// </summary>
        /// <param name="device_id"></param>
        /// <returns></returns>
        [HttpGet("GetLiveStreamVedioResolution")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordVedioResolutionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLiveStreamVedioResolution(string device_id)
        {
            try
            {
                RecordVedioResolutionResponse resolutionObj = new RecordVedioResolutionResponse();

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
                    if (data.FeatureConfig.VideoResolution != null)
                    {
                        resolutionObj.Resolution = data.FeatureConfig.VideoResolution.livestream;
                        resolutionObj.Status = true;
                        resolutionObj.Message = "Resolution Fetched";
                    }
                    else
                    {
                        resolutionObj.Status = false;
                        resolutionObj.Message = "Resolution not found";
                    }
                }
                else
                {
                    resolutionObj.Status = false;
                    resolutionObj.Message = "Invalid deviceId";
                }
                return Ok(new MMSHttpReponse<RecordVedioResolutionResponse> { ResponseBody = resolutionObj });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Get Live stream Vedio Resolution", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Set Record Vedio Resolution
        /// </summary>
        /// <param name="vedioResolutionReq"></param>
        /// <returns></returns>
        [HttpPost("SetRecordVedioResolution")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordVedioResolutionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetRecordVedioResolution(RecordVedioResolutionRequest vedioResolutionReq)
        {
            RecordVedioResolutionResponse resolutionObj = new RecordVedioResolutionResponse();

            if (!string.IsNullOrEmpty(vedioResolutionReq.device_id))
            {
                var data = await _deviceRepo.GetDevice(vedioResolutionReq.device_id, OrgId);
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
                    resolutionObj.Status = false;
                    resolutionObj.Message = "SD Card is not plugged-In";
                    return Ok(new MMSHttpReponse<RecordVedioResolutionResponse> { ResponseBody = resolutionObj });
                }
                if (data.recordSetting != null)
                {
                    if (data.recordSetting.eventRecording)
                    {
                        resolutionObj.Status = false;
                        resolutionObj.Message = "Event recording is enabled , can't change resolution ";
                        return Ok(new MMSHttpReponse<RecordVedioResolutionResponse> { ResponseBody = resolutionObj });
                    }
                    if (data.FeatureConfig.VideoResolution == null)
                    {
                        VideoResolution videoResolution = new VideoResolution()
                        {
                            sdCard = vedioResolutionReq.Resolution,
                        };
                        data.FeatureConfig.VideoResolution = videoResolution;
                    }
                    else
                    {

                        data.FeatureConfig.VideoResolution.sdCard = vedioResolutionReq.Resolution;
                    }
                }
                else
                {
                    RecordSetting recordSetting = new RecordSetting();
                    data.recordSetting = recordSetting;
                    if (data.FeatureConfig.VideoResolution == null)
                    {
                        VideoResolution videoResolution = new VideoResolution()
                        {
                            sdCard = vedioResolutionReq.Resolution,
                        };
                        data.FeatureConfig.VideoResolution = videoResolution;
                    }
                    else
                    {
                        data.FeatureConfig.VideoResolution.sdCard = vedioResolutionReq.Resolution;
                    }
                }

                try
                {
                    await _iotHub.UpdateDeviceVideoResolution(vedioResolutionReq.device_id, "sdCard_stream_resolution", vedioResolutionReq.Resolution);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception in Configuring Iot for deviceid {vedioResolutionReq.device_id} Exception :- {ex}");
                    resolutionObj.Status = false;
                    resolutionObj.Message = $"Exception in DeviceValidationCheck for deviceid {vedioResolutionReq.device_id}";
                    return Ok(new MMSHttpReponse<RecordVedioResolutionResponse> { ResponseBody = resolutionObj });
                }

                await _deviceRepo.UpdateDevice(OrgId, data);
                resolutionObj.Status = true;
                resolutionObj.Message = "Video resolution is updated Successfully";
            }
            else
            {
                resolutionObj.Status = false;
                resolutionObj.Message = "Invalid deviceId";
            }

            return Ok(new MMSHttpReponse<RecordVedioResolutionResponse> { ResponseBody = resolutionObj });

        }

        /// <summary>
        /// Set Livestream video resolutions
        /// </summary>
        /// <param name="vedioResolutionReq"></param>
        /// <returns></returns>
        [HttpPost("SetLiveStreamVedioResolution")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordVedioResolutionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetLiveStreamVedioResolution(RecordVedioResolutionRequest vedioResolutionReq)
        {
            RecordVedioResolutionResponse resolutionObj = new RecordVedioResolutionResponse();
            try
            {
                if (!string.IsNullOrEmpty(vedioResolutionReq.device_id))
                {
                    var data = await _deviceRepo.GetDevice(vedioResolutionReq.device_id, OrgId);
                    if (data == null)
                    {
                        return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                    }

                    if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                    {
                        return Forbid();
                    }
                    if (!string.IsNullOrEmpty(vedioResolutionReq.Resolution))
                    {
                        if (data.FeatureConfig.VideoResolution == null)
                        {
                            VideoResolution videoResolution = new VideoResolution()
                            {
                                livestream = vedioResolutionReq.Resolution,
                            };
                            data.FeatureConfig.VideoResolution = videoResolution;
                        }
                        else
                        {

                            data.FeatureConfig.VideoResolution.livestream = vedioResolutionReq.Resolution;
                        }
                        try
                        {
                            var jsonMethodParam = new
                            {
                                resolution = vedioResolutionReq.Resolution,
                            };
                            await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "set_livestream_resolution", 10, _logger, JsonConvert.SerializeObject(jsonMethodParam));
                        }
                        catch (DeviceNotFoundException ex)
                        {
                            _logger.LogError("Device not found {ex}", ex);
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                        }
                        catch (TimeoutException ex)
                        {
                            _logger.LogError("Request time out {ex}", ex);
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains(":404103,"))
                            {
                                data.Online = false;
                                await _deviceRepo.UpdateDevice(OrgId, data);
                                _logger.LogError("Device is not online {ex}", ex);
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Device is not online or device does not have this feature" });
                            }
                            if (ex.Message.Contains(":504101,"))
                            {
                                _logger.LogError("Request time out {ex}", ex);
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                            }
                            _logger.LogError("Exception in DeviceValidationCheck {ex}", ex);
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                        await _deviceRepo.UpdateDevice(OrgId, data);
                       // await SendPushNotificationToDevices(_logger, data, vedioResolutionReq.Resolution);
                        resolutionObj.Status = true;
                        resolutionObj.Message = "Video resolution is updated Successfully";
                    }
                    else
                    {
                        resolutionObj.Status = false;
                        resolutionObj.Message = "Invalid Resolution";
                    }
                }
                else
                {
                    resolutionObj.Status = false;
                    resolutionObj.Message = "Invalid deviceId";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Live stream resolution updation failed", ex);
                resolutionObj.Status = false;
                resolutionObj.Message = ex.Message;
            }
            return Ok(new MMSHttpReponse<RecordVedioResolutionResponse> { ResponseBody = resolutionObj });

        }

        /// <summary>
        /// To Get the record settings
        /// </summary>
        /// <param name="device_id"></param>
        /// <returns></returns>
        [HttpGet("GetRecordSettings")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordSettingsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecordSettings(string device_id)
        {
            try
            {
                RecordSettingsResponse recordSettingsResponse = new RecordSettingsResponse();
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

                    if (data.recordSetting != null)
                    {

                        recordSettingsResponse.Status = true;
                        recordSettingsResponse.Message = "Record Settings fetched successfully";
                        EventType eventType = new EventType()
                        {
                            Loiter = data.recordSetting.loiter,
                            Crowd = data.recordSetting.crowd,
                            Trespassing = data.recordSetting.trespassing,
                        };
                        Event @event = new Event()
                        {
                            eventRecording = data.recordSetting.eventRecording,
                            eventType = eventType,
                        };
                        string Resolution = null;
                        if (data.FeatureConfig.VideoResolution != null)
                        {
                            Resolution = data.FeatureConfig.VideoResolution.sdCard == "string" ? "480" : data.FeatureConfig.VideoResolution.sdCard;
                        }
                        List<Schedule> recordSchedulesDatas = new List<Schedule>();
                        if (data.recordSchedules != null)
                        {
                            for (int i = 0; i < data.recordSchedules.Count; i++)
                            {
                                if (!data.recordSchedules[i].fullday)
                                {
                                    List<time> time = new List<time>();
                                    if (data.recordSchedules[i].time.Count > 0)
                                    {
                                        foreach (var item in data.recordSchedules[i].time)
                                        {
                                            time timeObj = new time()
                                            {
                                                starttime = item.starttime,
                                                endtime = item.endtime,
                                            };
                                            time.Add(timeObj);
                                        }
                                    }
                                    recordSchedulesDatas.Add(new Schedule()
                                    {
                                        fullday = data.recordSchedules[i].fullday,
                                        day = data.recordSchedules[i].day,
                                        time = time,
                                    });
                                }
                                else
                                {
                                    recordSchedulesDatas.Add(new Schedule()
                                    {
                                        fullday = data.recordSchedules[i].fullday,
                                        day = data.recordSchedules[i].day,
                                        time = null,
                                    });
                                }


                            }
                        }
                        recordSettingsResponse.data = new recordSettingsData()
                        {
                            Connected = data.Online,
                            Event = @event,
                            Offline = data.recordSetting != null ? data.recordSetting.offlineRecording : false,
                            Manual = data.recordSetting != null ? data.recordSetting.manualRecording == "start" ? "Start" : "Stop" : "Stop",
                            Resolution = Resolution,
                            Recording_in_Progress = data.SdCardRecordingStatus == null ? false : data.SdCardRecordingStatus == "Recording Stop" ? false : true,
                            recordSchedules = recordSchedulesDatas,
                            Fifo = data.recordSetting.fifo,
                            Schedule = data.recordSetting != null ? data.recordSetting.scheduleRecording : false,
                            EventRec = data.recordSetting != null ? data.recordSetting.eventRecording : false,
                        };
                    }
                    else
                    {
                        recordSettingsResponse.Status = false;
                        recordSettingsResponse.Message = "There is no settings are available for this device: " + device_id;
                    }
                }
                else
                {
                    recordSettingsResponse.Status = false;
                    recordSettingsResponse.Message = "Invalid deviceId";
                }

                return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Record Settings", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// To Copy the camera settings from one camera to multiple/one camera
        /// </summary>
        /// <param name="device_IdsList"></param>
        /// <returns></returns>
        [HttpPost("CopyCameraSettings")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<CopyCameraSettingResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CopyCameraSettings(Device_IdsList device_IdsList)
        {
            try
            {
                CopyCameraSettingResponse copyCameraSettingResponse = new CopyCameraSettingResponse();
                RecordSetting recordSetting = new RecordSetting();
                if (!string.IsNullOrEmpty(device_IdsList.getCameraId) && device_IdsList.setCameraIds.Count > 0)
                {
                    var data = await _deviceRepo.GetDevice(device_IdsList.getCameraId, OrgId);
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
                        copyCameraSettingResponse.Status = false;
                        copyCameraSettingResponse.Message = "Copy setting failed,Source device has no SD Card";
                        return Ok(new MMSHttpReponse<CopyCameraSettingResponse> { ResponseBody = copyCameraSettingResponse });
                    }
                    foreach (var item in device_IdsList.setCameraIds)
                    {
                        var deviceObj = await _deviceRepo.GetDevice(item.deviceId, OrgId);
                        if (deviceObj != null)
                        {
                            if (!deviceObj.SdCardAvilable)
                            {
                                copyCameraSettingResponse.Status = false;
                                copyCameraSettingResponse.Message = $"Copy setting failed,Device with {deviceObj.DeviceName} has no Sd card.";
                                return Ok(new MMSHttpReponse<CopyCameraSettingResponse> { ResponseBody = copyCameraSettingResponse });
                            }
                            if (data.SdCardSpaceLeft != null)
                            {
                                if (Math.Round(Convert.ToDouble(deviceObj.SdCardSpaceLeft)) == 0)
                                {
                                    copyCameraSettingResponse.Status = false;
                                    copyCameraSettingResponse.Message = $"Copy camera settings is not possible: SD Card is full for Device: {deviceObj.DeviceName}";
                                    return Ok(new MMSHttpReponse<CopyCameraSettingResponse> { ResponseBody = copyCameraSettingResponse });
                                }
                            }
                            bool chkSech = false;
                            if (deviceObj.recordSetting != null)
                            {
                                if (data.recordSetting.eventRecording)
                                {
                                    var availFeatures = deviceObj.Subscription?.AvailableFeatures?.Select(x => Helper.Replace_withWhiteSpaceAndMakeTheStringIndexValueUpperCase(Convert.ToString((AlertType)x))).ToList();

                                    if (availFeatures.Contains("Loiter"))
                                    {
                                        deviceObj.recordSetting.loiter =true;
                                        deviceObj.recordSetting.eventRecording = true;
                                    }
                                    if (availFeatures.Contains("Trespassing"))
                                    {
                                        deviceObj.recordSetting.crowd = true;
                                        deviceObj.recordSetting.eventRecording = true;
                                    }
                                    if (availFeatures.Contains("Crowd"))
                                    {
                                        deviceObj.recordSetting.trespassing =true;
                                        deviceObj.recordSetting.eventRecording = true;
                                    }
                                }
                                else
                                {
                                    deviceObj.recordSetting.eventRecording = false;
                                }
                                chkSech = data.recordSetting.scheduleRecording;
                                deviceObj.recordSetting.fifo = data.recordSetting != null ? data.recordSetting.fifo : false;
                                deviceObj.recordSetting.scheduleRecording = data.recordSetting != null ? data.recordSetting.scheduleRecording : false;
                                deviceObj.recordSetting.offlineRecording = data.recordSetting != null ? data.recordSetting.offlineRecording : false;
                                deviceObj.recordSetting.manualRecording = data.recordSetting != null ? data.recordSetting.manualRecording : "stop";
                                deviceObj.SdCardRecordingStatus = data.SdCardRecordingStatus;
                                if (data.recordSchedules != null)
                                {
                                    if (deviceObj.recordSchedules != null)
                                    {
                                        deviceObj.recordSchedules = data.recordSchedules;
                                    }
                                    else
                                    {
                                        List<RecordSchedules> recSchLst = new List<RecordSchedules>();
                                        recSchLst = data.recordSchedules;
                                        deviceObj.recordSchedules = recSchLst;
                                    }
                                }
                                else
                                {
                                    List<RecordSchedules> recSchLst = new List<RecordSchedules>();
                                    deviceObj.recordSchedules = recSchLst;
                                }

                                if (deviceObj.FeatureConfig != null && deviceObj.FeatureConfig.VideoResolution != null)
                                {
                                    if (data.FeatureConfig.VideoResolution != null)
                                    {
                                        deviceObj.FeatureConfig.VideoResolution.sdCard = data.FeatureConfig.VideoResolution.sdCard;
                                    }
                                    else
                                    {
                                        deviceObj.FeatureConfig.VideoResolution.sdCard = "720";
                                    }
                                }
                            }
                            else
                            {
                                RecordSetting recObj = new RecordSetting();
                                recObj.fifo = data.recordSetting != null ? data.recordSetting.fifo : false;
                                recObj.scheduleRecording = data.recordSetting != null ? data.recordSetting.scheduleRecording : false;
                                recObj.offlineRecording = data.recordSetting != null ? data.recordSetting.offlineRecording : false;
                                if (data.recordSetting.eventRecording)
                                {
                                    var availFeatures = deviceObj.Subscription?.AvailableFeatures?.Select(x => Helper.Replace_withWhiteSpaceAndMakeTheStringIndexValueUpperCase(Convert.ToString((AlertType)x))).ToList();

                                    if (availFeatures.Contains("Loiter"))
                                    {
                                        recObj.loiter = true;
                                        recObj.eventRecording = true;
                                    }
                                    if (availFeatures.Contains("Trespassing"))
                                    {
                                        recObj.crowd = true;
                                        recObj.eventRecording = true;
                                    }
                                    if (availFeatures.Contains("Crowd"))
                                    {
                                        recObj.trespassing = true;
                                        recObj.eventRecording = true;
                                    }
                                }
                                else
                                {
                                    recObj.eventRecording = false;
                                }
                                deviceObj.recordSetting = recObj;
                                if (data.recordSchedules != null)
                                {
                                    if (deviceObj.recordSchedules != null)
                                    {
                                        deviceObj.recordSchedules = data.recordSchedules;
                                    }
                                    else
                                    {
                                        List<RecordSchedules> recSchLst = new List<RecordSchedules>();
                                        recSchLst = data.recordSchedules;
                                        deviceObj.recordSchedules = recSchLst;
                                    }
                                }
                                else
                                {
                                    List<RecordSchedules> recSchLst = new List<RecordSchedules>();
                                    deviceObj.recordSchedules = recSchLst;
                                }
                                if (deviceObj.FeatureConfig.VideoResolution != null)
                                {
                                    if (data.FeatureConfig.VideoResolution != null)
                                    {
                                        deviceObj.FeatureConfig.VideoResolution.sdCard = data.FeatureConfig.VideoResolution.sdCard;
                                    }
                                    else
                                    {
                                        deviceObj.FeatureConfig.VideoResolution.sdCard = "720";
                                    }
                                }
                                else
                                {
                                    VideoResolution videoReObj = new VideoResolution();
                                    if (data.FeatureConfig.VideoResolution != null)
                                    {
                                        videoReObj.sdCard = data.FeatureConfig.VideoResolution.sdCard;
                                    }
                                    else
                                    {
                                        videoReObj.sdCard = "480";
                                    }
                                    deviceObj.FeatureConfig.VideoResolution = videoReObj;
                                }
                            }
                            List<IotHub.Models.Schedule> iotrecordScheduleslst = new List<IotHub.Models.Schedule>();
                            int type = deviceObj.recordSetting.scheduleRecording ? 2 : deviceObj.recordSetting.offlineRecording ? 4 : deviceObj.recordSetting.manualRecording == "start" ? 3 : deviceObj.recordSetting.eventRecording ? 1 : 0;
                            if (deviceObj.recordSchedules.Count > 0)
                            {
                                foreach (var sch in deviceObj.recordSchedules)
                                {
                                    List<IotHub.Models.time> iotScheduletimeLst = new List<IotHub.Models.time>();
                                    if (sch.time.Count > 0)
                                    {
                                        foreach (var tm in sch.time)
                                        {
                                            IotHub.Models.time iotscheduletime = new IotHub.Models.time()
                                            {
                                                st = Convert.ToDateTime(tm.starttime).ToString("HH:mm"),
                                                et = Convert.ToDateTime(tm.endtime).ToString("HH:mm")
                                            };
                                            iotScheduletimeLst.Add(iotscheduletime);
                                        }
                                        IotHub.Models.Schedule iotrecordSchedules = new IotHub.Models.Schedule()
                                        {
                                            enabled = true,
                                            fullday = sch.fullday,
                                            times = iotScheduletimeLst
                                        };
                                        iotrecordScheduleslst.Add(iotrecordSchedules);
                                    }
                                    else
                                    {
                                        IotHub.Models.Schedule iotrecordSchedules = new IotHub.Models.Schedule()
                                        {
                                            enabled = false,
                                            fullday = sch.fullday,
                                            times = iotScheduletimeLst
                                        };
                                        iotrecordScheduleslst.Add(iotrecordSchedules);
                                    }
                                }
                            }

                            try
                            {
                                await _iotHub.UpdateCameraSettings(item.deviceId, deviceObj.recordSetting.fifo, type, deviceObj.FeatureConfig.VideoResolution.sdCard, _logger, iotrecordScheduleslst);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Exception in Configuring Iot for deviceid {item.deviceId} Exception :- {ex}");
                                copyCameraSettingResponse.Status = false;
                                copyCameraSettingResponse.Message = $"Device not found for deviceid {item.deviceId}";
                                return Ok(new MMSHttpReponse<CopyCameraSettingResponse> { ResponseBody = copyCameraSettingResponse });
                            }
                            List<RecHistory> recLst = new List<RecHistory>();
                            var date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Split(" ");
                            if (deviceObj.recordSetting.scheduleRecording)
                            {
                                for (int i = 0; i < deviceObj.recordSchedules.Count; i++)
                                {
                                    DateTime schdate = DateTime.UtcNow;
                                    string day = schdate.DayOfWeek.ToString();
                                    string stTime = DateTime.UtcNow.ToString("HH:mm:ss");
                                    int m = day == "Monday" ? 0 : (day == "Tuesday" ? 1 : (day == "Wednesday" ? 2 : (day == "Thursday" ? 3 : (day == "Friday" ? 4 : (day == "Saturday" ? 5 : 6)))));
                                    schdate = i < m ? schdate.AddDays(7 - m + i) : (m < i ? schdate.AddDays(i - m) : schdate);
                                    if (deviceObj.recordSchedules[i].fullday)
                                    {
                                        if (day != deviceObj.recordSchedules[i].day)
                                        {
                                            stTime = "00:00:00";
                                        }
                                        RecHistory recHistory = new RecHistory()
                                        {
                                            recType = 1,
                                            date = schdate.ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[0],
                                            starttime = stTime,
                                            endtime = "23:59:59",
                                        };
                                        recLst.Add(recHistory);
                                    }
                                    else
                                    {
                                        if (deviceObj.recordSchedules[i].time.Count > 0)
                                        {
                                            foreach (var tmItem in deviceObj.recordSchedules[i].time)
                                            {
                                                RecHistory recHistory = new RecHistory()
                                                {
                                                    recType = 1,
                                                    date = schdate.ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[0],
                                                    starttime = Convert.ToDateTime(tmItem.starttime).ToString("HH:mm:ss"),
                                                    endtime = Convert.ToDateTime(tmItem.endtime).ToString("HH:mm:ss"),
                                                };
                                                recLst.Add(recHistory);
                                            }
                                        }
                                    }
                                }
                                deviceObj.recHistories = recLst;
                            }
                            else
                            {
                                if (deviceObj.recordSetting.offlineRecording || deviceObj.recordSetting.manualRecording == "start")
                                {
                                    if (deviceObj.recHistories != null)
                                    {
                                        var advSch = deviceObj.recHistories.Where(x => Convert.ToDateTime(x.date + " " + x.starttime) >= DateTime.UtcNow).ToList();
                                        if (advSch.Count > 0)
                                        {
                                            foreach (var recItem in advSch)
                                            {
                                                deviceObj.recHistories.Remove(recItem);
                                            }
                                            if (Convert.ToDateTime(deviceObj.recHistories[deviceObj.recHistories.Count - 1].date + " " + deviceObj.recHistories[deviceObj.recHistories.Count - 1].endtime) >= DateTime.UtcNow)
                                            {
                                                deviceObj.recHistories[deviceObj.recHistories.Count - 1].endtime = date[1];
                                            }
                                        }
                                        if (!chkSech)
                                        {
                                            var lastRec = deviceObj.recHistories.Where(x => x.endtime == "").FirstOrDefault();
                                            if (lastRec != null)
                                            {
                                                lastRec.endtime = date[1];
                                            }
                                        }

                                        RecHistory recHistory = new RecHistory()
                                        {
                                            recType = deviceObj.recordSetting.offlineRecording ? 4 : 3,
                                            date = date[0],
                                            starttime = date[1],
                                            endtime = "",
                                        };
                                        deviceObj.recHistories.Add(recHistory);
                                    }
                                    else
                                    {//1 for schdeule,2 for event,3 for manual,4 offine
                                        RecHistory recHistory = new RecHistory()
                                        {
                                            recType = deviceObj.recordSetting.offlineRecording ? 4 : 3,
                                            date = date[0],
                                            starttime = date[1],
                                            endtime = "",
                                        };
                                        recLst.Add(recHistory);
                                        deviceObj.recHistories = recLst;
                                    }
                                }
                            }
                            await _deviceRepo.UpdateDevice(OrgId, deviceObj);
                        }
                    }

                    copyCameraSettingResponse.Status = true;
                    copyCameraSettingResponse.Message = "Camera settings copied successfully";
                }
                else
                {
                    copyCameraSettingResponse.Status = false;
                    copyCameraSettingResponse.Message = "Invalid deviceIds";
                }
                return Ok(new MMSHttpReponse<CopyCameraSettingResponse> { ResponseBody = copyCameraSettingResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while Fetching and setting Camera settings", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get all cameras by locations
        /// </summary>
        /// <param name="OrgId"></param>
        /// <returns></returns>
        [HttpGet("GetAllCameraLocation")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordVedioResolutionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCameraLocation(string OrgId)
        {
            try
            {
                List<AllCamareaLocationData> allCamareaLocationDatas = new List<AllCamareaLocationData>();
                AllCameraLocationResponse allCameraLocationResponse = new AllCameraLocationResponse();
                if (!string.IsNullOrEmpty(OrgId))
                {
                    var alldata = _locationRepo.Query(x => x.OrgId == OrgId).ToList();
                    if (alldata.Count > 0)
                    {
                        var allLocations = alldata.Select(x => x.Name).Distinct().ToList();
                        foreach (string loc in allLocations)
                        {
                            var ret = alldata.Where(x => x.Name == loc).Select(x => x.Id).ToList();
                            AllCamareaLocationData allcma = new AllCamareaLocationData()
                            {
                                Location = loc,
                                cameras = ret
                            };
                            allCamareaLocationDatas.Add(allcma);
                        }
                        allCameraLocationResponse.data = allCamareaLocationDatas;
                        allCameraLocationResponse.Status = true;
                        allCameraLocationResponse.Message = "Success";
                    }
                    else
                    {
                        allCameraLocationResponse.Status = false;
                        allCameraLocationResponse.Message = "OrgId not exist";
                    }
                }
                else
                {
                    allCameraLocationResponse.Status = false;
                    allCameraLocationResponse.Message = "Invalid OrgId";
                }
                return Ok(new MMSHttpReponse<AllCameraLocationResponse> { ResponseBody = allCameraLocationResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while getting All Camera locations {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Override on fifo
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="fifo"></param>
        /// <returns></returns>
        [HttpGet("OverrideOnFIFO")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordSettingsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> OverrideOnFIFO(string device_id, bool fifo)
        {
            try
            {
                RecordSettingsResponse recordSettingsResponse = new RecordSettingsResponse();
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
                        recordSettingsResponse.Status = false;
                        recordSettingsResponse.Message = "SD Card is not plugged-In";
                        return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
                    }
                    if (data.recordSetting != null)
                    {
                        data.recordSetting.fifo = fifo;
                    }
                    else
                    {
                        RecordSetting recordSetting = new RecordSetting();
                        recordSetting.fifo = fifo;
                        data.recordSetting = recordSetting;
                    }
                    try
                    {
                        await _iotHub.UpdateSDCardRecordSettings(device_id, "setFifo", false, false, false, false, fifo, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception in Configuring Iot for deviceid {device_id} Exception :- {ex}");
                        recordSettingsResponse.Status = false;
                        recordSettingsResponse.Message = $"Request time out for deviceid{device_id}";
                        return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
                    }
                    await _deviceRepo.UpdateDevice(OrgId, data);

                    recordSettingsResponse.Status = true;
                    recordSettingsResponse.Message = "FIFO updated successfully";
                }
                else
                {
                    recordSettingsResponse.Status = false;
                    recordSettingsResponse.Message = "Invalid deviceId";
                }

                return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError("FIFO updation failed", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Clear the Recordsettings
        /// </summary>
        /// <param name="device_id"></param>
        /// <returns></returns>
        [HttpGet("ClearRecordSettings")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordSettingsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearRecordSettings(string device_id)
        {
            try
            {
                var date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Split(" ");
                RecordSettingsResponse recordSettingsResponse = new RecordSettingsResponse();
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
                    if (data.recordSetting != null)
                    {

                        if (data.recHistories != null)
                        {
                            var advSch = data.recHistories.Where(x => Convert.ToDateTime(x.date + " " + x.starttime) >= DateTime.UtcNow).ToList();
                            if (advSch.Count > 0)
                            {
                                foreach (var item in advSch)
                                {
                                    data.recHistories.Remove(item);
                                }

                            }
                            if (data.recHistories.Count > 0)
                                data.recHistories[data.recHistories.Count - 1].endtime = date[1];

                        }
                        data.recordSetting.scheduleRecording = false;
                        data.recordSetting.scheduleRecording = false;
                        data.recordSetting.eventRecording = false;
                        data.recordSetting.offlineRecording = false;
                        data.recordSetting.manualRecording = "stop";
                        data.SdCardRecordingStatus = "Recording Stop";
                        data.recordSetting.loiter = false;
                        data.recordSetting.trespassing = false;
                        data.recordSetting.crowd = false;
                        //data.recHistories = null;
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
                            recordSettingsResponse.Status = false;
                            recordSettingsResponse.Message = $"Request time out for deviceid{device_id}";
                            return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
                        }
                        await _deviceRepo.UpdateDevice(OrgId, data);
                    }
                    recordSettingsResponse.Status = true;
                    recordSettingsResponse.Message = "Record settings are cleared successfully";
                }
                else
                {
                    recordSettingsResponse.Status = false;
                    recordSettingsResponse.Message = "Invalid deviceId";
                }

                return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to clear record settings", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Device is hitting this api to upload the sdcard record history to database
        /// </summary>
        /// <param name="sdCardRecHistoryRequest"></param>
        /// <returns></returns>
        [HttpPost("sd_card_rechistory")]
        [AllowAnonymous]
        public async Task<IActionResult> sdCardRecHistory(SDCardRecHistoryRequest sdCardRecHistoryRequest)

        {
            ResponseDTO responseDTO = new ResponseDTO();
            try
            {
                string scrtKey = _configuration.GetSection("SRTServerCredentials").GetSection("secretKey").Value;
                if (sdCardRecHistoryRequest.secretKey == scrtKey)
                {
                    if (sdCardRecHistoryRequest != null)
                    {
                        using (var transaction = _unitOfWork.BeginTransaction())
                        {
                            try
                            {
                                List<SDCardHistory> SDCardHistoryList = new List<SDCardHistory>();
                                if (sdCardRecHistoryRequest.sdCardRecHistory != null)
                                {
                                    if (sdCardRecHistoryRequest.sdCardRecHistory.Count > 0)
                                    {
                                        //1=sch,2=evnt,3=ma,4=off
                                        var cardRec = await _cardHistoryRepo.Query(x => x.deviceId == sdCardRecHistoryRequest.deviceId && x.type != 2).ToListAsync();
                                        if (cardRec.Count > 0)
                                        {
                                            _cardHistoryRepo.RemoveRangeAsync(cardRec.ToArray());
                                        }

                                        foreach (var item in sdCardRecHistoryRequest.sdCardRecHistory)
                                        {
                                            foreach (var recItem in item.sdRecTime)
                                            {
                                                SDCardHistory sDCardHistory = new SDCardHistory()
                                                {
                                                    Id = Guid.NewGuid().ToString(),
                                                    deviceId = sdCardRecHistoryRequest.deviceId,
                                                    date = item.date,
                                                    startTime = recItem.st.Split('#')[0].Replace('-', ':'),
                                                    endTime = recItem.et == "ongoing" ? recItem.et : recItem.et.Replace('-', ':'),
                                                    CreatedBy = "",
                                                    ModifiedBy = "",
                                                    CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                                    type = Convert.ToInt32(recItem.st.Split('#')[1]) == 2 ? 1 : Convert.ToInt32(recItem.st.Split('#')[1]),
                                                    size= Math.Round(Convert.ToDouble(recItem.s) / (1024 * 1024), 2).ToString(),
                                                };

                                                SDCardHistoryList.Add(sDCardHistory);
                                            }
                                        }
                                        await _cardHistoryRepo.AddRangeAsync(SDCardHistoryList);
                                    }
                                    else
                                    {
                                        var cardRec = await _cardHistoryRepo.Query(x => x.deviceId == sdCardRecHistoryRequest.deviceId && x.type != 2).ToListAsync();
                                        if (cardRec.Count > 0)
                                        {
                                            _cardHistoryRepo.RemoveRangeAsync(cardRec.ToArray());
                                        }
                                    }
                                    _cardHistoryRepo.SaveChanges();
                                    transaction.Commit();

                                    responseDTO.Status = true;
                                    responseDTO.Message = "Success";
                                }
                                if (sdCardRecHistoryRequest.eventRecHistory != null)
                                {
                                    if (sdCardRecHistoryRequest.eventRecHistory.Count > 0)
                                    {
                                        var cardRec = await _cardHistoryRepo.Query(x => x.deviceId == sdCardRecHistoryRequest.deviceId && x.type == 2).ToListAsync();
                                        if (cardRec.Count > 0)
                                        {
                                            _cardHistoryRepo.RemoveRangeAsync(cardRec.ToArray());
                                        }
                                        foreach (var item in sdCardRecHistoryRequest.eventRecHistory)
                                        {
                                            var getDateTime = item.Split('_');
                                            DateTime endTime = Convert.ToDateTime(getDateTime[0] + ' ' + getDateTime[1].Replace('-', ':').Split('.')[0]).AddSeconds(9);
                                            SDCardHistory sDCardHistory = new SDCardHistory()
                                            {
                                                Id = Guid.NewGuid().ToString(),
                                                deviceId = sdCardRecHistoryRequest.deviceId,
                                                date = getDateTime[0],
                                                startTime = getDateTime[1].Replace('-', ':').Split('.')[0],
                                                endTime = endTime.ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[1],
                                                CreatedBy = "",
                                                ModifiedBy = "",
                                                CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                                type = 2,
                                                size="3.00"
                                            };

                                            SDCardHistoryList.Add(sDCardHistory);
                                            await _cardHistoryRepo.AddRangeAsync(SDCardHistoryList);
                                        }
                                    }
                                    else
                                    {
                                        var cardRec = await _cardHistoryRepo.Query(x => x.deviceId == sdCardRecHistoryRequest.deviceId && x.type == 2).ToListAsync();
                                        if (cardRec.Count > 0)
                                        {
                                            _cardHistoryRepo.RemoveRangeAsync(cardRec.ToArray());
                                        }
                                    }
                                    _cardHistoryRepo.SaveChanges();
                                    transaction.Commit();
                                    responseDTO.Status = true;
                                    responseDTO.Message = "Success";
                                }

                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                _logger.LogError(ex.Message);
                                responseDTO.Status = false;
                                responseDTO.Message = ex.Message;
                            }
                        }
                    }
                    else
                    {
                        responseDTO.Status = false;
                        responseDTO.Message = "request json is empty";
                    }
                }
                else
                {
                    responseDTO.Status = false;
                    responseDTO.Message = "wrong secret key";
                }

            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Message = ex.Message;
            }
            return Ok(responseDTO);
        }

        /// <summary>
        /// Send commands device To upload the sd card history to database
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <returns></returns>
        [HttpPost("video_duration_list")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> videoDurationList(DeviceList deviceIds)
        {
            int j = 0;
            foreach (var item in deviceIds.setCameraIds)
            {
            Loop:
                if (j == deviceIds.setCameraIds.Count)
                    break;
                j++;

                var data = await _deviceRepo.GetDevice(deviceIds.setCameraIds[j - 1].deviceId, OrgId);
                if (data != null)
                {
                    try
                    {
                        await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "video_duration_list", 10, _logger);
                    }
                    catch (Exception ex)
                    {
                        goto Loop;
                    }
                }
            }
            return Ok(new MMSHttpReponse { SuccessMessage = "success" });
        }

        public static async Task SendPushNotificationToDevices(ILogger log, Device device, string Resolution)
        {
            try
            {
                var message = $"Video resolution is updated to {Resolution} Successfully.";
                var notifMessage = new FirebaseNotifModel()
                {
                    Condition = $"'{device.OrgId}' in topics",
                    Title = $"{device.DeviceName} ",
                    Body = $"{message}"
                };
                var FinalMessage = JsonConvert.SerializeObject(notifMessage);

                QueueClient queue = new QueueClient(Environment.GetEnvironmentVariable("pushNotifConnectionString"), Environment.GetEnvironmentVariable("pushNotifQueue"), new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });

                await queue.SendMessageAsync(FinalMessage);

                log.LogInformation($"Firebase message sent to OrgId {device.OrgId}");
            }
            catch (JsonException ex)
            {

                log.LogError($"Error while deserializing queue item {ex}");
            }
            catch (Exception ex)
            {
                log.LogError($"General error occured {ex}");
            }
        }
    }
}
