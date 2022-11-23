using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.KinesisVideo;
using Amazon.KinesisVideo.Model;
using Amazon.Runtime.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMSConstants;
using Tev.API.Models;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using System.Diagnostics;
using Tev.IotHub.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tev.DAL;
using Tev.Cosmos.IRepository;
using Tev.Cosmos.Entity;
using Microsoft.Azure.Devices.Client.Exceptions;
using Amazon.KinesisVideoArchivedMedia.Model;
using Amazon.KinesisVideoArchivedMedia;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class VideoController : TevControllerBase
    {
        private readonly ITevIoTRegistry _iotHub;
        private readonly ILogger<VideoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly IGenericRepo<DAL.Entities.LiveStreaming> _liveStreamRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDeviceRepo _deviceRepo;

       



        public VideoController(ITevIoTRegistry iotHub, ILogger<VideoController> logger, IConfiguration configuration,
            IGenericRepo<UserDevicePermission> genericRepo, IGenericRepo<DAL.Entities.LiveStreaming> liveStreamRepo, IUnitOfWork unitOfWork,
            IDeviceRepo deviceRepo)
        {
            _iotHub = iotHub;
            _logger = logger;
            _configuration = configuration;
            _userDevicePermissionRepo = genericRepo;
            _liveStreamRepo = liveStreamRepo;
            _unitOfWork = unitOfWork;
            _deviceRepo = deviceRepo;
        }

        /// <summary>
        /// API to start video streaming, actual HLS URL will be provided by signal R
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("Start/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<VideoStreamingStartResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Start([Required] string deviceId)
        {
            try
            {
                var T1 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                //Test publish
                _logger.LogInformation($"LIVEO API called at {T1} for deviceid {deviceId}");
                var awsAccessKeyId = this._configuration.GetSection("liveStreaming").GetSection("awsAccessKeyId").Value;
                var awsSecretKey = this._configuration.GetSection("liveStreaming").GetSection("awsSecretKey").Value;
                var region = this._configuration.GetSection("liveStreaming").GetSection("awsRegion").Value;
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (device != null)
                {
                    //Commenting for Bug 1894
                    if (!IsDeviceAuthorizedAsAdmin(device, _userDevicePermissionRepo))
                    {
                        return Forbid();
                    }
                    if (device.DeviceType != nameof(Applications.TEV) && device.DeviceType != nameof(Applications.TEV2))
                    {
                        return Forbid();
                    }
                    if (device.LiveStreaming.IsLiveStreaming)
                    {
                        try
                        {
                            _logger.LogInformation($"LIVEO IsLiveStreaming {device.LiveStreaming.IsLiveStreaming} for deviceid {deviceId}");
                            using (AmazonKinesisVideoClient kinesisClient = new AmazonKinesisVideoClient(awsAccessKeyId, awsSecretKey, RegionEndpoint.APSouth1))
                            {
                                var result = await GetHLSUrl(kinesisClient, awsAccessKeyId, awsSecretKey, deviceId, device.DeviceType);
                                if(string.IsNullOrEmpty(result) || result == "Error")
                                {
                                    _logger.LogInformation($"LIVEO IsLiveStreaming TRUE but got error  :- {result} for deviceid {deviceId}");
                                    device.LiveStreaming.IsLiveStreaming = false;
                                    var res = await PrepareLiveTreaming(device, deviceId, awsAccessKeyId, awsSecretKey, region);
                                    await _deviceRepo.UpdateDevice(OrgId, device);
                                    //try
                                    //{
                                    //    await CreateAWSVideoStream(awsAccessKeyId, awsSecretKey, deviceId);
                                    //    var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                                    //    var diff = T2 - T1;
                                    //    _logger.LogInformation($"LIVEO IsLiveStreaming TRUE but got error  after that response at T2 {T2} Time Diff {diff} for deviceid {deviceId}");
                                    //    return res;

                                    //}
                                    //catch (ResourceInUseException)
                                    //{

                                    //    return res;
                                    //}
                                    return res;
                                }
                                else
                                {
                                    _logger.LogInformation($"LIVEO IsLiveStreaming TRUE no error for deviceid {deviceId} ");
                                    var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                                    var diff = T2 - T1;
                                    _logger.LogInformation($"LIVEO IsLiveStreaming TRUE no error response sending at {T2} Time Diff {diff} for deviceid {deviceId}");
                                    return Ok(new MMSHttpReponse<VideoStreamingStartResponse>
                                    {
                                        ResponseBody = new VideoStreamingStartResponse
                                        {
                                            RetryAfter = 1,
                                            IsLiveStreaming = true,
                                            HLSUrl = result
                                        }
                                    });
                                }
                                

                            }
                        }
                        catch (Amazon.KinesisVideoArchivedMedia.Model.ResourceNotFoundException ex)
                        {
                            _logger.LogInformation($"LIVEO IsLiveStreaming TRUE Resource Not Found for deviceid {deviceId} Exception  :- {ex}");
                            device.LiveStreaming.IsLiveStreaming = false;
                            await _deviceRepo.UpdateDevice(OrgId, device);
                            var res = await PrepareLiveTreaming(device, deviceId, awsAccessKeyId, awsSecretKey, region);
                            //try
                            //{
                            //    await CreateAWSVideoStream(awsAccessKeyId, awsSecretKey, deviceId);
                            //    var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                            //    var diff = T2 - T1;
                            //    _logger.LogInformation($"LIVEO IsLiveStreaming TRUE Resource Not Found Exception response sending at {T2} Time Diff {diff} for deviceid {deviceId}");
                            //    return res;

                            //}
                            //catch (ResourceInUseException)
                            //{
                            //    var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                            //    var diff = T2 - T1;
                            //    _logger.LogInformation($"LIVEO IsLiveStreaming TRUE Resource Not Found Exception and ResourceInUseException response sending at {T2} Time Diff {diff} for deviceid {deviceId}");
                            //    return res;
                            //}
                            return res;
                            //return Ok(new MMSHttpReponse<VideoStreamingStartResponse>
                            //{
                            //    ResponseBody = new VideoStreamingStartResponse
                            //    {
                            //        RetryAfter = 1,
                            //        IsLiveStreaming=false,
                            //        HLSUrl = null
                            //    }
                            //});
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"LIVEO IsLiveStreaming FALSE started at {new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()} for deviceid {deviceId}");
                        var res = await PrepareLiveTreaming(device, deviceId, awsAccessKeyId, awsSecretKey, region);
                        //try
                        //{
                        //    await CreateAWSVideoStream(awsAccessKeyId, awsSecretKey, deviceId);
                        //    var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                        //    var diff = T2 - T1;
                        //    _logger.LogInformation($"LIVEO IsLiveStreaming FALSE response sending at {T2} Time Diff {diff} for deviceid {deviceId}");
                        //    return res;

                        //}
                        //catch (ResourceInUseException)
                        //{
                        //    var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                        //    var diff = T2 - T1;
                        //    _logger.LogInformation($"LIVEO IsLiveStreaming FALSE and ResourceInUseException response sending at {T2} Time Diff {diff} for deviceid {deviceId}");
                        //    return res;
                        //}
                        return res;
                    }
                    
                }
                else
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }
                //UpdateTimeInSQL(deviceId);// Abonded code
              
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while live streaming {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
            }
        }

        /// <summary>
        /// API to stop video streaming
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="seqNumber"></param>
        /// <returns></returns>
        [HttpGet("Stop/{deviceId}/{seqNumber}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Stop([Required] string deviceId, [Required] long seqNumber)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device is not found" });
                }

                if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return Forbid();
                }

                if (device.DeviceType != nameof(Applications.TEV) && device.DeviceType != nameof(Applications.TEV2))
                {
                    return Forbid();
                }

                IQueueClient queueClient = new QueueClient(this._configuration.GetSection("serviceBus").GetSection("connectionString").Value,
                    this._configuration.GetSection("serviceBus").GetSection("liveStreamQueueName").Value);



                // Send a message to the queue, so that azure function can cancel the auto stop queue message, close live streaming and record viewed time in database
                var liveStreamQueueModel = new LiveStreamQueueModel
                {
                    DeviceId = device.Id,
                    LogicalDeviceId = device.LogicalDeviceId,                            
                    Message = "Manual stop by user",
                    MessageCode = 9996,
                    AzureFunctionRetryCount = 0,
                    DeviceName = device.DeviceName,
                    AutoStopSequenceNumber = Convert.ToInt64(seqNumber),
                    OrgId = Convert.ToInt32(OrgId)

                };
                Message msg = new Message()
                {
                    Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(liveStreamQueueModel)),
                };
                await queueClient.SendAsync(msg);
                return Ok( new MMSHttpReponse { SuccessMessage = "success"});
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while stopping live streaming {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "Error occured while stopping live stream" });
            }
        }

        /// <summary>
        /// API to get HLS url
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetHLSUrl/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHLS([Required] string deviceId)
        {

            try
            {
                var T1 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                _logger.LogInformation($"GetHLS API called at :- {T1}");
                var awsAccessKeyId = this._configuration.GetSection("liveStreaming").GetSection("awsAccessKeyId").Value;
                var awsSecretKey = this._configuration.GetSection("liveStreaming").GetSection("awsSecretKey").Value;
                var region = this._configuration.GetSection("liveStreaming").GetSection("awsRegion").Value;

                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                if (device != null)
                {
                    //Commenting for Bug 1894
                    if (!IsDeviceAuthorizedAsAdmin(device, _userDevicePermissionRepo))
                    {
                        return Forbid();
                    }

                    if (device.DeviceType != nameof(Applications.TEV) && device.DeviceType != nameof(Applications.TEV2))
                    {
                        return Forbid();
                    }
                    try
                        {
                            using (AmazonKinesisVideoClient kinesisClient = new AmazonKinesisVideoClient(awsAccessKeyId, awsSecretKey, RegionEndpoint.APSouth1))
                            {
                                var result = await GetHLSUrl(kinesisClient, awsAccessKeyId, awsSecretKey, deviceId, device.DeviceType);
                                _logger.LogInformation($"GetHLS API  Result :- {result}");
                                var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                                var diff = T2 - T1;
                                if (!string.IsNullOrEmpty(result))
                                {
                                    _logger.LogInformation($"Time taken in seconds for GetHLS API is {diff} and T1 is {T1} T2 is {T2}");
                                }

                            return Ok(new MMSHttpReponse<string>
                                {
                                   ResponseBody = result
                                });
                        }
                        }
                        catch (Exception ex)
                        {
                        _logger.LogError($"Exception occured while getting HLS url exception :- {ex}");
                        return Ok(new MMSHttpReponse<string>
                        {
                            ResponseBody = null
                        }); 

                        }
                }
                else
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while live streaming {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
            }
        }

        /// <summary>
        /// API to start video streaming, actual HLS URL will be provided by signal R
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("StartVideoForIos/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<VideoStreamingStartResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartVideoForIos([Required] string deviceId)
        {
             var T1 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                _logger.LogInformation($"LIVEO API called at {T1} for deviceid {deviceId}");
                var awsAccessKeyId = this._configuration.GetSection("liveStreaming").GetSection("awsAccessKeyId").Value;
                var awsSecretKey = this._configuration.GetSection("liveStreaming").GetSection("awsSecretKey").Value;
                var region = this._configuration.GetSection("liveStreaming").GetSection("awsRegion").Value;
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
            if (device != null)
            {
                if (!IsDeviceAuthorizedAsAdmin(device, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                if (device.DeviceType != nameof(Applications.TEV) && device.DeviceType != nameof(Applications.TEV2))
                {
                    return Forbid();
                }
            }

            var res = await PrepareLiveTreaming(device, deviceId, awsAccessKeyId, awsSecretKey, region);
            return res;
                      
        }

        [HttpGet("CheckUserIsAuthorized/{deviceId}")]
        public async Task<IActionResult> CheckUserIsAuthorized(string deviceId)
        {
            var device = await _deviceRepo.GetDevice(deviceId, OrgId);

            if (device == null)
            {
                return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
            }

            if (device.DeviceType != nameof(Applications.TEV) && device.DeviceType != nameof(Applications.TEV2))
            {
                return Forbid();
            }

            if (!IsDeviceAuthorizedAsAdmin(device, _userDevicePermissionRepo))
            {
                return Forbid();
            }

            return Ok();
        }

        /// <summary>
        /// API to get HLS url
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetHLSUrlForIos/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHLSUrlForIos([Required] string deviceId, int mode)
        {
            try
            {
                var T1 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                _logger.LogInformation($"GetHLS API called at :- {T1}");
                var awsAccessKeyId = this._configuration.GetSection("liveStreaming").GetSection("awsAccessKeyId").Value;
                var awsSecretKey = this._configuration.GetSection("liveStreaming").GetSection("awsSecretKey").Value;
                var region = this._configuration.GetSection("liveStreaming").GetSection("awsRegion").Value;

                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                if (device != null)
                {
                    //Commenting for Bug 1894
                    if (!IsDeviceAuthorizedAsAdmin(device, _userDevicePermissionRepo))
                    {
                        return Forbid();
                    }

                    if (device.DeviceType != nameof(Applications.TEV) && device.DeviceType != nameof(Applications.TEV2))
                    {
                        return Forbid();
                    }
                    try
                    {
                        using (AmazonKinesisVideoClient kinesisClient = new AmazonKinesisVideoClient(awsAccessKeyId, awsSecretKey, RegionEndpoint.APSouth1))
                        {
                            var result = await GetHLSUrlForSDCard(kinesisClient, awsAccessKeyId, awsSecretKey, deviceId, mode);
                            _logger.LogInformation($"GetHLS API  Result :- {result}");
                            var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                            var diff = T2 - T1;
                            if (!string.IsNullOrEmpty(result))
                            {
                                _logger.LogInformation($"Time taken in seconds for GetHLS API is {diff} and T1 is {T1} T2 is {T2}");
                            }

                            return Ok(new MMSHttpReponse<string>
                            {
                                ResponseBody = result
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception occured while getting HLS url exception :- {ex}");
                        return Ok(new MMSHttpReponse<string>
                        {
                            ResponseBody = null
                        });

                    }
                }
                else
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while live streaming {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
            }
        }

        /// <summary>
        /// API to stop SD Card video streaming
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("StopSDCardStreaming/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> StopSDCardStreaming([Required] string deviceId)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device is not found" });
                }

                if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return Forbid();
                }

                if (device.DeviceType != nameof(Applications.TEV) && device.DeviceType != nameof(Applications.TEV2))
                {
                    return Forbid();
                }
                try
                {
                    await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "live_stream_stop",5, _logger);
                   
                }
                catch (DeviceNotFoundException ex)
                {
                    _logger.LogError($"Stop Sd Card Stream Device not found for deviceid {deviceId} Exception :- {ex}");
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }
                catch (TimeoutException ex)
                {
                    _logger.LogError($"Stop Sd Card Stream Request time out for deviceid {deviceId} Exception :-  {ex}");
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains(":404103,"))
                    {
                        _logger.LogError($"PrepareLiveTreaming Device is not online for deviceid {deviceId} Exception :- {ex}");
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "Device is not online" });
                    }
                    if (ex.Message.Contains(":504101,"))
                    {
                        _logger.LogError($"PrepareLiveTreaming Request time out for deviceid {deviceId} Exception :- {ex}");
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                    }
                    _logger.LogError($"PrepareLiveTreaming Exception in DeviceValidationCheck for deviceid {deviceId} Exception :- {ex}");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                device.LiveStreaming.IsLiveStreaming = false;
                await _deviceRepo.UpdateDevice(OrgId, device);

                return Ok(new MMSHttpReponse { SuccessMessage = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while stopping live streaming {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "Error occured while stopping live stream" });
            }
        }

        /// <summary>
        /// Updates device Video configuration for HLS Live Streaming / SD Card Stream / Web RTC LiveStreaming
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("UpdateDeviceVideoConfiguration/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDeviceVideoConfiguration(string deviceId, [FromBody] UpdateDeviceVideoConfigurationRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest();
                }
                if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device is not found" });
                }
                
                switch (model.VideoMethod)
                {
                    case "hls_livestream_resolution":
                        try
                        {
                            await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "hls_livestream_resolution", 5, _logger,model.Resolution);
                            
                            await _iotHub.UpdateDeviceVideoResolution(device.Id, "hls_livestream_resolution", model.Resolution);
                                if (device != null)
                                {
                                    device.FeatureConfig.VideoResolution.hls = model.Resolution;
                                    await _deviceRepo.UpdateDevice(OrgId, device);
                                }
                        }
                        catch (DeviceNotFoundException ex)
                        {
                            _logger.LogError($"hls_livestream_resolution Device not found for deviceid {deviceId} Exception :- {ex}");
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                        }
                        catch (TimeoutException ex)
                        {
                            _logger.LogError($"hls_livestream_resolution Request time out for deviceid {deviceId} Exception :-  {ex}");
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains(":404103,"))
                            {
                                _logger.LogError($"hls_livestream_resolution Device is not online for deviceid {deviceId} Exception :- {ex}");
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Device is not online" });
                            }
                            if (ex.Message.Contains(":504101,"))
                            {
                                _logger.LogError($"hls_livestream_resolution Request time out for deviceid {deviceId} Exception :- {ex}");
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                            }
                            _logger.LogError($"hls_livestream_resolution Exception in DeviceValidationCheck for deviceid {deviceId} Exception :- {ex}");
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                        break;
                    case "webRtc_livestream_resolution":
                        try
                        {
                            await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "webRtc_livestream_resolution", 5, _logger, model.Resolution);
                          
                                await _iotHub.UpdateDeviceVideoResolution(device.Id, "webRtc_livestream_resolution", model.Resolution);
                                if (device != null)
                                {
                                    device.FeatureConfig.VideoResolution.webRtc = model.Resolution;
                                    await _deviceRepo.UpdateDevice(OrgId, device);
                                }
                           
                        }
                        catch (DeviceNotFoundException ex)
                        {
                            _logger.LogError($"webRtc_livestream_resolution Device not found for deviceid {deviceId} Exception :- {ex}");
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                        }
                        catch (TimeoutException ex)
                        {
                            _logger.LogError($"webRtc_livestream_resolution Request time out for deviceid {deviceId} Exception :-  {ex}");
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains(":404103,"))
                            {
                                _logger.LogError($"webRtc_livestream_resolution Device is not online for deviceid {deviceId} Exception :- {ex}");
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Device is not online" });
                            }
                            if (ex.Message.Contains(":504101,"))
                            {
                                _logger.LogError($"webRtc_livestream_resolution Request time out for deviceid {deviceId} Exception :- {ex}");
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                            }
                            _logger.LogError($"webRtc_livestream_resolution Exception in DeviceValidationCheck for deviceid {deviceId} Exception :- {ex}");
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                        break;
                    case "sdCard_stream_resolution":
                        try
                        {
                            await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "sdCard_stream_resolution", 5, _logger, model.Resolution);
                           
                                await _iotHub.UpdateDeviceVideoResolution(device.Id, "sdCard_stream_resolution", model.Resolution);
                                if (device != null)
                                {
                                    device.FeatureConfig.VideoResolution.sdCard = model.Resolution;
                                    await _deviceRepo.UpdateDevice(OrgId, device);
                                }
                           
                        }
                        catch (DeviceNotFoundException ex)
                        {
                            _logger.LogError($"sdCard_stream_resolution Device not found for deviceid {deviceId} Exception :- {ex}");
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                        }
                        catch (TimeoutException ex)
                        {
                            _logger.LogError($"sdCard_stream_resolution Request time out for deviceid {deviceId} Exception :-  {ex}");
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains(":404103,"))
                            {
                                _logger.LogError($"sdCard_stream_resolution Device is not online for deviceid {deviceId} Exception :- {ex}");
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Device is not online" });
                            }
                            if (ex.Message.Contains(":504101,"))
                            {
                                _logger.LogError($"sdCard_stream_resolution Request time out for deviceid {deviceId} Exception :- {ex}");
                                return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                            }
                            _logger.LogError($"sdCard_stream_resolution Exception in DeviceValidationCheck for deviceid {deviceId} Exception :- {ex}");
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                        break;
                    default:
                       _logger.LogError("Method Name {ex} not valid ", model.VideoMethod);
                        return null;
                }

                return Ok(new MMSHttpReponse { SuccessMessage = "Device configuration updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while updating device configuration {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }


        [HttpGet("GetVideoConfiguration/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<GetVideoConfigurationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetVideoConfiguration(string deviceId, [Required] string methodName)
        {
            try
            {
                #region IoTHub
                if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }
                #endregion

                #region Response
                GetVideoConfigurationResponse result = null;
                    var data = await _deviceRepo.GetDeviceFeatureConfiguration(deviceId, OrgId);
                    if (data == null)
                    {
                        return Ok(new MMSHttpReponse<GetVideoConfigurationResponse> { ResponseBody = result });
                    }
                string res = "";
                switch (methodName)
                {
                    case "hls_livestream_resolution":
                            res = data.VideoResolution.hls;
                        break;
                    case "webRtc_livestream_resolution":
                        res = data.VideoResolution.webRtc;
                        break;
                    case "sdCard_stream_resolution":
                        res = data.VideoResolution.sdCard;
                        break;
                    default:
                        return null;
                }
                    result = new GetVideoConfigurationResponse
                    {
                       
                            Resolution = res
                    };

                    return Ok(new MMSHttpReponse<GetVideoConfigurationResponse> { ResponseBody = result });
                    #endregion
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
        }

        private async Task<(long seqNumber, long autoStopSeqNumber)> ScheduleMsgToLiveStreamQueue(Device device)
        {
            IQueueClient queueClient = new QueueClient(this._configuration.GetSection("serviceBus").GetSection("connectionString").Value,
                this._configuration.GetSection("serviceBus").GetSection("liveStreamQueueName").Value);

            // Send a message to the queue to turn off live streaming automatically after specified time
            var autoStop = new LiveStreamQueueModel
            {
                DeviceId = device.Id,
                LogicalDeviceId = device.LogicalDeviceId,
                Message = "Stop live streaming",
                MessageCode = 9997,
                AzureFunctionRetryCount = 0,
                DeviceName = device.DeviceName,
                OrgId= Convert.ToInt32(OrgId)
            };
            var liveStreamingStopTime = Convert.ToInt32(_configuration.GetSection("liveStreaming").GetSection("LiveStreamingStopAfterMinutes").Value);
            Message autoStopStreamingMsg = new Message()
            {
                Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(autoStop)),
                ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddMinutes(liveStreamingStopTime)
            };
            var autoStopSeqNumber = await queueClient.ScheduleMessageAsync(autoStopStreamingMsg, new DateTimeOffset(DateTime.UtcNow.AddMinutes(liveStreamingStopTime)));

            long deviceOfflinemarkerSeqNumber = 0;

            if (Convert.ToDouble(device.CurrentFirmwareVersion) == 1.0)
            {
                // The below message a starts a timer and if the device does not start streaming in 20 seconds the device will be considered as offline.
                // Users will get a message that the device is offline
                var liveStreamQueueModel = new LiveStreamQueueModel
                {
                    DeviceId = device.Id,
                    LogicalDeviceId = device.LogicalDeviceId,
                    Message = "Device not reachable",
                    MessageCode = 9998,
                    AzureFunctionRetryCount = 0,
                    DeviceName = device.DeviceName,
                    AutoStopSequenceNumber = autoStopSeqNumber,
                    OrgId = Convert.ToInt32(OrgId)

                };

                Message msg = new Message()
                {
                    Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(liveStreamQueueModel)),
                    ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(20)
                };

                deviceOfflinemarkerSeqNumber = await queueClient.ScheduleMessageAsync(msg, new DateTimeOffset(DateTime.UtcNow.AddSeconds(20)));

            }
            return (deviceOfflinemarkerSeqNumber, autoStopSeqNumber);
        }



        //private async Task CreateAWSVideoStream(string awsAccessKeyId, string awsSecretKey, string deviceId)
        //{
        //    using (var kinesisClient = new AmazonKinesisVideoClient(awsAccessKeyId, awsSecretKey, RegionEndpoint.APSouth1))
        //    {
        //        try
        //        {
        //            var describeStreamReq = new DescribeStreamRequest();
        //            describeStreamReq.StreamName = deviceId;
        //            await kinesisClient.DescribeStreamAsync(describeStreamReq);
        //        }
        //        catch(Amazon.KinesisVideoArchivedMedia.Model.ResourceNotFoundException ex)
        //        {
        //            _logger.LogError("resource not found exception while creating the AWS stream for device {d}:- {ex}", deviceId, ex);
                  
        //        }
        //        catch(Exception ex)
        //        {
        //           if(ex.Message == "The requested stream is not found or not active.")
        //            {
        //                var createRequest = new CreateStreamRequest();
        //                createRequest.StreamName = deviceId;
        //                createRequest.MediaType = "video/h265";
        //                createRequest.DataRetentionInHours = 1;
        //                await kinesisClient.CreateStreamAsync(createRequest);
        //            }
        //            else
        //            {
        //                _logger.LogError("Exception while creating the AWS stream for device {d}:- {ex}", deviceId, ex);
        //                throw;
        //            }
                  
        //        }
        //    }
        //}

        private async Task<IActionResult> PrepareLiveTreaming(Device device, string deviceId, string awsAccessKeyId, string awsSecretKey, string region)
        {
            // var deviceReplica = await _deviceRepo.GetDevice(deviceId, OrgId);
            var seqNumbers = await ScheduleMsgToLiveStreamQueue(device);
          
                var liveStreamData = new
                {
                    stream = true,
                    stream_name = deviceId,
                    access_key = awsAccessKeyId,
                    secret_key = awsSecretKey,
                    aws_region = region,
                    servicebus_sequence_number = seqNumbers.seqNumber,
                    auto_stop_servicebus_seq_num = seqNumbers.autoStopSeqNumber
                };
          
            try
            {
                await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "live_stream_start",5, _logger, JsonConvert.SerializeObject(liveStreamData));
            }
            catch (DeviceNotFoundException ex)
            {
                if (device != null && device.LiveStreaming.IsLiveStreaming)
                {
                    device.LiveStreaming.IsLiveStreaming = false;
                    await _deviceRepo.UpdateDevice(OrgId, device);
                }
                _logger.LogError($"PrepareLiveTreaming Device not found for deviceid {deviceId} Exception :- {ex}");
                return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
            }
            catch (TimeoutException ex)
            {
                if (device != null && device.LiveStreaming.IsLiveStreaming)
                {
                    device.LiveStreaming.IsLiveStreaming = false;
                    await _deviceRepo.UpdateDevice(OrgId, device);
                }
                _logger.LogError($"PrepareLiveTreaming Request time out for deviceid {deviceId} Exception :-  {ex}");
                return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
            }
            catch (Exception ex)
            {
                if (device != null && device.LiveStreaming.IsLiveStreaming)
                {
                    device.LiveStreaming.IsLiveStreaming = false;
                    await _deviceRepo.UpdateDevice(OrgId, device);
                }
                if (ex.Message.Contains(":404103,"))
                {
                    _logger.LogError($"PrepareLiveTreaming Device is not online for deviceid {deviceId} Exception :- {ex}");
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Device is not online" });
                }
                if (ex.Message.Contains(":504101,"))
                {
                    _logger.LogError($"PrepareLiveTreaming Request time out for deviceid {deviceId} Exception :- {ex}");
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Request time out" });
                }
                _logger.LogError($"PrepareLiveTreaming Exception in DeviceValidationCheck for deviceid {deviceId} Exception :- {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            //Update Cosmos Device Data 

            if (device != null)
            {
                device.LiveStreaming.IsLiveStreaming = true;
                device.TwinChangeStatus = TwinChangeStatus.LiveStreaming;
                await _deviceRepo.UpdateDevice(OrgId, device);
            }
            var seqNumbers1 = await ScheduleMsgToLiveStreamQueue(device);
            var ret = new VideoStreamingStartResponse();
            ret.StopSequenceNumber = seqNumbers.autoStopSeqNumber;
            ret.SignalRNegotiateUrl = $"{_configuration.GetSection("liveStreaming").GetSection("SignalRNegotiateBaseUrl").Value}{deviceId}";
            return Ok(new MMSHttpReponse<VideoStreamingStartResponse> { ResponseBody = ret });
        }

        //private void UpdateTimeInSQL(string deviceId)
        //{
        //    var liveStreamObj = _liveStreamRepo.Query(x => x.LogicalDeviceId == deviceId).FirstOrDefault();

        //    if (liveStreamObj != null)
        //    {
        //        if (liveStreamObj.StartedUTC.Month != DateTime.UtcNow.Month) // record exist for previous month, reset total time
        //        {
        //            using (var transaction = _unitOfWork.BeginTransaction())
        //            {
        //                try
        //                {
        //                    liveStreamObj.SecondsLiveStreamed = 0;
        //                    _liveStreamRepo.Update(liveStreamObj);
        //                    _liveStreamRepo.SaveChanges();
        //                    transaction.Commit();
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    _logger.LogError("Exception while live streaming {ex}", ex);
        //                }
        //            }
        //        }
        //    }
        //}

        private async Task<string> GetHLSUrl(AmazonKinesisVideoClient kinesisClient, string awsAcceccKeyId, string awsSecretKey, string deviceId, string deviceType = null)
        {
            try
            {
                var urlReq = new GetDataEndpointRequest();
                urlReq.APIName = APIName.GET_HLS_STREAMING_SESSION_URL;
                urlReq.StreamName = deviceId;
                var T1 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                var url = await kinesisClient.GetDataEndpointAsync(urlReq);
               
             if(deviceType == nameof(Applications.TEV2))
                {
                    using (var videoArchivedMediaClient = new Amazon.KinesisVideoArchivedMedia.AmazonKinesisVideoArchivedMediaClient(awsAcceccKeyId, awsSecretKey, url.DataEndpoint))
                    {
                        var reqBody = new GetHLSStreamingSessionURLRequest();
                        //reqBody.PlaybackMode = HLSPlaybackMode.ON_DEMAND;
                        reqBody.PlaybackMode = HLSPlaybackMode.LIVE;
                        //reqBody.MaxMediaPlaylistFragmentResults = 5000;
                        reqBody.StreamName = deviceId;
                        reqBody.HLSFragmentSelector = new HLSFragmentSelector()
                        {
                            FragmentSelectorType = HLSFragmentSelectorType.PRODUCER_TIMESTAMP//,

                            //TimestampRange = new HLSTimestampRange { StartTimestamp = DateTime.Now.AddMinutes(-5), EndTimestamp = DateTime.Now.AddMinutes(7) }
                        };
                        //reqBody.MaxMediaPlaylistFragmentResults = 3;
                        reqBody.Expires = 7 * 60; // Keep expired to seven minutes
                                                  //reqBody.DiscontinuityMode = HLSDiscontinuityMode.ON_DISCONTINUITY;
                        var HLSURL = await videoArchivedMediaClient.GetHLSStreamingSessionURLAsync(reqBody);
                        var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                        var diff = T2 - T1;
                        _logger.LogInformation($"Time taken in seconds for GetHLSUrl is {diff} and T1 is {T1} T2 is {T2} for deviceid {deviceId}");
                        return HLSURL.HLSStreamingSessionURL;

                    }
                }
             else
                {
                    using (var videoArchivedMediaClient = new Amazon.KinesisVideoArchivedMedia.AmazonKinesisVideoArchivedMediaClient(awsAcceccKeyId, awsSecretKey, url.DataEndpoint))
                    {
                        var reqBody = new GetHLSStreamingSessionURLRequest();
                        reqBody.PlaybackMode = HLSPlaybackMode.LIVE;
                        reqBody.StreamName = deviceId;
                        reqBody.Expires = 7 * 60; // Keep expired to seven minutes
                        var HLSURL = await videoArchivedMediaClient.GetHLSStreamingSessionURLAsync(reqBody);
                        var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                        var diff = T2 - T1;
                        _logger.LogInformation($"Time taken in seconds for GetHLSUrl is {diff} and T1 is {T1} T2 is {T2} for deviceid {deviceId}");
                        return HLSURL.HLSStreamingSessionURL;
                    }
                }
                
                
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error occured while getting HLS url for deviceid {deviceId} Exception :- {ex}");
                return null;
            }
        }

        private async Task<string> GetHLSUrlForSDCard(AmazonKinesisVideoClient kinesisClient, string awsAcceccKeyId, string awsSecretKey, string deviceId, int mode,string deviceType = null)
        {
            try
            {
                var urlReq = new GetDataEndpointRequest();
                urlReq.APIName = APIName.GET_HLS_STREAMING_SESSION_URL;
                urlReq.StreamName = deviceId;
                var T1 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                var url = await kinesisClient.GetDataEndpointAsync(urlReq);

                if (deviceType == nameof(Applications.TEV2))
                {
                    using (var videoArchivedMediaClient = new Amazon.KinesisVideoArchivedMedia.AmazonKinesisVideoArchivedMediaClient(awsAcceccKeyId, awsSecretKey, url.DataEndpoint))
                    {
                        var reqBody = new GetHLSStreamingSessionURLRequest();
                        //reqBody.PlaybackMode = HLSPlaybackMode.ON_DEMAND;
                        reqBody.PlaybackMode = HLSPlaybackMode.LIVE;
                        //reqBody.MaxMediaPlaylistFragmentResults = 5000;
                        reqBody.StreamName = deviceId;
                        reqBody.HLSFragmentSelector = new HLSFragmentSelector()
                        {
                            FragmentSelectorType = HLSFragmentSelectorType.SERVER_TIMESTAMP//,

                            //TimestampRange = new HLSTimestampRange { StartTimestamp = DateTime.Now.AddMinutes(-5), EndTimestamp = DateTime.Now.AddMinutes(7) }
                        };
                        //reqBody.MaxMediaPlaylistFragmentResults = 3;
                        reqBody.Expires = 7 * 60; // Keep expired to seven minutes
                                                   reqBody.DiscontinuityMode = mode == 1 ? HLSDiscontinuityMode.ALWAYS : mode == 2 ? HLSDiscontinuityMode.ON_DISCONTINUITY : HLSDiscontinuityMode.NEVER;
                        var HLSURL = await videoArchivedMediaClient.GetHLSStreamingSessionURLAsync(reqBody);
                        var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                        var diff = T2 - T1;
                        _logger.LogInformation($"Time taken in seconds for GetHLSUrl is {diff} and T1 is {T1} T2 is {T2} for deviceid {deviceId}");
                        return HLSURL.HLSStreamingSessionURL;

                    }
                }
                else
                {
                    using (var videoArchivedMediaClient = new Amazon.KinesisVideoArchivedMedia.AmazonKinesisVideoArchivedMediaClient(awsAcceccKeyId, awsSecretKey, url.DataEndpoint))
                    {
                        var reqBody = new GetHLSStreamingSessionURLRequest();
                        reqBody.PlaybackMode = HLSPlaybackMode.LIVE;
                        reqBody.StreamName = deviceId;
                        reqBody.Expires = 7 * 60; // Keep expired to seven minutes
                        var HLSURL = await videoArchivedMediaClient.GetHLSStreamingSessionURLAsync(reqBody);
                        var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                        var diff = T2 - T1;
                        _logger.LogInformation($"Time taken in seconds for GetHLSUrl is {diff} and T1 is {T1} T2 is {T2} for deviceid {deviceId}");
                        return HLSURL.HLSStreamingSessionURL;
                    }
                }


            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occured while getting HLS url for deviceid {deviceId} Exception :- {ex}");
                return null;
            }
        }

    }




}