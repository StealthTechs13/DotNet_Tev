using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MMSConstants;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Tev.API.Models;
using Tev.Cosmos.IRepository;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class PlayBackController : TevControllerBase
    {
        private readonly IGenericRepo<SDCardHistory> _cardHistoryRepo;
        private readonly ILogger<RecordingController> _logger;
        private readonly IGenericRepo<Location> _locationRepo;
        private readonly IDeviceRepo _deviceRepo;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly ITevIoTRegistry _iotHub;
        private readonly IConfiguration _configuration;

        public PlayBackController(IConfiguration configuration, ITevIoTRegistry iotHub, ILogger<RecordingController> logger, IGenericRepo<Location> locationRepo, IDeviceRepo deviceRepo, IGenericRepo<UserDevicePermission> userDevicePermissionRepo, IGenericRepo<SDCardHistory> cardHistoryRepo)
        {
            _iotHub = iotHub;
            _logger = logger;
            _deviceRepo = deviceRepo;
            _locationRepo = locationRepo;
            _configuration = configuration;
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _cardHistoryRepo = cardHistoryRepo;
        }

        //[HttpPost("GetVideosList")]
        //[ProducesResponseType(typeof(MMSHttpReponse<List<PlayBackResponse>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetVideosList(PlayBackRequest playBackRequest)
        //{
        //    PlayBackResponse playBackResponse = new PlayBackResponse();
        //    try
        //    {
        //        var data = await _deviceRepo.GetDevice(playBackRequest.device_id, OrgId);
        //        if (data == null)
        //        {
        //            return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
        //        }

        //        if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
        //        {
        //            return Forbid();
        //        }
        //        List<VideoUrl> videoUrls = new List<VideoUrl>();
        //        for (int i = 1; i < 4; i++)
        //        {
        //            VideoUrl videoUrl = new VideoUrl()
        //            {
        //                videoUrls = "https://dummy/video" + i + ".mp4",
        //            };
        //            videoUrls.Add(videoUrl);
        //        }

        //        playBackResponse.Status = true;
        //        playBackResponse.Message = "Vedio Recordings";
        //        playBackResponse.data = videoUrls;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error occured while fetching Record Settings", ex);
        //        playBackResponse.Status = false;
        //        playBackResponse.Message = ex.Message;
        //    }
        //    return Ok(new MMSHttpReponse<PlayBackResponse> { ResponseBody = playBackResponse });
        //}

        /// <summary>
        /// Get the Dates of available videos
        /// </summary>
        /// <param name="device_Id"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet("GetDates")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<PlayBackResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDates(string device_Id, string month, string year)
        {
            PlayBackResponse playBackResponse = new PlayBackResponse();
            try
            {

                var data = await _deviceRepo.GetDevice(device_Id, OrgId);
                if (data == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                List<EntityLst> entitylst = new List<EntityLst>();
                var sdCardHist = await _cardHistoryRepo.Query(x => x.deviceId == device_Id).ToListAsync();
                if (sdCardHist.Count > 0)
                {
                    var monthData = sdCardHist.Where(x => x.date.Split("-")[1] == month).ToList();
                    if (monthData.Count > 0)
                    {
                        foreach (var item in monthData)
                        {
                            var checkEx = entitylst.Where(x => x.date == item.date).FirstOrDefault();
                            if (checkEx == null)
                            {
                                EntityLst enty = new EntityLst()
                                {
                                    date = item.date,
                                    count = monthData.Count(x => x.date == item.date),
                                };

                                entitylst.Add(enty);
                            }
                        }
                    }
                }
                if (entitylst.Count > 0)
                {
                    playBackResponse.Status = true;
                    playBackResponse.Message = "Dates of the month";
                    playBackResponse.entity = entitylst;
                }
                else
                {
                    playBackResponse.Status = false;
                    playBackResponse.Message = "No Record found";
                    playBackResponse.entity = entitylst;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Record Settings", ex);
                playBackResponse.Status = false;
                playBackResponse.Message = ex.Message;
            }
            return Ok(new MMSHttpReponse<PlayBackResponse> { ResponseBody = playBackResponse });
        }

        /// <summary>
        /// Get all revording available dates according to logical device id
        /// </summary>
        /// <param name="device_Id"></param>
        /// <returns></returns>
        [HttpGet("GetAllDates")]
        [ProducesResponseType(typeof(MMSHttpReponse<GetAllDatesResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDates(string device_Id)
        {
            GetAllDatesResponse getAllDatesResponse = new GetAllDatesResponse();
            try
            {

                var data = await _deviceRepo.GetDevice(device_Id, OrgId);
                if (data == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                List<EntityObj> entitylst = new List<EntityObj>();
                var sdCardHist = await _cardHistoryRepo.Query(x => x.deviceId == device_Id).ToListAsync();
                if (sdCardHist.Count > 0)
                {
                    var year = sdCardHist.OrderByDescending(x => Convert.ToDateTime(x.date + " " + x.startTime)).GroupBy(x => x.date.Split("-")[0]).Select(x => x.FirstOrDefault()).ToList();
                    foreach (var item in year)
                    {
                        List<MonthObj> mnthObjLst = new List<MonthObj>();
                        var months = sdCardHist.Where(x => x.date.Split("-")[0] == item.date.Split("-")[0]).OrderByDescending(x => Convert.ToDateTime(x.date + " " + x.startTime)).GroupBy(x => x.date.Split("-")[1]).Select(x => x.FirstOrDefault()).ToList();
                        foreach (var itemMonths in months)
                        {
                            List<DateObj> dateObjLst = new List<DateObj>();
                            MonthObj mnthObj = new MonthObj();
                            mnthObj.month = itemMonths.date.Split("-")[1];
                            var monthData = sdCardHist.Where(x => x.date.Split("-")[1] == itemMonths.date.Split("-")[1] && x.date.Split("-")[0] == itemMonths.date.Split("-")[0]).OrderByDescending(x => Convert.ToDateTime(x.date + " " + x.startTime)).GroupBy(x => x.date).Select(x => x.FirstOrDefault()).ToList();
                            foreach (var itemdt in monthData)
                            {
                                DateObj dateObj = new DateObj();
                                dateObj.date = itemdt.date;
                                dateObj.count = sdCardHist.Count(x => x.date == itemdt.date);
                                dateObjLst.Add(dateObj);
                            }
                            mnthObj.dates = dateObjLst;
                            mnthObjLst.Add(mnthObj);
                        }
                        EntityObj enty = new EntityObj()
                        {
                            year = item.date.Split("-")[0],
                            months = mnthObjLst,
                        };
                        entitylst.Add(enty);
                    }
                }
                if (entitylst.Count > 0)
                {
                    getAllDatesResponse.Status = true;
                    getAllDatesResponse.Message = "Dates of the device available recordings";
                    getAllDatesResponse.entity = entitylst;
                }
                else
                {
                    getAllDatesResponse.Status = false;
                    getAllDatesResponse.Message = "No Record found";
                    getAllDatesResponse.entity = entitylst;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Record Settings", ex);
                getAllDatesResponse.Status = false;
                getAllDatesResponse.Message = ex.Message;
            }
            return Ok(new MMSHttpReponse<GetAllDatesResponse> { ResponseBody = getAllDatesResponse });
        }

        /// <summary>
        /// Available Video's time in array for the selected day
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="date"></param>
        /// <param name="offSet"></param>
        /// <returns></returns>
        /// 
        [HttpPost("GetTimeArray")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<PlayBackResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTimeArray(string device_id, string date, double offSet)
        {
            PlayBackResponse timeArrayResponse = new PlayBackResponse();
            List<Grid24Hour> grid24HourLst = new List<Grid24Hour>();
            try
            {
                string dt = date.Split("T")[0];
                var data = await _deviceRepo.GetDevice(device_id, OrgId);
                if (data == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                {
                    return Forbid();
                }

                var sdCardHis = await _cardHistoryRepo.Query(x => x.date == dt && x.deviceId == device_id).ToListAsync();
                if (sdCardHis.Count > 0)
                {
                    foreach (var item in sdCardHis.OrderBy(x => Convert.ToDateTime(x.date + " " + x.startTime)))
                    {
                        if (item.endTime == "ongoing")
                        {
                            string currentdt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                            item.endTime = Convert.ToDateTime(currentdt).AddMilliseconds(offSet).ToString("yyyy-MM-dd HH:mm:ss").Split(" ")[1];
                        }
                        Grid24Hour grid24Hour = new Grid24Hour()
                        {
                            start_time = item.startTime,
                            end_time = item.endTime,
                            type = item.type,
                        };
                        grid24HourLst.Add(grid24Hour);
                    }
                    if (grid24HourLst.Count > 0)
                    {
                        timeArrayResponse.Status = true;
                        timeArrayResponse.Message = "Successfully Get Time Array for 24 hours grid";
                        timeArrayResponse.data = grid24HourLst;
                    }
                    else
                    {
                        timeArrayResponse.Status = false;
                        timeArrayResponse.Message = "No Time Array for 24 hours grid";
                        timeArrayResponse.data = grid24HourLst;
                    }
                }
                else
                {
                    timeArrayResponse.Status = false;
                    timeArrayResponse.Message = "No Time Array for 24 hours grid";
                    timeArrayResponse.data = grid24HourLst;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Record Settings", ex);
                timeArrayResponse.Status = false;
                timeArrayResponse.Message = ex.Message;
            }
            return Ok(new MMSHttpReponse<PlayBackResponse> { ResponseBody = timeArrayResponse });
        }


        /// <summary>
        /// To Send the command to device for available video list for the selected time period
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        [HttpPost("VideoDownload")]
        [ProducesResponseType(typeof(MMSHttpReponse<VideoList>), StatusCodes.Status200OK)]
        public async Task<IActionResult> VideoDownload(DeviceList deviceIds, string startDateTime, string endDateTime)
        {
            DateTime st = Convert.ToDateTime(startDateTime);
            DateTime et = Convert.ToDateTime(endDateTime);
            List<VideoList> videoLists = new List<VideoList>();
            string date = startDateTime.Split('T')[0];
            if (deviceIds.setCameraIds.Count > 0)
            {
                foreach (var item in deviceIds.setCameraIds)
                {
                    try
                    {
                        List<fileDetails> listfileDetail = new List<fileDetails>();
                        var data = await _deviceRepo.GetDevice(item.deviceId, OrgId);
                        if (data != null)
                        {
                            var sdCardHistory = await _cardHistoryRepo.Query(x => x.date == date && x.deviceId == item.deviceId && x.endTime != "ongoing").ToListAsync();

                            var sdCardHis = sdCardHistory.Where(x => (Convert.ToDateTime(x.date + " " + x.endTime) >= et 
                                && Convert.ToDateTime(x.date + " " + x.startTime) <= et) 
                                || (Convert.ToDateTime(x.date + " " + x.startTime) <= st 
                                && Convert.ToDateTime(x.date + " " + x.endTime) > st) 
                                || (Convert.ToDateTime(x.date + " " + x.startTime) >= st 
                                && Convert.ToDateTime(x.date + " " + x.endTime) <= et)).ToList();
                            
                            if (sdCardHis.Count > 0)
                            {
                                List<Cosmos.Entity.VideoList> videoDatas = new List<Cosmos.Entity.VideoList>();
                                foreach (var vlist in sdCardHis)
                                {
                                    string filename = vlist.date + "_" + vlist.startTime.Replace(':', '-');
                                    if (vlist.type == 2)
                                    {
                                        filename = filename + ".mp4";
                                    }
                                    else
                                    {
                                        //on cloud type 1 is for schedule and in device type 1 is for event
                                        if (vlist.type == 1)
                                        {
                                            filename = filename + "#2" + ".mkv";
                                        }
                                        else
                                        {
                                            filename = filename + "#" + vlist.type + ".mkv";
                                        }
                                    }

                                    TimeSpan t = Convert.ToDateTime(vlist.date + " " + vlist.endTime) - Convert.ToDateTime(vlist.date + " " + vlist.startTime);
                                    fileDetails filedetails = new fileDetails()
                                    {
                                        filename = filename,
                                        size = vlist.size + "MB",
                                        cameraName = data.DeviceName,
                                        startDateTime = vlist.startTime,
                                        duration = t.TotalSeconds.ToString()
                                    };
                                    listfileDetail.Add(filedetails);

                                    Cosmos.Entity.VideoList videoData = new Cosmos.Entity.VideoList()
                                    {
                                        filename = filedetails.filename,
                                        size = filedetails.size.Contains("MB") ? filedetails.size : (filedetails.size + "MB"),
                                        duration = filedetails.duration,
                                        Isuploaded = false,
                                        IsDeleted = false,
                                        timestamp = DateTime.Now
                                    };
                                    videoDatas.Add(videoData);
                                }
                                VideoList videoList = new VideoList()
                                {
                                    deviceId = item.deviceId,
                                    filedetails = listfileDetail
                                };
                                videoLists.Add(videoList);

                                if (data.videoLists == null)
                                {
                                    data.videoLists = videoDatas;
                                }
                                else
                                {
                                    foreach (var vdata in videoDatas)
                                    {
                                        if (!data.videoLists.Contains(vdata))
                                            data.videoLists.Add(vdata);
                                    }
                                }
                                await _deviceRepo.UpdateDevice(OrgId, data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error occured while fetching video list", ex);
                    }
                }
            }
            return Ok(new MMSHttpReponse<List<VideoList>> { ResponseBody = videoLists });
        }

        /// <summary>
        /// To Upload the Video on blob storage
        /// </summary>
        /// <param name="downloadVedioList"></param>
        /// <returns></returns>
        [HttpPost("UploadVideo")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MMSHttpReponse<List<VideoUploadResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadVideo(DownloadVedioList downloadVedioList)
        {
            List<VideoUploadResponse> videoUploadResponses = new List<VideoUploadResponse>();
            if (downloadVedioList.camerasLists.Count > 0)
            {

                foreach (var item in downloadVedioList.camerasLists)
                {
                    var data = await _deviceRepo.GetDevice(item.deviceId, OrgId);
                    bool isblobAlreadyExist = false;
                    if (data != null)
                    {
                        try
                        {
                            var jsonMethodParam = new
                            {
                                video_list = false,
                                filename = item.filename
                            };

                            #region Azure Blob
                            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_configuration.GetSection("blob").GetSection("ConnectionString").Value);
                            CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
                            CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(_configuration.GetSection("blob").GetSection("downloadvideoContainerName").Value);
                            CloudBlobDirectory directory = _cloudBlobContainer.GetDirectoryReference(data.LogicalDeviceId);
                            var rootDirFolders = directory.ListBlobsSegmentedAsync(true, BlobListingDetails.Metadata, 100, null, null, null).Result;
                            if (rootDirFolders.Results.Count() == 0)
                            {
                                var blockBlob = directory.GetBlockBlobReference("Demo.txt");
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    await blockBlob.UploadFromStreamAsync(ms); //Empty memory stream. Will create an empty blob.
                                }
                            }

                            foreach (var lst in rootDirFolders.Results.OfType<CloudBlockBlob>())
                            {
                                string existingFile = lst.Name.Split('/')[1];
                                if (item.filename == existingFile)
                                    isblobAlreadyExist = true;
                            }


                            #endregion Azure Blob
                            if (!isblobAlreadyExist)
                            {
                                var res = await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "sdcard_video", 10, _logger, Newtonsoft.Json.JsonConvert.SerializeObject(jsonMethodParam));
                                if (res.Status == 200)
                                {
                                    await _iotHub.UpdateVideoFilename(item.deviceId, item.filename);
                                    VideoUploadResponse videoUploadResponse = new VideoUploadResponse()
                                    {
                                        deviceId = item.deviceId,
                                        Status = true,
                                        Message = "Request sent to upload the video"
                                    };
                                    videoUploadResponses.Add(videoUploadResponse);
                                }
                                else
                                {
                                    if (data.videoLists != null && data.videoLists.Count > 0)
                                    {
                                        var file = data.videoLists.Where(x => x.filename == item.filename).FirstOrDefault();
                                        if (file != null)
                                        {
                                            data.videoLists.Remove(file);
                                            await _deviceRepo.UpdateDevice(OrgId, data);
                                        }
                                    }
                                    VideoUploadResponse videoUploadResponse = new VideoUploadResponse()
                                    {
                                        deviceId = item.deviceId,
                                        Status = true,
                                        Message = "Video not found"
                                    };
                                    videoUploadResponses.Add(videoUploadResponse);
                                }
                            }
                            else
                            {
                                var vdoDetail = data.videoLists.Where(x => x.filename == item.filename).FirstOrDefault();
                                if (vdoDetail != null)
                                {
                                    vdoDetail.Isuploaded = true;
                                    await _deviceRepo.UpdateDevice(OrgId, data);
                                }
                                VideoUploadResponse videoUploadResponse = new VideoUploadResponse()
                                {
                                    deviceId = item.deviceId,
                                    Status = true,
                                    Message = "Request sent to upload the video"
                                };
                                videoUploadResponses.Add(videoUploadResponse);

                            }
                        }
                        catch (DeviceNotFoundException ex)
                        {
                            _logger.LogError("Device not found {ex}", ex);
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found: " + item.deviceId });
                        }
                        catch (TimeoutException ex)
                        {
                            _logger.LogError("Request time out {ex}", ex);
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out: " + item.deviceId });
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains(":404103,"))
                            {
                                data.Online = false;
                                await _deviceRepo.UpdateDevice(OrgId, data);
                                _logger.LogError("Device is not online {ex}", ex);
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Device is not online or device does not have this feature: " + item.deviceId });
                            }
                            if (ex.Message.Contains(":504101,"))
                            {
                                _logger.LogError("Request time out {ex}", ex);
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out: " + item.deviceId });
                            }
                            _logger.LogError("Exception in DeviceValidationCheck {ex}", ex);
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                    }
                }
            }
            return Ok(new MMSHttpReponse<List<VideoUploadResponse>> { ResponseBody = videoUploadResponses });
        }

        /// <summary>
        /// To create the  Downloadable url for selected video file
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <returns></returns>
        [HttpPost("GetVideoDownloadUrls")]
        [ProducesResponseType(typeof(MMSHttpReponse<DownloadUrlsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVideoDownloadUrls(DownloadVedioList deviceIds)
        {
            DownloadUrlsResponse downloadUrlsResponse = new DownloadUrlsResponse();
            List<DownloadUrlsObj> downloadUrlsObj = new List<DownloadUrlsObj>();
            if (deviceIds.camerasLists.Count > 0)
            {
                #region Azure Blob
                var client = new BlobContainerClient(_configuration.GetSection("blob").GetSection("ConnectionString").Value,
                  _configuration.GetSection("blob").GetSection("downloadvideoContainerName").Value);
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
                #endregion Azure Blob
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_configuration.GetSection("blob").GetSection("ConnectionString").Value);
                CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(_configuration.GetSection("blob").GetSection("downloadvideoContainerName").Value);

                foreach (var item in deviceIds.camerasLists)
                {
                    var data = await _deviceRepo.GetDevice(item.deviceId, OrgId);
                    if (data != null && data.videoLists != null && data.videoLists.Count > 0)
                    {
                        var dltvideo = data.videoLists.Where(x => DateTime.UtcNow.Subtract(x.timestamp).TotalSeconds >= 86400).ToList();
                        if (dltvideo != null && dltvideo.Count > 0)
                        {
                            CloudBlobDirectory directory = _cloudBlobContainer.GetDirectoryReference(data.LogicalDeviceId);
                            int i = 0;
                            foreach (var vdo in dltvideo)
                            {
                                if (i >= dltvideo.Count)
                                    break;
                                try
                                {
                                    data.videoLists.Remove(vdo);
                                    CloudBlockBlob _blockBlobImag = directory.GetBlockBlobReference(vdo.filename);
                                    var res = await _blockBlobImag.DeleteIfExistsAsync();
                                    i++;
                                }
                                catch (Exception ex)
                                {
                                    i++;
                                    continue;
                                }
                            }
                            await _deviceRepo.UpdateDevice(OrgId, data);
                        }

                        var vlist = data.videoLists.Where(x => x.Isuploaded == true && x.IsDeleted == false && x.filename == item.filename && DateTime.UtcNow.Subtract(x.timestamp).TotalSeconds <= 86400).FirstOrDefault();
                        if (vlist != null)
                        {
                            DownloadUrlsObj dwnObj = new DownloadUrlsObj()
                            {
                                DeviceId = item.deviceId,
                                DeviceName = data.DeviceName,
                                DownUrls = _configuration.GetSection("blob").GetSection("sdcardblob").Value + "/" + data.LogicalDeviceId + "/" + $"{vlist.filename}?" + sas,
                                filename = vlist.filename
                            };
                            downloadUrlsObj.Add(dwnObj);
                            await _iotHub.UpdateVideoFilename(item.deviceId, "");
                        }
                    }
                }


                if (downloadUrlsObj.Count > 0)
                {
                    downloadUrlsResponse.Status = true;
                    downloadUrlsResponse.Message = "Downloadable urls are fetched successfully";
                    downloadUrlsResponse.downloadUrlsObj = downloadUrlsObj;
                }
                else
                {
                    downloadUrlsResponse.Status = false;
                    downloadUrlsResponse.Message = "No downloads are present";
                    downloadUrlsResponse.downloadUrlsObj = downloadUrlsObj;
                }
            }
            else
            {
                downloadUrlsResponse.Status = false;
                downloadUrlsResponse.Message = "No device id is found";
                downloadUrlsResponse.downloadUrlsObj = downloadUrlsObj;
            }
            return Ok(new MMSHttpReponse<DownloadUrlsResponse> { ResponseBody = downloadUrlsResponse });
        }

        /// <summary>
        /// To get the available video list
        /// </summary>
        /// <param name="deviceList"></param>
        /// <returns></returns>
        [HttpPost("GetvideoList")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<VideoList>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetvideoList(DeviceList deviceList)
        {
            List<VideoList> videoLists = new List<VideoList>();
            try
            {
                if (deviceList.setCameraIds.Count > 0)
                {
                    foreach (var item in deviceList.setCameraIds)
                    {
                        List<fileDetails> listfileDetail = new List<fileDetails>();
                        var data = await _deviceRepo.GetDevice(item.deviceId, OrgId);
                        if (data.videoLists != null && data.videoLists.Count > 0)
                        {
                            foreach (var vlist in data.videoLists)
                            {
                                var chkFile = listfileDetail.Where(x => x.filename == vlist.filename).FirstOrDefault();
                                if (chkFile == null)
                                {
                                    string dateTime = vlist.filename;
                                    dateTime = dateTime.Replace("_", " ").Split('.')[0];

                                    string size = "";
                                    if (vlist.size.Contains("mb"))
                                        size = Math.Round(Convert.ToDouble(vlist.size.Split("mb")[0]), 2).ToString() + "mb";
                                    else if (vlist.size.Contains("b"))
                                        size = Math.Round(Convert.ToDouble(vlist.size.Split("b")[0]), 2).ToString() + "b";
                                    fileDetails filedetails = new fileDetails()
                                    {
                                        filename = vlist.filename,
                                        size = size,
                                        cameraName = data.DeviceName,
                                        startDateTime = dateTime,
                                        duration = vlist.duration

                                    };
                                    listfileDetail.Add(filedetails);
                                }
                            }
                            VideoList videoList = new VideoList()
                            {
                                deviceId = item.deviceId,
                                filedetails = listfileDetail
                            };
                            videoLists.Add(videoList);
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Record Settings", ex);
            }
            return Ok(new MMSHttpReponse<List<VideoList>> { ResponseBody = videoLists });
        }

        /// <summary>
        /// To Get the available video for the devices
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetAllDeviceVideoList")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<VideoList>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDeviceVideoList()
        {
            List<VideoList> videoLists = new List<VideoList>();
            try
            {
                var result = await _deviceRepo.GetTev2DeviceByOrgId(OrgId);
                if (result.Count > 0)
                {
                    foreach (var item in result)
                    {
                        List<fileDetails> listfileDetail = new List<fileDetails>();
                        if (item.videoLists != null && item.videoLists.Count > 0)
                        {
                            var allList = item.videoLists.Where(x => DateTime.UtcNow.Subtract(x.timestamp).TotalSeconds <= 86400).ToList();
                            if (allList != null && allList.Count > 0)
                            {
                                foreach (var vlist in allList)
                                {
                                    var chkFile = listfileDetail.Where(x => x.filename == vlist.filename).FirstOrDefault();
                                    if (chkFile == null)
                                    {
                                        string dateTime = vlist.filename;
                                        dateTime = dateTime.Replace("_", " ").Split('.')[0];
                                        string size = "";
                                        if (vlist.size.Contains("mb"))
                                            size = Math.Round(Convert.ToDouble(vlist.size.Split("mb")[0]), 2).ToString() + "mb";
                                        else if (vlist.size.Contains("b"))
                                            size = Math.Round(Convert.ToDouble(vlist.size.Split("b")[0]), 2).ToString() + "b";

                                        string fileSize = "";
                                        fileSize = vlist.size != String.Empty && !vlist.size.ToLower().Contains("mb")
                                                    ? vlist.size + "MB" : vlist.size;

                                        fileDetails filedetails = new fileDetails()
                                        {
                                            filename = vlist.filename,
                                            size = vlist.size,
                                            cameraName = item.DeviceName,
                                            startDateTime = dateTime,
                                            duration = vlist.duration
                                        };
                                        listfileDetail.Add(filedetails);
                                    }
                                }
                                VideoList videoList = new VideoList()
                                {
                                    deviceId = item.LogicalDeviceId,
                                    filedetails = listfileDetail
                                };
                                videoLists.Add(videoList);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while fetching Record Settings", ex);
            }
            return Ok(new MMSHttpReponse<List<VideoList>> { ResponseBody = videoLists });
        }

        /// <summary>
        /// Get the Download Video Url for all the available videos for all devices
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetAllDeviceVideoDownloadUrls")]
        [ProducesResponseType(typeof(MMSHttpReponse<DownloadUrlsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDeviceVideoDownloadUrls()
        {
            DownloadUrlsResponse downloadUrlsResponse = new DownloadUrlsResponse();
            List<DownloadUrlsObj> downloadUrlsObj = new List<DownloadUrlsObj>();
            try
            {
                var result = await _deviceRepo.GetTev2DeviceByOrgId(OrgId);

                if (result.Count > 0)
                {
                    #region Azure Blob
                    var client = new BlobContainerClient(_configuration.GetSection("blob").GetSection("ConnectionString").Value,
                      _configuration.GetSection("blob").GetSection("downloadvideoContainerName").Value);
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
                    #endregion Azure Blob
                    foreach (var item in result)
                    {
                        if (item.videoLists != null && item.videoLists.Count > 0)
                        {
                            var vlist = item.videoLists.Where(x => x.Isuploaded == true && x.IsDeleted == false).ToList();
                            if (vlist != null && vlist.Count > 0)
                            {
                                foreach (var urlData in vlist)
                                {
                                    DownloadUrlsObj dwnObj = new DownloadUrlsObj()
                                    {
                                        DeviceId = item.LogicalDeviceId,
                                        DeviceName = item.DeviceName,
                                        DownUrls = _configuration.GetSection("blob").GetSection("sdcardblob").Value + "/" + item.LogicalDeviceId + "/" + $"{urlData.filename}?" + sas,
                                        filename = urlData.filename
                                    };
                                    downloadUrlsObj.Add(dwnObj);
                                    await _iotHub.UpdateVideoFilename(item.LogicalDeviceId, "");
                                }
                            }
                        }
                    }
                    if (downloadUrlsObj.Count > 0)
                    {
                        downloadUrlsResponse.Status = true;
                        downloadUrlsResponse.Message = "Downloadable urls are fetched successfully";
                        downloadUrlsResponse.downloadUrlsObj = downloadUrlsObj;
                    }
                    else
                    {
                        downloadUrlsResponse.Status = false;
                        downloadUrlsResponse.Message = "No downloads are present";
                        downloadUrlsResponse.downloadUrlsObj = downloadUrlsObj;
                    }
                }
                else
                {
                    downloadUrlsResponse.Status = false;
                    downloadUrlsResponse.Message = "No device id is found";
                    downloadUrlsResponse.downloadUrlsObj = downloadUrlsObj;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured creating video download urls", ex);
            }
            return Ok(new MMSHttpReponse<DownloadUrlsResponse> { ResponseBody = downloadUrlsResponse });
        }

        /// <summary>
        /// Delete the video details from Database
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpPost("DeleteVideo")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<PlayBackResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteVideo(string device_id, string filename)
        {
            try
            {

                if (filename == null || filename == "")
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Invalid filename" });

                var data = await _deviceRepo.GetDevice(device_id, OrgId);
                if (data == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                if (data.videoLists != null && data.videoLists.Count > 0)
                {
                    var vlist = data.videoLists.Where(x => x.filename == filename).FirstOrDefault();
                    if (vlist != null)
                    {
                        //data.videoLists.Remove(vlist);
                        vlist.IsDeleted = true;
                        await _deviceRepo.UpdateDevice(OrgId, data);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while deleting video file details from cosmos", ex);
                return Ok("success");
            }
            return Ok("success");
        }

    }
}
