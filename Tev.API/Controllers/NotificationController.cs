using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMSConstants;
using Tev.API.Models;
using Microsoft.Extensions.Logging;
using Tev.DAL.RepoContract;
using Tev.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Tev.Cosmos;
using Tev.Cosmos.Entity;
using Tev.IotHub;
using System.ComponentModel.DataAnnotations;
using Tev.Cosmos.IRepository;

namespace Tev.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "IMPACT")]
    public class NotificationController : TevControllerBase
    {
       
        private readonly ILogger<NotificationController> _logger;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly IConfiguration _config;
        private readonly IDeviceSetupRepo _deviceSetupRepo;
        private readonly IDeviceRepo _deviceRepo;
        private readonly ITevIoTRegistry _iotHub;

        public NotificationController(ILogger<NotificationController> logger, IGenericRepo<UserDevicePermission> userDevicePermissionRepo,
            IConfiguration config, IDeviceSetupRepo deviceSetupRepo, ITevIoTRegistry iotHub, IDeviceRepo deviceRepo)
        {
           
            _logger = logger;
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _config = config;
            _deviceSetupRepo = deviceSetupRepo;
            _deviceRepo = deviceRepo;
            _iotHub = iotHub;
        }

        /// <summary>
        /// Subscribe to firebase push notification
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("subscribe")]
        public async Task<IActionResult> SubscribeToTopic([FromBody] FirebaseTopicRequest reqBody)
        {
            try
            {
                if(reqBody != null)
                {
                    var listOfTopic = new List<string>();
                    if (!IsOrgAdmin(reqBody.Application))
                    {
                        // Get device permission list and subscribe to deviceId topic 
                        var deviceIds = _userDevicePermissionRepo.Query(x => x.UserEmail == UserEmail).Select(x => x.DeviceId).ToList();
                        listOfTopic.AddRange(deviceIds);
                    }
                    else
                    {
                        listOfTopic.Add(OrgId);
                    }
                    var topicResponse = new List<TopicManagementResponse>();
                    foreach (var deviceId in listOfTopic)
                    {
                        _logger.LogInformation("Adding FCM token to topic {topic}", deviceId);
                        var result = await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(new List<string> { reqBody.FCMToken }, deviceId);
                        topicResponse.Add(result);
                    }
                    if (topicResponse.Any(x => x.FailureCount > 0))
                    {
                        topicResponse.ForEach(x =>
                        {
                            foreach (var error in x.Errors)
                            {
                                _logger.LogError($"Error while subscribing to FCM {error.Reason}");
                            }
                        });
                        return BadRequest(new MMSHttpReponse<List<TopicManagementResponse>> { ResponseBody = topicResponse });
                       
                    }
                    else
                    {
                        return Ok(new MMSHttpReponse { SuccessMessage = "Subscribed to device topic succesfully" });
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
              
                
            }
            catch ( Exception ex)
            {

                _logger.LogError("Error while registering to firebase topics {exception} {Application}", ex, ApplicationNames.TEV);
                return BadRequest(new MMSHttpReponse { ErrorMessage = ex.Message + " " + ex.InnerException?.Message });
            }
            
        }

        /// <summary>
        /// UnSubscribe to firebase push notification
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("unsubscribe")]
        public async Task<IActionResult> UnSubscribeToTopic([FromBody] FirebaseTopicRequest reqBody)
        {
            try
            {
                if (reqBody != null)
                {
                    var listOfTopic = new List<string>();
                    if (!IsOrgAdmin(reqBody.Application))
                    {
                        // Get device permission list and subscribe to deviceId topic 
                        var deviceIds = _userDevicePermissionRepo.Query(x => x.UserEmail == UserEmail).Select(x => x.DeviceId).ToList();
                        listOfTopic.AddRange(deviceIds);
                    }
                    else 
                    {
                        listOfTopic.Add(OrgId);
                    }
                    var topicResponse = new List<TopicManagementResponse>();
                    foreach (var deviceId in listOfTopic)
                    {
                        _logger.LogInformation("Unsubscribing FCM token from topic {topic}", deviceId);
                        var result = await FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(new List<string> { reqBody.FCMToken }, deviceId);
                        topicResponse.Add(result);
                    }

                    // even though this is an error return 200 to the client so that client can logout. Log the error
                    if (topicResponse.Any(x => x.FailureCount > 0))
                    {
                        topicResponse.ForEach(x =>
                        {
                            foreach (var error in x.Errors)
                            {
                                _logger.LogError($"Error while unsubscribing from FCM {error.Reason}");
                            }
                        });
                       
                        return Ok(new MMSHttpReponse { SuccessMessage = "UnSubscribed to device topic succesfully" });
                    }
                    else
                    {
                        return Ok(new MMSHttpReponse { SuccessMessage = "UnSubscribed to device topic succesfully" });
                    }
                }
                else
                {
                    return Ok(new MMSHttpReponse { SuccessMessage = "UnSubscribed to device topic succesfully" });
                }

              

            }
            catch (Exception ex)
            {

                _logger.LogError("Error while unsubscribing to firebase topics {exception} {Application}", ex, ApplicationNames.TEV);
                return Ok(new MMSHttpReponse { SuccessMessage = "UnSubscribed to device topic succesfully" });
            }

        }

        /// <summary>
        /// Starts the device setup process
        /// </summary>
        /// <param name="logicalDeviceId"></param>
        /// <returns></returns>
        [HttpGet("deviceSetup")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeviceSetupInit([Required]string logicalDeviceId)
        {

            try
            {
                if (IsOrgAdmin(CurrentApplications))
                {
                    var connectionString = _config.GetSection("serviceBus").GetSection("connectionString").Value;
                    var queueName = _config.GetSection("serviceBus").GetSection("queueName").Value;
                    var message = new
                    {
                        messageCode = 0,
                        logicalDeviceId = logicalDeviceId,
                        orgId = OrgId
                    };
                    await using (ServiceBusClient client = new ServiceBusClient(connectionString))
                    {
                        ServiceBusSender sender = client.CreateSender(queueName);
                        ServiceBusMessage msg = new ServiceBusMessage(JsonConvert.SerializeObject(message));
                        msg.SessionId = logicalDeviceId;
                        await sender.SendMessageAsync(msg);
                        _logger.LogInformation("Device setup init signal sent");
                    }
                     return Ok(new MMSHttpReponse { SuccessMessage = "Device setup initiated" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
               
            }
            catch (Exception ex)
            {

                _logger.LogError("Error initializing device setup notification {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);

            }
        }

        /// <summary>
        /// Get the device setup status
        /// </summary>
        /// <param name="logicalDeviceId"></param>
        /// <param name="deviceType"></param>
        /// <returns></returns>
        [HttpGet("deviceSetupStatus")]
        [ProducesResponseType(typeof(MMSHttpReponse<DeviceSetupStatusResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeviceSetupStatus([Required]string logicalDeviceId,string deviceType)
        {
            
            try
            {
                if(deviceType == "TEV2")
                {
                    var result = await _deviceSetupRepo.GetDeviceSetupStatus(logicalDeviceId, OrgId);
                    if (result != null)
                    {
                        var res = new DeviceSetupStatusResponse
                        {
                            LogicalDeviceId = result.LogicalDeviceId,
                            Status = result.Status,
                            RetryCount = result.RetryCount,
                            Retrying = result.Retrying,
                            DeviceName = result.DeviceName,
                            Message = result.Message,
                            MessageCode = result.MessageCode
                        };
                        if ( deviceType == "TEV2" && result.Status == Status.Complete)
                        {
                            //res.Message = "Smart AI Camera setup successfull.";
                            var device = await _deviceRepo.GetDevice(logicalDeviceId, OrgId);
                            var latestAvilableFirmware = await _deviceRepo.GetLatestFirmwareVersion(device.DeviceType);
                            if (Convert.ToDouble(device.CurrentFirmwareVersion) < Convert.ToDouble(latestAvilableFirmware))
                            {
                                var isUpdate = await _iotHub.UpdateFirmware(device.LogicalDeviceId);

                                //Update Cosmos Device Data
                                if (device != null)
                                {
                                    device.Firmware.UserApproved = true;
                                    device.TwinChangeStatus = TwinChangeStatus.DesiredPropFirmwareUpdate;
                                    await _deviceRepo.UpdateDevice(OrgId, device);
                                }
                                if (isUpdate)
                                {
                                    res.Message = $"Smart AI Camera setup successfull.Device firmware upgrade started to new firmware {latestAvilableFirmware} ";
                                }
                                else
                                {
                                    _logger.LogError("Error while updating firmware update deive twin, isUpdate valud is {0}", isUpdate);
                                }
                            }
                        }
                        return Ok(new MMSHttpReponse<DeviceSetupStatusResponse> { ResponseBody = res });
                    }
                    else
                    {
                        var res = new DeviceSetupStatusResponse
                        {
                            Status = Status.InProgress,
                            Message = deviceType != null && deviceType == "TEV2" ? "Waiting for Smart AI Camera to connect to internet" : "Waiting for Smart AI Supervision to connect to internet",
                            MessageCode = 0
                        };

                        return Ok(new MMSHttpReponse<DeviceSetupStatusResponse> { ResponseBody = res });
                    }
                }
                else if(deviceType == "WSD")
                {

                    var result = await _deviceSetupRepo.GetDeviceSetupStatus(logicalDeviceId, OrgId);
                    if (result != null)
                    {
                        var res = new DeviceSetupStatusResponse
                        {
                            LogicalDeviceId = result.LogicalDeviceId,
                            Status = result.Status,
                            RetryCount = result.RetryCount,
                            Retrying = result.Retrying,
                            DeviceName = result.DeviceName,
                            Message = result.Status == Status.Error ? $"Error occured while setting up {result.DeviceName}" : result.Message,
                            MessageCode = result.MessageCode
                        };
                      
                        return Ok(new MMSHttpReponse<DeviceSetupStatusResponse> { ResponseBody = res });
                    }
                    else
                    {
                        var res = new DeviceSetupStatusResponse
                        {
                            Status = Status.InProgress,
                            Message = "WSD setup is in Progress",
                            MessageCode = 0
                        };

                        return Ok(new MMSHttpReponse<DeviceSetupStatusResponse> { ResponseBody = res });
                    }
                }
                else
                {
                    var result = await _deviceSetupRepo.GetDeviceSetupStatus(logicalDeviceId, OrgId);
                    if (result != null)
                    {
                        var res = new DeviceSetupStatusResponse
                        {
                            LogicalDeviceId = result.LogicalDeviceId,
                            Status = result.Status,
                            RetryCount = result.RetryCount,
                            Retrying = result.Retrying,
                            DeviceName = result.DeviceName,
                            Message = result.Status == Status.Error ? $"Error occured while setting up {result.DeviceName}" : result.Message,
                            MessageCode = result.MessageCode
                        };
                        if (res.Message == "Smart AI Supervision setup successfull." && deviceType == "TEV2")
                        {
                            res.Message = "Smart AI Camera setup successfull.";
                        }
                        return Ok(new MMSHttpReponse<DeviceSetupStatusResponse> { ResponseBody = res });
                    }
                    else
                    {
                        var res = new DeviceSetupStatusResponse
                        {
                            Status = Status.InProgress,
                            Message = deviceType != null && deviceType == "TEV2" ? "Waiting for Smart AI Camera to connect to internet" : "Waiting for Smart AI Supervision to connect to internet",
                            MessageCode = 0
                        };

                        return Ok(new MMSHttpReponse<DeviceSetupStatusResponse> { ResponseBody = res });
                    }
                }
             

            }
            catch (Exception ex)
            {

                _logger.LogError("Error getting device setup status {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            
        }
    }
}
