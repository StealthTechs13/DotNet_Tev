using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMSConstants;
using Microsoft.Extensions.Logging;
using Tev.API.Enums;
using Tev.API.Models;
using Tev.IotHub;
using Tev.IotHub.Models;
using Tev.Cosmos;
using Tev.DAL.HelperService;
using Tev.DAL.RepoContract;
using Tev.DAL.Entities;
using System.Globalization;
using Tev.DAL;
using ZohoSubscription;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Tev.Cosmos.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Devices.Client.Exceptions;
using Azure.Storage.Blobs;
using Tev.Cosmos.Entity;
using ZohoSubscription.Models;
using Amazon.KinesisVideo;
using Amazon;
using Amazon.KinesisVideo.Model;

namespace Tev.API.Controllers
{
    /// <summary>
    /// APIs for device management
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class DevicesController : TevControllerBase
    {

        private readonly ITevIoTRegistry _iotHub;
        private readonly ILogger<DevicesController> _logger;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAlertRepo _alertRepo;
        private readonly IGeneralRepo _generalRepo;
        private readonly IUserDevicePermissionService _userDevicePermissionService;
        private readonly IGenericRepo<Location> _locationRepo;
        private readonly IZohoSubscription _zohoSubscription;
        private readonly IZohoAuthentication _zohoAuthentication;
        private readonly IConfiguration _configuration;
        private readonly IPeopleCountRepo _peoplecountRepo;
        private readonly IGenericRepo<ZohoSubscriptionHistory> _subscriptionRepo;
        private readonly IGenericRepo<DeviceDetachedHistory> _deviceDetachedRepo;
        private readonly IGenericRepo<DeviceReplacement> _deviceReplacementRepo;
        private readonly IGenericRepo<FeatureSubscriptionAssociation> _featureSubscriptionRepo;
        private readonly IDeviceRepo _deviceRepo;
        private readonly IGenericRepo<QRAuthCode> _qrAuthCodeRepo;
        private readonly IGenericRepo<SRTRoutes> _srtRoutesRepo;

        public DevicesController(ITevIoTRegistry iotHub, IAlertRepo alertRepo, ILogger<DevicesController> logger, IGeneralRepo generalRepo,
                                IUserDevicePermissionService userDevicePermissionService,
                                IGenericRepo<UserDevicePermission> userDevicePermissionRepo, IGenericRepo<Location> locationRepo, IUnitOfWork unitOfWork,
                                IZohoSubscription zohoSubscription, IZohoAuthentication zohoAuthentication, IConfiguration config, IPeopleCountRepo peopleCountRepo,
                                IGenericRepo<ZohoSubscriptionHistory> subscriptionRepo, IGenericRepo<DeviceDetachedHistory> deviceDetachedRepo,
                                IGenericRepo<DeviceReplacement> deviceReplacementRepo, IGenericRepo<FeatureSubscriptionAssociation> featureSubscriptionRepo,
                                IDeviceRepo deviceRepo, IGenericRepo<QRAuthCode> qrAuthCodeRepo, IGenericRepo<SRTRoutes> srtRoutesRepo)
        {


            _iotHub = iotHub;
            _logger = logger;
            _alertRepo = alertRepo;
            _generalRepo = generalRepo;
            _userDevicePermissionService = userDevicePermissionService;
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _unitOfWork = unitOfWork;
            _locationRepo = locationRepo;
            _zohoSubscription = zohoSubscription;
            _zohoAuthentication = zohoAuthentication;
            _configuration = config;
            _peoplecountRepo = peopleCountRepo;
            _subscriptionRepo = subscriptionRepo;
            _deviceDetachedRepo = deviceDetachedRepo;
            _deviceReplacementRepo = deviceReplacementRepo;
            _featureSubscriptionRepo = featureSubscriptionRepo;
            _deviceRepo = deviceRepo;
            _qrAuthCodeRepo = qrAuthCodeRepo;
            _srtRoutesRepo = srtRoutesRepo;
        }


        /// <summary>
        /// Adds a device to the org heirarchy
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("addDevice")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddDevice([FromBody] AddDeviceRequest reqBody)
        {
            try
            {
                if (reqBody == null)
                {
                    return BadRequest();
                }

                // When adding device directly to a new location, the new location get attached to the org..
                // Only orgadmin can add a new location to the org
                if (!IsOrgAdmin(reqBody.Application))
                {
                    return Forbid();
                }
                if (reqBody.IsNewSite)
                {
                    using (var transaction = _unitOfWork.BeginTransaction())
                    {
                        try
                        {
                            var location = new Location
                            {
                                CreatedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds(),
                                OrgId = OrgId,
                                CreatedBy = UserEmail,
                                Name = reqBody.SiteName,
                                Id = reqBody.SiteId.ToString()
                            };
                            await _locationRepo.AddAsync(location);
                            _locationRepo.SaveChanges();
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {


                            transaction.Rollback();
                            _logger.LogError("Error Occured on Create User Device Permission on Sql {exception}", ex);
                            return StatusCode(StatusCodes.Status500InternalServerError);

                        }
                    }
                }
                //if(reqBody.DeviceType == "TEV2")
                //{
                //    var device = await _deviceRepo.GetDevice(reqBody.DeviceId.ToString(), OrgId);

                //    if (device == null)
                //    {
                //        return NotFound(new MMSHttpReponse { ErrorMessage = "Device is not found" });
                //    }

                //    if (device != null)
                //    {
                //        device.DeviceName = reqBody.DeviceName;
                //        device.DeviceType = reqBody.DeviceType;
                //        device.LocationId = reqBody.SiteId.ToString();
                //        device.LocationName = reqBody.SiteName;
                //        await _deviceRepo.UpdateDevice(OrgId, device);
                //    }
                //}

                if (reqBody.DeviceType == "TEV" || reqBody.DeviceType == "TEV2")
                {
                    var awsAccessKeyId = this._configuration.GetSection("liveStreaming").GetSection("awsAccessKeyId").Value;
                    var awsSecretKey = this._configuration.GetSection("liveStreaming").GetSection("awsSecretKey").Value;
                    var region = this._configuration.GetSection("liveStreaming").GetSection("awsRegion").Value;
                    using (var kinesisClient = new AmazonKinesisVideoClient(awsAccessKeyId, awsSecretKey, RegionEndpoint.APSouth1))
                    {
                        try
                        {
                            var createRequest = new CreateStreamRequest();
                            createRequest.StreamName = reqBody.DeviceId.ToString();
                            createRequest.MediaType = reqBody.DeviceType == "TEV2" ? "video/h265" : "video/h264";
                            createRequest.DataRetentionInHours = 1;
                            await kinesisClient.CreateStreamAsync(createRequest);
                        }
                        catch (Exception ex)
                        {

                            _logger.LogError("Exception while creating the AWS stream for device {d}:- {ex}", reqBody.DeviceId, ex);
                            return Ok(new MMSHttpReponse { SuccessMessage = "Device added successfully" });
                        }
                    }
                }


                return Ok(new MMSHttpReponse { SuccessMessage = "Device added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while adding device to gremlin {exception}", ex);
                return BadRequest(new MMSHttpReponse { ErrorMessage = ex.Message + " " + ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Gets all devices across all locations...
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(MMSHttpReponse<List<DeviceResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDevices([FromQuery] DevicePermissionEnum? permission)
        {
            try
            {
                var newfirmwareVersionTev1 = _deviceRepo.GetLatestFirmwareVersion("TEV").GetAwaiter().GetResult();
                var newfirmwareVersionTev2 = _deviceRepo.GetLatestFirmwareVersion("TEV2").GetAwaiter().GetResult();
                // OrgAdmin is authorized to see all the devices
                if (IsOrgAdmin(CurrentApplications))
                {
                    var result = await _deviceRepo.GetDeviceByOrgId(OrgId);

                    var ret = result.Select(x => MapDTO(x, newfirmwareVersionTev1, newfirmwareVersionTev2)).OrderByDescending(x => x.CreatedOn).ToList();
                    var allDeviceIdsForOrgAdmin = ret.Select(z => z.Id).ToList();
                    var allPermissions = _userDevicePermissionRepo.Query(z => allDeviceIdsForOrgAdmin.Contains(z.DeviceId)).ToList();
                    foreach (var res in ret)
                    {
                        res.CurrentUserPermission = DevicePermissionEnum.Owner.GetName();
                        res.DevicePermissions = allPermissions.Where(z => z.DeviceId == res.Id).Select(z => new DevicePermission()
                        {
                            UserEmail = z.UserEmail,
                            Permission = z.DevicePermission.GetName()
                        }).ToList();
                    }
                    return Ok(new MMSHttpReponse<List<DeviceResponse>> { ResponseBody = ret });
                }
                else
                {
                    var permittedDevicesWithPermission = _userDevicePermissionService.GetDeviceIdAndPermission(UserEmail, permission);
                    var permittedDevices = permittedDevicesWithPermission.Select(z => z.DeviceId).ToList();
                    if (permittedDevices.Count == 0)
                    {
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "Sorry, you don't have any permission, please contact your admin" });
                    }
                    var result = await _deviceRepo.GetDeviceByDeviceIds(permittedDevices, OrgId);
                    var ret = result.Select(x => MapDTO(x, newfirmwareVersionTev1, newfirmwareVersionTev2)).OrderByDescending(x => x.CreatedOn).ToList();

                    var allDeviceIdsForUser = result.Select(z => z.LogicalDeviceId).ToList();

                    var allPermissions = _userDevicePermissionRepo.Query(z => allDeviceIdsForUser.Contains(z.DeviceId)).ToList();
                    foreach (var res in ret)
                    {
                        res.CurrentUserPermission = permittedDevicesWithPermission.Where(z => z.DeviceId == res.Id).Select(z => z.DevicePermission.GetName()).FirstOrDefault();
                        res.DevicePermissions = allPermissions.Where(z => z.DeviceId == res.Id).Select(z => new DevicePermission()
                        {
                            UserEmail = z.UserEmail,
                            Permission = z.DevicePermission.GetName()
                        }).ToList();
                    }
                    _logger.LogInformation("Devices returned succesfully");
                    return Ok(new MMSHttpReponse<List<DeviceResponse>> { ResponseBody = ret });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while retrieving device for User {UserEmail} and application {CurrentApplications}  Exception :- {exception}", UserEmail, CurrentApplications, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }


        /// <summary>
        /// Gets TEV2 devices across all locations
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetTev2AllDevices")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<Tev2DeviceResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTev2AllDevices([FromQuery] DevicePermissionEnum? permission)
        {
            try
            {
                var newfirmwareVersionTev2 = _deviceRepo.GetLatestFirmwareVersion("TEV2").GetAwaiter().GetResult();
                // OrgAdmin is authorized to see all the devices
                if (IsOrgAdmin(CurrentApplications))
                {
                    var result = await _deviceRepo.GetTev2DeviceByOrgId(OrgId);
                    var ret = result.Select(x => MapTevDTO(x, newfirmwareVersionTev2)).OrderByDescending(x => x.CreatedOn).ToList();
                    var allDeviceIdsForOrgAdmin = ret.Select(z => z.Id).ToList();
                    var allPermissions = _userDevicePermissionRepo.Query(z => allDeviceIdsForOrgAdmin.Contains(z.DeviceId)).ToList();
                    foreach (var res in ret)
                    {
                        res.CurrentUserPermission = DevicePermissionEnum.Owner.GetName();
                        res.DevicePermissions = allPermissions.Where(z => z.DeviceId == res.Id).Select(z => new DevicePermission()
                        {
                            UserEmail = z.UserEmail,
                            Permission = z.DevicePermission.GetName()
                        }).ToList();
                    }
                    return Ok(new MMSHttpReponse<List<Tev2DeviceResponse>> { ResponseBody = ret });
                }
                else
                {
                    var permittedDevicesWithPermission = _userDevicePermissionService.GetDeviceIdAndPermission(UserEmail, permission);
                    var permittedDevices = permittedDevicesWithPermission.Select(z => z.DeviceId).ToList();
                    if (permittedDevices.Count == 0)
                    {
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "Sorry, you don't have any permission, please contact your admin" });
                    }
                    var result = await _deviceRepo.GetTev2DeviceByDeviceIds(permittedDevices, OrgId);
                    var ret = result.Select(x => MapTevDTO(x, newfirmwareVersionTev2)).OrderByDescending(x => x.CreatedOn).ToList();
                    var allDeviceIdsForUser = result.Select(z => z.LogicalDeviceId).ToList();

                    var allPermissions = _userDevicePermissionRepo.Query(z => allDeviceIdsForUser.Contains(z.DeviceId)).ToList();
                    foreach (var res in ret)
                    {
                        res.CurrentUserPermission = permittedDevicesWithPermission.Where(z => z.DeviceId == res.Id).Select(z => z.DevicePermission.GetName()).FirstOrDefault();
                        res.DevicePermissions = allPermissions.Where(z => z.DeviceId == res.Id).Select(z => new DevicePermission()
                        {
                            UserEmail = z.UserEmail,
                            Permission = z.DevicePermission.GetName()
                        }).ToList();
                    }
                    _logger.LogInformation("Devices returned succesfully");
                    return Ok(new MMSHttpReponse<List<Tev2DeviceResponse>> { ResponseBody = ret });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while retrieving device for User {UserEmail} and application {CurrentApplications}  Exception :- {exception}", UserEmail, CurrentApplications, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// Gets devices specific to a location
        /// </summary>
        /// <param name="id">The location id</param>
        /// <returns></returns>
        [HttpGet("location/{id}")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<DeviceResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDevicesForLocation(string id)
        {
            try
            {
                var siteList = new List<string>();

                siteList.Add(id);

                var result = await _deviceRepo.GetDevicesByLocation(id, OrgId);

                var newfirmwareVersionTev1 = _deviceRepo.GetLatestFirmwareVersion("TEV").GetAwaiter().GetResult();
                var newfirmwareVersionTev2 = _deviceRepo.GetLatestFirmwareVersion("TEV2").GetAwaiter().GetResult();

                var ret = result.Select(x => MapDTO(x, newfirmwareVersionTev1, newfirmwareVersionTev2)).OrderByDescending(x => x.CreatedOn).ToList();

                if (!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2))
                {
                    var permittedDevices = _userDevicePermissionService.GetDeviceIdForViewer(UserEmail);
                    ret = ret.Where(z => permittedDevices.Contains(z.Id)).ToList();

                    var allDeviceIdsForUser = ret.Select(z => z.Id).ToList();

                    var allPermissions = _userDevicePermissionRepo.Query(z => allDeviceIdsForUser.Contains(z.DeviceId));

                    foreach (var res in ret)
                    {
                        res.CurrentUserPermission = allPermissions.Where(z => z.DeviceId == res.Id).Select(z => z.DevicePermission.GetName()).AsEnumerable().FirstOrDefault();
                        res.DevicePermissions = allPermissions.Where(z => z.DeviceId == res.Id).Select(z => new DevicePermission()
                        {
                            UserEmail = z.UserEmail,
                            Permission = z.DevicePermission.GetName()
                        }).ToList();
                    }
                    ret.RemoveAll(r => r.Name == null);
                }

                return Ok(new MMSHttpReponse<List<DeviceResponse>> { ResponseBody = ret });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while retrieving device {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete a device by passing device id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("DeleteDevice/{id}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDevice(string id)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(id, OrgId);

                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device does not exist" });
                }

                if (!IsDeviceAuthorizedAsAdmin(device, _userDevicePermissionRepo))
                {
                    return Forbid();
                }

                var data = _deviceReplacementRepo.Query(z => z.DeviceId == id && z.ReplaceStatus != ReplaceStatusEnum.Closed).OrderByDescending(x => x.CreatedDate).FirstOrDefault();

                if (data != null)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = $"replacement request exist for the device and the current status is {data.ReplaceStatus.GetName()}" });
                }

                var token = await _zohoAuthentication.GetZohoToken();

                var subcriptionDetails = new RetrieveSubscriptionDetailsResponse();

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var deviceDetachedHistory = new DeviceDetachedHistory
                        {
                            LogicalDetachedDeviceId = device.LogicalDeviceId,
                            PhysicalDetachedDeviceId = device.Id,
                            OrgId = OrgId
                        };

                        _deviceDetachedRepo.Add(deviceDetachedHistory);
                        _deviceDetachedRepo.SaveChanges();
                        var postData = new ZohoSubscriptionHistory();


                        if (!string.IsNullOrEmpty(device.Subscription?.SubscriptionId))
                        {
                            if (device.Subscription.SubscriptionStatus == nameof(SubscriptionStatus.live))
                            {
                                subcriptionDetails = await _zohoSubscription.PauseSubscription(token, device.Subscription.SubscriptionId, OtherConstants.FactoryReset);

                            }
                            else
                            {
                                subcriptionDetails = await _zohoSubscription.GetSubscription(token, device.Subscription.SubscriptionId);
                            }

                            if (subcriptionDetails.message != "error")
                            {
                                postData = new ZohoSubscriptionHistory()
                                {
                                    SubscriptionNumber = subcriptionDetails.subscription.subscription_number,
                                    OrgId = OrgId,
                                    SubscriptionId = subcriptionDetails.subscription.subscription_id,
                                    DeviceId = device.LogicalDeviceId,
                                    DeviceName = device.DeviceName,
                                    PlanCode = subcriptionDetails.subscription.plan.plan_code,
                                    CreatedTime = Convert.ToDateTime(subcriptionDetails.subscription.created_time),
                                    ProductName = subcriptionDetails.subscription.product_name,
                                    PlanName = subcriptionDetails.subscription.plan.name,
                                    Status = subcriptionDetails.subscription.status,
                                    EventType = nameof(SubscriptionEventType.subscription_paused),
                                    PlanPrice = subcriptionDetails.subscription.plan.price,
                                    CreatedDate = DateTime.UtcNow.Ticks,
                                    ModifiedDate = DateTime.UtcNow.Ticks,
                                    CGSTName = "CGST",
                                    CGSTAmount = subcriptionDetails.subscription.taxes[0].tax_amount,
                                    SGSTName = "SGST",
                                    SGSTAmount = subcriptionDetails.subscription.taxes[0].tax_amount,
                                    TaxPercentage = subcriptionDetails.subscription.plan.tax_percentage,
                                    Amount = subcriptionDetails.subscription.amount,
                                    SubTotal = subcriptionDetails.subscription.sub_total,
                                    Email = subcriptionDetails.subscription.customer.email,
                                    //todo email here is always the email of the 1st org admin and not the one who actually added the subscription
                                    CompanyName = subcriptionDetails.subscription.customer.company_name,
                                    CreatedBy = subcriptionDetails.subscription.customer.email,
                                    ModifiedBy = subcriptionDetails.subscription.customer.email,
                                    InvoiceId = null, // downgrade subscription invoice wont generate 
                                    Interval = subcriptionDetails.subscription.interval,
                                    IntervalUnit = subcriptionDetails.subscription.interval_unit,
                                    Currency = subcriptionDetails.subscription.currency_code,
                                    NextBillingAt = subcriptionDetails.subscription.current_term_ends_at

                                };

                                _subscriptionRepo.Add(postData);
                                _subscriptionRepo.SaveChanges();

                                var sub = _subscriptionRepo.Query(z => z.Id == postData.Id).FirstOrDefault();

                                foreach (var item in subcriptionDetails.subscription.addons)
                                {
                                    var subFeature = new FeatureSubscriptionAssociation()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Code = item.addon_code,
                                        Name = item.name,
                                        Price = item.price,
                                        CreatedBy = subcriptionDetails.subscription.customer.email,
                                        ModifiedBy = subcriptionDetails.subscription.customer.email,
                                        CreatedDate = DateTime.UtcNow.Ticks,
                                        ModifiedDate = DateTime.UtcNow.Ticks,
                                        ZohoSubscriptionHistory = sub
                                        //ZohoSubscriptionHistoryFK = sub.Id
                                    };
                                    _featureSubscriptionRepo.Add(subFeature);
                                    _featureSubscriptionRepo.SaveChanges();
                                }
                            
                            }
                        }

                        var userDevicePermission = _userDevicePermissionRepo.Query(z => z.DeviceId == device.LogicalDeviceId).ToList();
                        if(userDevicePermission!= null || userDevicePermission.Count >0)
                        {
                            _userDevicePermissionRepo.RemoveRangeAsync(userDevicePermission.ToArray());
                            _userDevicePermissionRepo.SaveChanges();
                        }

                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError("Error in DeleteDevice action method in DevicesController {0}", ex);
                    }
                }


                var message = string.Empty;

                switch (device.DeviceType)
                {
                    case nameof(Applications.TEV):
                        message = "Device reset to factory version successfully";
                        break;
                    case nameof(Applications.TEV2):
                        message = "Device reset to factory version successfully";
                        break;
                    case nameof(Applications.WSD):
                        message = "Device deleted successfully";
                        break;
                    default:
                        break;
                }
                await _iotHub.DeleteDeviceFromDeviceTwin(device.Id);

                await _deviceRepo.DeleteDevice(id, OrgId);

                #region Clear route table entry for specified device in factory reset case
                var srtCompatibleFirmwareVersion = this._configuration.GetSection("SrtCompatibleFirmwareVersion").Value;

                if (device.DeviceType != null && device.DeviceType.Trim().ToUpper() == "TEV2"
                    && Convert.ToDouble(device.CurrentFirmwareVersion.Trim()) >= Convert.ToDouble(srtCompatibleFirmwareVersion.Trim()))
                {
                    var allocatedRoute = _srtRoutesRepo.GetAll()
                                .Where(r => r.DeviceId == id)
                                .ToList().FirstOrDefault();

                    if (allocatedRoute != null)
                    {
                        allocatedRoute.DeviceId = null;
                        allocatedRoute.ModifiedDate = null;
                        _srtRoutesRepo.SaveChanges();
                    }
                }
                #endregion

                if (device != null && (device.DeviceType == "TEV" || device.DeviceType == "TEV2"))
                {
                    var awsAccessKeyId = this._configuration.GetSection("liveStreaming").GetSection("awsAccessKeyId").Value;
                    var awsSecretKey = this._configuration.GetSection("liveStreaming").GetSection("awsSecretKey").Value;
                    var region = this._configuration.GetSection("liveStreaming").GetSection("awsRegion").Value;
                    using (var kinesisClient = new AmazonKinesisVideoClient(awsAccessKeyId, awsSecretKey, RegionEndpoint.APSouth1))
                    {
                        try
                        {
                            var createRequest = new DescribeStreamRequest();
                            createRequest.StreamName = device.LogicalDeviceId;
                            var res =  await kinesisClient.DescribeStreamAsync(createRequest);
                            var deleteStremReq = new DeleteStreamRequest();
                            deleteStremReq.StreamARN = res.StreamInfo.StreamARN;
                            deleteStremReq.CurrentVersion = res.StreamInfo.Version;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Exception while deleting the AWS stream for device {d}:- {ex}", device.LogicalDeviceId, ex);
                            return Ok(new MMSHttpReponse { SuccessMessage = message });
                        }
                    }
                }
                
                return Ok(new MMSHttpReponse { SuccessMessage = message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while deleting Device {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets all Device Types
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllDeviceTypes")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<DeviceTypeResponse>>), StatusCodes.Status200OK)]
        [AllowAnonymous]
        public IActionResult GetAllDeviceTypes()
        {
            var allDeviceTypes = Enum.GetValues(typeof(Applications)).Cast<Applications>().ToList();

            var allDeviceTypeResponse = allDeviceTypes.Select(z => new DeviceTypeResponse()
            {
                Name = z.ToString(),
                LongName = z.GetName(),
                Description = z.GetDescription()
            }).ToList();

            //allDeviceTypeResponse.RemoveAll(t => t.Name == "TEV2");

            return Ok(new MMSHttpReponse<List<DeviceTypeResponse>> { ResponseBody = allDeviceTypeResponse });
        }

        /// <summary>
        /// Gets the count of unack alerts
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <returns></returns>
        [HttpPost("unacknowledgedAlertsCount")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<UnAckAlertsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnacknowledgedAlertsCount([FromBody] SiteIdsRequest deviceIds)
        {
            try
            {
                if (deviceIds == null)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Device Id required" });
                }
                var result = await _generalRepo.GetUnacknowledgedAlerts(deviceIds.Ids, OrgId);
                var ret = result.Select(x => new UnAckAlertsResponse { Count = x.Count, DeviceId = x.DeviceId }).ToList();
                return Ok(new MMSHttpReponse<List<UnAckAlertsResponse>> { ResponseBody = ret });
            }
            catch (Exception ex)
            {

                _logger.LogError("Exception while getting unack alerts count {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates device configuration for loiter, crowd, trespassing and buzzer
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("UpdateDeviceConfiguration/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDeviceConfiguration(string deviceId, [FromBody] UpdateDeviceConfigRequest model)
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

                var CurrentFirmwareVersion = string.Empty;
                var ScheduleCompatibleFirmwareV = this._configuration.GetSection("fetaureScheduleCompatibleFirmwareVersion").Value;

                if (device.CurrentFirmwareVersion != null)
                {
                    CurrentFirmwareVersion = device.CurrentFirmwareVersion.Contains(".") ? device.CurrentFirmwareVersion : $"{device.CurrentFirmwareVersion}.0";
                }

                if (model.CrowdSchedule != null && model.TrespassingSchedule != null && model.LoiterSchedule != null && device.DeviceType == "TEV2" && Convert.ToDouble(CurrentFirmwareVersion) >= Convert.ToDouble(ScheduleCompatibleFirmwareV))
                {
                    var validModel = FormatScheduleFeatureRequest(model);

                    if (string.IsNullOrEmpty(validModel.ModelErrors))
                    {
                        validModel.logicalDeviceId = device.LogicalDeviceId;
                        await _iotHub.UpdateDeviceFeatureConfigurationAndSchedule(validModel);

                        //Update Cosmos Device Data

                        if (device != null)
                        {
                            device.FeatureConfig.Trespassing.TrespassingStartTime = validModel.TrespassingStartTime;
                            device.FeatureConfig.Trespassing.TrespassingEndTime = validModel.TrespassingEndTime;

                            device.FeatureConfig.Loiter.Time = model.LoiterTime * 60;
                            device.FeatureConfig.Crowd.CrowdLimit = model.CrowdPersonLimit;

                            device.FeatureConfig.BuzzerControl = model.BuzzerControl;
                            device.FeatureConfig.PersonDetectionSensitivity = model.PersonDetectionSensitivity;

                            device.TwinChangeStatus = TwinChangeStatus.Default;

                            device.FeatureConfig.Trespassing.TrespassingSchedule = validModel.TrespassingSchedule;
                            device.FeatureConfig.Loiter.LoiterSchedule = validModel.LoiterSchedule;
                            device.FeatureConfig.Crowd.CrowdSchedule = validModel.CrowdSchedule;
                        }
                    }
                    else
                    {
                        return BadRequest(new MMSHttpReponse { ErrorMessage = $"Error :- {validModel.ModelErrors}" });
                    }
                }
                else
                {

                    DateTime dt1 = DateTime.Parse(model.TrespassingStartTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                    DateTime dt2 = DateTime.Parse(model.TrespassingEndTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

                    await _iotHub.UpdateDeviceFeatureConfiguration(deviceId, dt1.ToString("HH:mm"), dt2.ToString("HH:mm"), model.LoiterTime * 60, model.CrowdPersonLimit, model.BuzzerControl, model.PersonDetectionSensitivity);

                    //Update Cosmos Device Data

                    if (device != null)
                    {
                        device.FeatureConfig.Trespassing.TrespassingStartTime = dt1.ToString("HH:mm");
                        device.FeatureConfig.Trespassing.TrespassingEndTime = dt2.ToString("HH:mm");

                        device.FeatureConfig.Loiter.Time = model.LoiterTime * 60;
                        device.FeatureConfig.Crowd.CrowdLimit = model.CrowdPersonLimit;

                        device.FeatureConfig.BuzzerControl = model.BuzzerControl;
                        device.FeatureConfig.PersonDetectionSensitivity = model.PersonDetectionSensitivity;

                        device.TwinChangeStatus = TwinChangeStatus.Default;
                    }
                }

                await _deviceRepo.UpdateDevice(OrgId, device);

                return Ok(new MMSHttpReponse { SuccessMessage = "Device configuration updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while updating device configuration {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets devices configuration and SignalR URL for latest snap from the device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetDeviceConfiguration/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<DeviceConfigurationResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceConfiguration(string deviceId)
        {
            if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
            {
                return Forbid();
            }
            try
            {
                var data = await _deviceRepo.GetDeviceFeatureConfiguration(deviceId, OrgId);
                if (data == null)
                {
                    return Ok(new MMSHttpReponse<DeviceConfigurationResponse> { ResponseBody = null });
                }

                var CurrentFirmwareVersion = string.Empty;

                var ScheduleCompatibleFirmwareV = this._configuration.GetSection("fetaureScheduleCompatibleFirmwareVersion").Value;
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                if (device.CurrentFirmwareVersion != null)
                {
                    CurrentFirmwareVersion = device.CurrentFirmwareVersion.Contains(".") ? device.CurrentFirmwareVersion : $"{device.CurrentFirmwareVersion}.0";
                }


                var result = new DeviceConfigurationResponse
                {
                    Trespassing = data.Trespassing,
                    Crowd = data.Crowd,
                    Loiter = new LoiterDTO { Time = data.Loiter.Time / 60, LoiterSchedule = data.Loiter.LoiterSchedule },
                    BuzzerControl = data.BuzzerControl,
                    PersonDetectionSensitivity = data.PersonDetectionSensitivity
                };

                if (device.DeviceType == "TEV2" && Convert.ToDouble(CurrentFirmwareVersion) >= Convert.ToDouble(ScheduleCompatibleFirmwareV))
                {
                    result.Crowd.SchedulingSupported = true;
                    result.Trespassing.SchedulingSupported = true;
                    result.Loiter.SchedulingSupported = true;
                }

                return Ok(new MMSHttpReponse<DeviceConfigurationResponse> { ResponseBody = result });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while getting device configuration {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("SetResetZoneFencing/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse<SetResetZoneFencingRequest>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetResetZoneFencing([Required] string deviceId, [FromBody] SetResetZoneFencingRequest data)
        {
            if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
            {
                return Forbid();
            }
            try
            {
                if (data != null)
                {
                    var deviceReplica = await _deviceRepo.GetDevice(deviceId, OrgId);
                    if (deviceReplica.DeviceType == Applications.TEV2.ToString())
                    {
                        data.Zone.x1 = (int)((float)1920 / data.ClientImageWidth * data.Zone.x1);
                        data.Zone.x2 = (int)((float)1920 / data.ClientImageWidth * data.Zone.x2);
                        data.Zone.y1 = (int)((float)1080 / data.ClientImageHeight * data.Zone.y1);
                        data.Zone.y2 = (int)((float)1080 / data.ClientImageHeight * data.Zone.y2);
                    }
                    else
                    {
                        data.Zone.x1 = (int)((float)640 / data.ClientImageWidth * data.Zone.x1);
                        data.Zone.x2 = (int)((float)640 / data.ClientImageWidth * data.Zone.x2);
                        data.Zone.y1 = (int)((float)480 / data.ClientImageHeight * data.Zone.y1);
                        data.Zone.y2 = (int)((float)480 / data.ClientImageHeight * data.Zone.y2);
                    }

                    var result = await _iotHub.UpdateZoneFencing(deviceId, data.Enabled, data.Zone);

                    //Update Cosmos Device Data

                    if (deviceReplica != null)
                    {
                        deviceReplica.FeatureConfig.Zone.X1 = data.Zone.x1;
                        deviceReplica.FeatureConfig.Zone.X2 = data.Zone.x2;
                        deviceReplica.FeatureConfig.Zone.Y1 = data.Zone.y1;
                        deviceReplica.FeatureConfig.Zone.Y2 = data.Zone.y2;

                        deviceReplica.FeatureConfig.ZoneFencingEnabled = data.Enabled;

                        deviceReplica.TwinChangeStatus = TwinChangeStatus.Default;
                        await _deviceRepo.UpdateDevice(OrgId, deviceReplica);
                    }


                    if (result)
                    {
                        if (data.Enabled)
                        {
                            return Ok(new MMSHttpReponse { SuccessMessage = "Features have been enabled in the selected area." });
                        }
                        else
                        {
                            return Ok(new MMSHttpReponse { SuccessMessage = "Zone has been reset to default." });
                        }
                    }
                }
                return BadRequest(new MMSHttpReponse { ErrorMessage = "Error occured while selecting the area" });

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while updating device configuration {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates a device name or location
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("UpdateDeviceNameOrLocation/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateDeviceNameOrLocation(string deviceId, [FromBody] UpdateDeviceRequest reqBody)
        {
            try
            {
                if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                var locationName = "";
                var locationId = "";
                if (!string.IsNullOrEmpty(reqBody.LocationId))
                {
                    var location = _locationRepo.Query(x => x.Id == reqBody.LocationId).FirstOrDefault();
                    if (location == null)
                    {
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "Invalid Location" });
                    }
                    locationName = location.Name;
                    locationId = location.Id;
                }
                var result = await _iotHub.UpdateNameOrLocation(deviceId, reqBody.DeviceName, locationId, locationName);

                //Update Comos Device Data
                if (device != null)
                {
                    if (device.DeviceType == "TEV2")
                    {
                        await _alertRepo.UpdateDefaultDeviceName(device.LogicalDeviceId, device.OrgId, reqBody.DeviceName, locationName);
                    }
                    device.LocationId = locationId;
                    device.LocationName = locationName;
                    device.DeviceName = reqBody.DeviceName;
                    device.TwinChangeStatus = TwinChangeStatus.Default;
                    await _deviceRepo.UpdateDevice(OrgId, device);

                }
                return Ok(new MMSHttpReponse { SuccessMessage = "Device updated succesfully. Syncing data may take a while." });
            }
            catch (Exception ex)
            {

                _logger.LogError("Exception while updating device name and location {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        [HttpGet("DeviceValidationCheck/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeviceValidationCheck(string deviceId)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device is not found" });
                }
                else
                {
                    return Ok(new MMSHttpReponse<bool> { SuccessMessage = "Selected device is valid", ResponseBody = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in DeviceValidationCheck {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("GetPeopleCount/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<GetPeopleCountResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPeopleCount(string deviceId, [Required] int skip, [Required] int take)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (device == null)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                var result = await _peoplecountRepo.GetPeopleCount(skip, take, deviceId, OrgId);

                var resultSet = new GetPeopleCountResponse
                {
                    LocationName = device.LocationName,
                    DeviceName = device.DeviceName,
                    PeopleCountList = result.Select(x => new PeopleCountData
                    {
                        OccurenceTimestamp = x.occurenceTimestamp,
                        PeopleCount = x.peopleCount
                    }).ToList()
                };
                return Ok(new MMSHttpReponse<GetPeopleCountResponse> { ResponseBody = resultSet, SuccessMessage = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in DeviceValidationCheck {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("Restart/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Restart(string deviceId)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                var response = await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "restart_device", 10, _logger);

                _logger.LogInformation("Device restarted");
                return Ok(new MMSHttpReponse { SuccessMessage = "Device restarted" });
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

        /// <summary>
        /// Get the auth pin for QR codes
        /// </summary>
        /// <param name="purpose">One of the three setup/edit/factoryreset</param>
        /// <param name="deviceId">The device id</param>
        /// <returns></returns>
        [HttpGet("QRAuthCode")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuthPin([Required] string purpose, [Required] string deviceId)
        {
            purpose = purpose.ToLower();
            if (purpose != "setup" && purpose != "edit" && purpose != "factoryreset")
            {
                return BadRequest(new MMSHttpReponse { ErrorMessage = "Invalid purpose" });
            }
            var rand = new Random();
            string number = rand.Next(10000000, 99999999).ToString();
            if (purpose == "setup" || purpose == "edit")
            {
                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var existingCode = _qrAuthCodeRepo.Query(x => x.LogicalDeviceId == deviceId && x.Type == purpose.ToLower()).SingleOrDefault();
                        if (existingCode != null)
                        {
                            _qrAuthCodeRepo.Remove(existingCode);
                        }
                        var entity = new QRAuthCode
                        {
                            Code = Convert.ToInt32(number),
                            LogicalDeviceId = deviceId,
                            Type = purpose
                        };
                        await _qrAuthCodeRepo.AddAsync(entity);
                        _locationRepo.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {


                        transaction.Rollback();
                        _logger.LogError("Error Occured on Create auth code Permission on Sql {exception}", ex);
                        return StatusCode(StatusCodes.Status500InternalServerError);

                    }
                }
            }
            else if (purpose == "factoryreset")
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                number = device.FactoryResetAuthCode;
                if (string.IsNullOrEmpty(number))
                {
                    number = "67876112";
                }
            }
            return Ok(new MMSHttpReponse<string> { ResponseBody = number.ToString() });
        }


        /// <summary>
        /// Validates the QR code auth pin
        /// </summary>
        /// <param name="purpose">one of the two setup/edit</param>
        /// <param name="deviceId">logical Device id</param>
        /// <param name="authCode">auth code</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("ValidateQRAuthCode")]
        [ProducesResponseType(typeof(MMSHttpReponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public IActionResult ValidateAuthPin([Required] string purpose, [Required] string deviceId, [Required] int authCode)
        {
            purpose = purpose.ToLower();
            if (purpose != "setup" && purpose != "edit")
            {
                return BadRequest(new MMSHttpReponse { ErrorMessage = "Invalid purpose" });
            }
            if (purpose == "setup" || purpose == "edit")
            {
                IActionResult res;

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var authPin = _qrAuthCodeRepo.Query(x => x.LogicalDeviceId == deviceId && x.Type == purpose.ToLower()).SingleOrDefault();
                        if (authPin == null)
                        {
                            res = BadRequest(new MMSHttpReponse { ErrorMessage = "QR auth code expired or is invalid" });
                            return res;
                        }

                        if (authPin.Code != authCode || GetEpochDate() - authPin.CreatedDate > Convert.ToInt64(_configuration.GetSection("QRCodeExpiryTimeInSeconds").Value))
                        {
                            res = BadRequest(new MMSHttpReponse { ErrorMessage = "QR auth code expired or is invalid" });
                        }
                        else
                        {
                            res = Ok(new MMSHttpReponse { SuccessMessage = "QR auth code validation success" });
                        }
                        _qrAuthCodeRepo.Remove(authPin);
                        _locationRepo.SaveChanges();
                        transaction.Commit();
                        return res;
                    }
                    catch (Exception ex)
                    {


                        transaction.Rollback();
                        _logger.LogError("Error Occured on Create auth code Permission on Sql {exception}", ex);
                        return StatusCode(StatusCodes.Status500InternalServerError);

                    }
                }
            }

            return BadRequest(new MMSHttpReponse<int> { ErrorMessage = "Invalid purpose" });
        }

        [HttpGet("GetZoneFencing/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<GetZoneFencingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetZoneFencing(string deviceId, [Required] int clientImageWidth, [Required] int clientIMageHeight)
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
                #region DeviceDirectMethod
                var response = await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "upload_picture", 15, _logger);
                #endregion
                if (response.Status == 400)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }
                if (response.Status == 200)
                {
                    _logger.LogInformation("Device uploaded the latest picture");

                    #region Azure Blob
                    var client = new BlobContainerClient(_configuration.GetSection("blob").GetSection("ConnectionString").Value,
                      _configuration.GetSection("blob").GetSection("ZoneFencingContainerName").Value);
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
                    var imageUrl = _configuration.GetSection("blob").GetSection("ZoneFencingBloburl").Value + "/" + $"{deviceId}.jpg?" + sas;
                    #endregion Azure Blob
                    #region Response
                    GetZoneFencingResponse result = null;
                    var data = await _deviceRepo.GetDeviceFeatureConfiguration(deviceId, OrgId);
                    if (data == null)
                    {
                        return Ok(new MMSHttpReponse<GetZoneFencingResponse> { ResponseBody = result });
                    }
                    result = new GetZoneFencingResponse
                    {

                        Enabled = data.ZoneFencingEnabled,
                        Zone = data.Zone,
                        ImageUrl = imageUrl
                    };
                    if (device.DeviceType == Applications.TEV2.ToString())
                    {
                        result.Zone.X1 = (int)Math.Ceiling(clientImageWidth * result.Zone.X1 / (float)1920);
                        result.Zone.X2 = (int)Math.Ceiling(clientImageWidth * result.Zone.X2 / (float)1920);
                        result.Zone.Y1 = (int)Math.Ceiling(clientIMageHeight * result.Zone.Y1 / (float)1080);
                        result.Zone.Y2 = (int)Math.Ceiling(clientIMageHeight * result.Zone.Y2 / (float)1080);
                    }
                    else
                    {
                        result.Zone.X1 = (int)Math.Ceiling(clientImageWidth * result.Zone.X1 / (float)640);
                        result.Zone.X2 = (int)Math.Ceiling(clientImageWidth * result.Zone.X2 / (float)640);
                        result.Zone.Y1 = (int)Math.Ceiling(clientIMageHeight * result.Zone.Y1 / (float)480);
                        result.Zone.Y2 = (int)Math.Ceiling(clientIMageHeight * result.Zone.Y2 / (float)480);
                    }


                    return Ok(new MMSHttpReponse<GetZoneFencingResponse> { ResponseBody = result });
                    #endregion
                }
                else
                {
                    _logger.LogError("Device not found, Error Code:- {ex}", response.Status.ToString());
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found, Error Code:- " + response.Status.ToString() });
                }

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

        /// <summary>
        /// Gets device type from the device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetDeviceType/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceType(string deviceId)
        {
            try
            {
                var data = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (data == null)
                {
                    return Ok(new MMSHttpReponse<string> { ResponseBody = null });
                }

                return Ok(new MMSHttpReponse<string> { ResponseBody = data.DeviceType });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while getting device type {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets device details by logical device Id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetDeviceByDeviceId/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<DeviceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeviceByDeviceId(string deviceId)
        {
            try
            {
                var data = await _deviceRepo.GetDevice(deviceId, OrgId);
                var newfirmwareVersionTev1 = "";
                var newfirmwareVersionTev2 = "";
                if (data == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                if (data.DeviceType == "TEV")
                {
                    newfirmwareVersionTev1 = _deviceRepo.GetLatestFirmwareVersion("TEV").GetAwaiter().GetResult();
                }
                else if (data.DeviceType == "TEV2")
                {
                    newfirmwareVersionTev2 = _deviceRepo.GetLatestFirmwareVersion("TEV2").GetAwaiter().GetResult();
                }
                var ret = MapDTO(data, newfirmwareVersionTev1, newfirmwareVersionTev2);

                return Ok(new MMSHttpReponse<DeviceResponse> { ResponseBody = ret });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while getting device type {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets device SD card Passphrase by logical device Id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetSDPassByDeviceId/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<SDCardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSDCardPaswordByDeviceId(string deviceId)
        {
            try
            {
                var data = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (data == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                {
                    return Forbid();
                }
                var ret = new SDCardResponse
                {
                    SDCardPassPhrase = data.sdCardPassPhrase
                };

                return Ok(new MMSHttpReponse<SDCardResponse> { ResponseBody = ret, SuccessMessage = "Successfuly fetched SD card Passphrase" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while getting device type {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// This method put the device in config mode using direct method call.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("StartDeviceConfigMode/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartDeviceConfigMode(string deviceId)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                var response = await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "config_mode", 10, _logger);

                _logger.LogInformation("Device config mode started");
                return Ok(new MMSHttpReponse { SuccessMessage = "Device enabled configuration mode" });
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

        /// <summary>
        /// Delete a duplicated device by passing device details.called by Azure Function App DeleteDuplicateDevice
        /// </summary>
        /// <param name="duplicatedDevice"></param>
        /// <returns></returns>
        [HttpPost("DeleteDuplicateDevice")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteDuplicateDevice([FromBody] Device duplicatedDevice)
        {
            try
            {
                if (!duplicatedDevice.SecretKey.Equals(_configuration.GetSection("ActivatePromoPlanSecretKey").Value))
                {
                    _logger.LogInformation($"Secret key not matching Bad Request input :- {duplicatedDevice.SecretKey} App config :- {_configuration.GetSection("ActivatePromoPlanSecretKey").Value}");
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Not Authorized" });
                }

                var data = _deviceReplacementRepo.Query(z => z.DeviceId == duplicatedDevice.Id && z.ReplaceStatus != ReplaceStatusEnum.Closed).OrderByDescending(x => x.CreatedDate).FirstOrDefault();

                if (data != null)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = $"replacement request exist for the device and the current status is {data.ReplaceStatus.GetName()}" });
                }

                var token = await _zohoAuthentication.GetZohoToken();

                var subcriptionDetails = new RetrieveSubscriptionDetailsResponse();

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var deviceDetachedHistory = new DeviceDetachedHistory
                        {
                            LogicalDetachedDeviceId = duplicatedDevice.LogicalDeviceId,
                            PhysicalDetachedDeviceId = duplicatedDevice.Id,
                            OrgId = duplicatedDevice.OrgId
                        };

                        _deviceDetachedRepo.Add(deviceDetachedHistory);
                        _deviceDetachedRepo.SaveChanges();
                        var postData = new ZohoSubscriptionHistory();


                        if (!string.IsNullOrEmpty(duplicatedDevice.Subscription?.SubscriptionId))
                        {
                            if (duplicatedDevice.Subscription.SubscriptionStatus == nameof(SubscriptionStatus.live))
                            {
                                var res = await _zohoSubscription.DeleteSubscription(token, duplicatedDevice.Subscription.SubscriptionId);

                            }
                            else
                            {
                                subcriptionDetails = await _zohoSubscription.GetSubscription(token, duplicatedDevice.Subscription.SubscriptionId);
                            }

                            if (subcriptionDetails.message != "error")
                            {
                                postData = new ZohoSubscriptionHistory()
                                {
                                    SubscriptionNumber = subcriptionDetails.subscription.subscription_number,
                                    OrgId = OrgId,
                                    SubscriptionId = subcriptionDetails.subscription.subscription_id,
                                    DeviceId = duplicatedDevice.LogicalDeviceId,
                                    DeviceName = duplicatedDevice.DeviceName,
                                    PlanCode = subcriptionDetails.subscription.plan.plan_code,
                                    CreatedTime = Convert.ToDateTime(subcriptionDetails.subscription.created_time),
                                    ProductName = subcriptionDetails.subscription.product_name,
                                    PlanName = subcriptionDetails.subscription.plan.name,
                                    Status = nameof(SubscriptionStatus.non_renewing),
                                    EventType = nameof(SubscriptionEventType.subscription_cancelled),
                                    PlanPrice = subcriptionDetails.subscription.plan.price,
                                    CreatedDate = DateTime.UtcNow.Ticks,
                                    ModifiedDate = DateTime.UtcNow.Ticks,
                                    CGSTName = "CGST",
                                    CGSTAmount = subcriptionDetails.subscription.taxes[0].tax_amount,
                                    SGSTName = "SGST",
                                    SGSTAmount = subcriptionDetails.subscription.taxes[0].tax_amount,
                                    TaxPercentage = subcriptionDetails.subscription.plan.tax_percentage,
                                    Amount = subcriptionDetails.subscription.amount,
                                    SubTotal = subcriptionDetails.subscription.sub_total,
                                    Email = subcriptionDetails.subscription.customer.email,
                                    //todo email here is always the email of the 1st org admin and not the one who actually added the subscription
                                    CompanyName = subcriptionDetails.subscription.customer.company_name,
                                    CreatedBy = subcriptionDetails.subscription.customer.email,
                                    ModifiedBy = subcriptionDetails.subscription.customer.email,
                                    InvoiceId = null, // downgrade subscription invoice wont generate 
                                    Interval = subcriptionDetails.subscription.interval,
                                    IntervalUnit = subcriptionDetails.subscription.interval_unit,
                                    Currency = subcriptionDetails.subscription.currency_code,
                                    NextBillingAt = subcriptionDetails.subscription.current_term_ends_at

                                };

                                _subscriptionRepo.Add(postData);
                                _subscriptionRepo.SaveChanges();

                                var sub = _subscriptionRepo.Query(z => z.Id == postData.Id).FirstOrDefault();

                                foreach (var item in subcriptionDetails.subscription.addons)
                                {
                                    var subFeature = new FeatureSubscriptionAssociation()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Code = item.addon_code,
                                        Name = item.name,
                                        Price = item.price,
                                        CreatedBy = subcriptionDetails.subscription.customer.email,
                                        ModifiedBy = subcriptionDetails.subscription.customer.email,
                                        CreatedDate = DateTime.UtcNow.Ticks,
                                        ModifiedDate = DateTime.UtcNow.Ticks,
                                        ZohoSubscriptionHistory = sub
                                        //ZohoSubscriptionHistoryFK = sub.Id
                                    };
                                    _featureSubscriptionRepo.Add(subFeature);
                                    _featureSubscriptionRepo.SaveChanges();
                                }

                            }
                        }

                        var userDevicePermission = _userDevicePermissionRepo.Query(z => z.DeviceId == duplicatedDevice.LogicalDeviceId).ToList();
                        if (userDevicePermission != null || userDevicePermission.Count > 0)
                        {
                            _userDevicePermissionRepo.RemoveRangeAsync(userDevicePermission.ToArray());
                            _userDevicePermissionRepo.SaveChanges();
                        }

                        transaction.Commit();
                        _logger.LogInformation($"Device {duplicatedDevice.DeviceName} ");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError("Error in DeleteDevice action method in DevicesController {0}", ex);
                    }
                }


                var message = string.Empty;

                message = $"Device {duplicatedDevice.DeviceName} Deleted Successfuly";

                return Ok(new MMSHttpReponse { SuccessMessage = message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while deleting Device {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets Wifi signal strength value from device between 0 to 100 using direct method call.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetWifiSignalStrength/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWifiSignalStrength(string deviceId)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                if (device == null)
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                }

                var response = await _iotHub.InvokeDeviceDirectMethodAsync(device.Id, "signal_strength", 10, _logger);

                var jsonResult = "";

                if (response.Status == 200)
                {
                    jsonResult = response.GetPayloadAsJson();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                _logger.LogInformation($"Wifi signal Strength for device  {deviceId} is {jsonResult}");
                return Ok(new MMSHttpReponse<string> { ResponseBody = jsonResult });
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

        private long GetEpochDate()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var epochDate = (long)t.TotalSeconds;
            return epochDate;
        }

        /// <summary>
        /// Maps TevDevice class to DeviceResponse class
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private DeviceResponse MapDTO(Device x, string newfirmwareVersionTev1, string newfirmwareVersionTev2)
        {
            var firmwareVersion = string.Empty;
            var newFirmwareVersionAvailableToOrg = string.Empty;
            if (x.CurrentFirmwareVersion != null && x.DeviceType == "TEV")
            {
                firmwareVersion = x.CurrentFirmwareVersion.Contains(".") ? x.CurrentFirmwareVersion : $"{x.CurrentFirmwareVersion}.0";
            }
            if (x.CurrentFirmwareVersion != null && x.DeviceType == "TEV2")
            {
                firmwareVersion = x.CurrentFirmwareVersion.Contains(".") ? x.CurrentFirmwareVersion : $"{x.CurrentFirmwareVersion}.0";
            }
            if (x.CurrentFirmwareVersion != null && x.DeviceType == "WSD")
            {
                firmwareVersion = x.CurrentFirmwareVersion;
            }
            if (newfirmwareVersionTev1 != null && x.DeviceType == "TEV")
            {
                newfirmwareVersionTev1 = newfirmwareVersionTev1.Contains(".") ? newfirmwareVersionTev1 : $"{newfirmwareVersionTev1}.0";
                newFirmwareVersionAvailableToOrg = newfirmwareVersionTev1;
            }
            if (newfirmwareVersionTev2 != null && x.DeviceType == "TEV2")
            {
                newfirmwareVersionTev2 = newfirmwareVersionTev2.Contains(".") ? newfirmwareVersionTev2 : $"{newfirmwareVersionTev2}.0";
                newFirmwareVersionAvailableToOrg = newfirmwareVersionTev2;
            }
            if (x.Firmware?.NewFirmwareVersion != null)
            {
                if (x.DeviceType == "TEV2")
                {
                    newFirmwareVersionAvailableToOrg = Convert.ToDouble(x.Firmware.NewFirmwareVersion) > Convert.ToDouble(newfirmwareVersionTev2) ? x.Firmware.NewFirmwareVersion : newfirmwareVersionTev2;
                }
                else
                {
                    newFirmwareVersionAvailableToOrg = Convert.ToDouble(x.Firmware.NewFirmwareVersion) > Convert.ToDouble(newfirmwareVersionTev1) ? x.Firmware.NewFirmwareVersion : newfirmwareVersionTev1;
                }

            }

            if (x.DeviceType == "TEV2" && x.Subscription != null && x.Subscription.AvailableFeatures.Count() > 0 && x.Subscription.AvailableFeatures.Contains(2))
            {
                x.Subscription.AvailableFeatures = x.Subscription.AvailableFeatures.Where(val => val != 2).ToArray(); // removing addon number 2 which is used for Last 7 days cloud storage for Alert.
            }
            var SrtCompatibleFirmwareV = this._configuration.GetSection("SrtCompatibleFirmwareVersion").Value;
            bool srtSupported = true;
            if (x.DeviceType == "TEV2" && Convert.ToDouble(firmwareVersion) >= Convert.ToDouble(SrtCompatibleFirmwareV))
            {
                srtSupported = true;
            }
            else
            {
                srtSupported = false;
            }

            return new DeviceResponse
            {
                Id = x.LogicalDeviceId,
                Name = x.DeviceName,
                LocationId = x.LocationId,
                LocationName = x.LocationName,
                Connected = x.Online,
                MacAddress = null,
                FirmwareVersion = firmwareVersion,
                SubscriptionId = x.Subscription?.SubscriptionId,
                SubscriptionExpiryDate = Helper.ConvertDateTime(x.Subscription?.SubscriptionExpiryDate),
                AvailableFeatures = x.DeviceType == nameof(Applications.TEV) || x.DeviceType == nameof(Applications.TEV2) ?
                                    x.Subscription?.AvailableFeatures?.Select(x => Helper.Replace_withWhiteSpaceAndMakeTheStringIndexValueUpperCase(Convert.ToString((AlertType)x))).ToList()
                                    : x.Subscription?.AvailableFeatures?.Select(x => Helper.Replace_withWhiteSpaceAndMakeTheStringIndexValueUpperCase(Convert.ToString((FeatureType)x))).ToList(),
                DeviceType = Helper.GetDeviceType(x.DeviceType),
                SubscriptionStatus = x.Subscription?.SubscriptionStatus,
                Disabled = false,
                WifiName = x.WifiName,
                NewFirmwareVersion = newFirmwareVersionAvailableToOrg,
                isUpdateAvailable = x.DeviceType == nameof(Applications.TEV) || x.DeviceType == nameof(Applications.TEV2) ? CheckUpdateAvailable(x, newFirmwareVersionAvailableToOrg) : false,
                PlanName = x.Subscription?.PlanName,
                CreatedOn = x.CreatedOn,
                BatteryStatus = x.BatteryStatus,
                BatteryStatusDate = x.BatteryStatusDate,
                mspVersion = x.mspVersion == null ? "" : x.mspVersion,
                sdCardStatus = x.sdCardStatus,
                SdCardAvilable = x.SdCardAvilable,
                srtSupported = srtSupported
            };
        }

        /// <summary>
        /// Maps TevDevice class to Tev2 DeviceResponse class
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private Tev2DeviceResponse MapTevDTO(Device x, string newfirmwareVersionTev2)
        {
            var firmwareVersion = string.Empty;
            var newFirmwareVersionAvailableToOrg = string.Empty;

            if (x.CurrentFirmwareVersion != null && x.DeviceType == "TEV2")
            {
                firmwareVersion = x.CurrentFirmwareVersion.Contains(".") ? x.CurrentFirmwareVersion : $"{x.CurrentFirmwareVersion}.0";
            }


            if (newfirmwareVersionTev2 != null && x.DeviceType == "TEV2")
            {
                newfirmwareVersionTev2 = newfirmwareVersionTev2.Contains(".") ? newfirmwareVersionTev2 : $"{newfirmwareVersionTev2}.0";
                newFirmwareVersionAvailableToOrg = newfirmwareVersionTev2;
            }
            if (x.Firmware?.NewFirmwareVersion != null)
            {
                if (x.DeviceType == "TEV2")
                {
                    newFirmwareVersionAvailableToOrg = Convert.ToDouble(x.Firmware.NewFirmwareVersion) > Convert.ToDouble(newfirmwareVersionTev2) ? x.Firmware.NewFirmwareVersion : newfirmwareVersionTev2;
                }

            }
            var SrtCompatibleFirmwareV = this._configuration.GetSection("SrtCompatibleFirmwareVersion").Value;
            bool srtSupported = true;
            if (x.DeviceType == "TEV2" && Convert.ToDouble(firmwareVersion) >= Convert.ToDouble(SrtCompatibleFirmwareV))
            {
                srtSupported = true;
            }
            else
            {
                srtSupported = false;
            }
            return new Tev2DeviceResponse
            {
                Id = x.LogicalDeviceId,
                Name = x.DeviceName,
                LocationId = x.LocationId,
                LocationName = x.LocationName,
                Connected = x.Online,
                DeviceType = Helper.GetDeviceType(x.DeviceType),
                sdCardStatus = x.sdCardStatus,
                SdCardAvilable = x.SdCardAvilable,
                srtSupported = srtSupported
            };
        }

        private FormattedUpdateDeviceConfigRequest FormatScheduleFeatureRequest(UpdateDeviceConfigRequest x)
        {
            FormattedUpdateDeviceConfigRequest req = new FormattedUpdateDeviceConfigRequest();
            req.TrespassingSchedule = new List<RecordSchedule>();
            req.LoiterSchedule = new List<RecordSchedule>();
            req.CrowdSchedule = new List<RecordSchedule>();
            req.LoiterTime = x.LoiterTime * 60;
            req.CrowdPersonLimit = x.CrowdPersonLimit;
            req.TrespassingStartTime = DateTime.Parse(x.TrespassingStartTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
            req.TrespassingEndTime = DateTime.Parse(x.TrespassingEndTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");

            if (x.TrespassingSchedule.Count() > 7)
            {
                req.ModelErrors = $"Tresspassing Schedule should not have more than 7 days";
                return req;
            }

            if (x.CrowdSchedule.Count() > 7)
            {
                req.ModelErrors = $"Crowd Schedule should not have more than 7 days";
                return req;
            }

            if (x.LoiterSchedule.Count() > 7)
            {
                req.ModelErrors = $"Loiter Schedule should not have more than 7 days";
                return req;
            }
            //Tresspasing Schedule Validation
            for (int i = 0; i < x.TrespassingSchedule.Count(); i++)
            {
                if (!(x.TrespassingSchedule[i].fullday) && (x.TrespassingSchedule[i].time.Count() >= 1))
                {
                    if (x.TrespassingSchedule[i].time.Count() >= 4)
                    {
                        req.ModelErrors = $"Tresspassing for day {x.TrespassingSchedule[i].day} , Schedule should not have more than 3 time slots";
                        return req;
                    }
                    //else
                    //{
                    //    for (int j = 0; j < x.TrespassingSchedule[i].time.Count(); j++)
                    //    {
                    //        if (j == 1)
                    //        {
                    //            if (DateTime.Parse(x.TrespassingSchedule[i].time[0].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.TrespassingSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Tresspassing schedule for {x.TrespassingSchedule[i].day} , Slot {j + 1} Start Time Should not be less than End Time of Slot {j} ";
                    //                return req;
                    //            }
                    //        }
                    //        else if (j == 2)
                    //        {
                    //            if (DateTime.Parse(x.TrespassingSchedule[i].time[1].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.TrespassingSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Tresspassing schedule for {x.TrespassingSchedule[i].day} , Slot {j + 1} Start Time Should not be less than End Time of Slot {j} ";
                    //                return req;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (DateTime.Parse(x.TrespassingSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.TrespassingSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Tresspassing schedule for {x.TrespassingSchedule[i].day} , Slot {j + 1} Start Time Should not be grester than End Time";
                    //                return req;
                    //            }
                    //        }
                    //    }
                    //}
                }
            }
            //Tresspasing Schedule Updation
            for (int i = 0; i < x.TrespassingSchedule.Count(); i++)
            {
                if (!(x.TrespassingSchedule[i].fullday) && (x.TrespassingSchedule[i].time.Count() >= 1))
                {
                    for (int j = 0; j < x.TrespassingSchedule[i].time.Count(); j++)
                    {
                        x.TrespassingSchedule[i].time[j].st = DateTime.Parse(x.TrespassingSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                        x.TrespassingSchedule[i].time[j].et = DateTime.Parse(x.TrespassingSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                    }
                    req.TrespassingSchedule.Add(x.TrespassingSchedule[i]);
                }
                else
                {
                    if (x.TrespassingSchedule[i].time.Count() > 0)
                    {
                        for (int j = 0; j < x.TrespassingSchedule[i].time.Count(); j++)
                        {
                            x.TrespassingSchedule[i].time[j].st = DateTime.Parse(x.TrespassingSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                            x.TrespassingSchedule[i].time[j].et = DateTime.Parse(x.TrespassingSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                        }
                        req.TrespassingSchedule.Add(x.TrespassingSchedule[i]);
                    }
                    else
                    {
                        req.TrespassingSchedule.Add(x.TrespassingSchedule[i]);
                    }
                }
            }

            //Loiter Schedule Validation
            for (int i = 0; i < x.LoiterSchedule.Count(); i++)
            {
                if (!(x.LoiterSchedule[i].fullday) && (x.LoiterSchedule[i].time.Count() >= 1))
                {
                    if (x.LoiterSchedule[i].time.Count() >= 4)
                    {
                        req.ModelErrors = $"Loiter for day {x.LoiterSchedule[i].day} , Schedule should not have more than 3 time slots";
                        return req;
                    }
                    //else
                    //{
                    //    for (int j = 0; j < x.LoiterSchedule[i].time.Count(); j++)
                    //    {
                    //        if (j == 1)
                    //        {
                    //            if (DateTime.Parse(x.LoiterSchedule[i].time[0].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.LoiterSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Loiter schedule for {x.LoiterSchedule[i].day} , Slot {j + 1} Start Time Should not be less than End Time of Slot {j} ";
                    //                return req;
                    //            }
                    //        }
                    //        else if (j == 2)
                    //        {
                    //            if (DateTime.Parse(x.LoiterSchedule[i].time[1].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.LoiterSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Loiter schedule for {x.LoiterSchedule[i].day} , Slot {j + 1} Start Time Should not be less than End Time of Slot {j} ";
                    //                return req;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (DateTime.Parse(x.LoiterSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.LoiterSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Loiter schedule for {x.LoiterSchedule[i].day} , Slot {j + 1} Start Time Should not be grester than End Time";
                    //                return req;
                    //            }
                    //        }
                    //    }
                    //}
                }
            }

            //Loiter Schedule Updation
            for (int i = 0; i < x.LoiterSchedule.Count(); i++)
            {
                if (!(x.LoiterSchedule[i].fullday) && (x.LoiterSchedule[i].time.Count() >= 1))
                {
                    for (int j = 0; j < x.LoiterSchedule[i].time.Count(); j++)
                    {
                        x.LoiterSchedule[i].time[j].st = DateTime.Parse(x.LoiterSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                        x.LoiterSchedule[i].time[j].et = DateTime.Parse(x.LoiterSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                    }
                    req.LoiterSchedule.Add(x.LoiterSchedule[i]);
                }
                else
                {
                    if (x.LoiterSchedule[i].time.Count() > 0)
                    {
                        for (int j = 0; j < x.LoiterSchedule[i].time.Count(); j++)
                        {
                            x.LoiterSchedule[i].time[j].st = DateTime.Parse(x.LoiterSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                            x.LoiterSchedule[i].time[j].et = DateTime.Parse(x.LoiterSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                        }
                        req.LoiterSchedule.Add(x.LoiterSchedule[i]);
                    }
                    else
                    {
                        req.LoiterSchedule.Add(x.LoiterSchedule[i]);
                    }
                }
            }

            //Crowd Schedule Validation
            for (int i = 0; i < x.CrowdSchedule.Count(); i++)
            {
                if (!(x.CrowdSchedule[i].fullday) && (x.CrowdSchedule[i].time.Count() >= 1))
                {
                    if (x.CrowdSchedule[i].time.Count() >= 4)
                    {
                        req.ModelErrors = $"Crowd for day {x.CrowdSchedule[i].day} , Schedule should not have more than 3 time slots";
                        return req;
                    }
                    //else
                    //{
                    //    for (int j = 0; j < x.CrowdSchedule[i].time.Count(); j++)
                    //    {
                    //        if (j == 1)
                    //        {
                    //            if (DateTime.Parse(x.CrowdSchedule[i].time[0].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.CrowdSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Crowd schedule for {x.CrowdSchedule[i].day} , Slot {j + 1} Start Time Should not be less than End Time of Slot {j} ";
                    //                return req;
                    //            }
                    //        }
                    //        else if (j == 2)
                    //        {
                    //            if (DateTime.Parse(x.CrowdSchedule[i].time[1].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.CrowdSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Crowd schedule for {x.CrowdSchedule[i].day} , Slot {j + 1} Start Time Should not be less than End Time of Slot {j} ";
                    //                return req;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (DateTime.Parse(x.CrowdSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) > DateTime.Parse(x.CrowdSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                    //            {
                    //                req.ModelErrors = $"Crowd schedule for {x.CrowdSchedule[i].day} , Slot {j + 1} Start Time Should not be grester than End Time";
                    //                return req;
                    //            }
                    //        }
                    //    }
                    //}
                }
            }

            //Crowd Schedule Updation
            for (int i = 0; i < x.CrowdSchedule.Count(); i++)
            {
                if (!(x.CrowdSchedule[i].fullday) && (x.CrowdSchedule[i].time.Count() >= 1))
                {
                    for (int j = 0; j < x.CrowdSchedule[i].time.Count(); j++)
                    {

                        x.CrowdSchedule[i].time[j].st = DateTime.Parse(x.CrowdSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                        x.CrowdSchedule[i].time[j].et = DateTime.Parse(x.CrowdSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                    }
                    req.CrowdSchedule.Add(x.CrowdSchedule[i]);
                }
                else
                {
                    if (x.CrowdSchedule[i].time.Count() > 0)
                    {
                        for (int j = 0; j < x.CrowdSchedule[i].time.Count(); j++)
                        {
                            x.CrowdSchedule[i].time[j].st = DateTime.Parse(x.CrowdSchedule[i].time[j].st, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                            x.CrowdSchedule[i].time[j].et = DateTime.Parse(x.CrowdSchedule[i].time[j].et, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToString("HH:mm");
                        }
                        req.CrowdSchedule.Add(x.LoiterSchedule[i]);
                    }
                    else
                    {
                        req.CrowdSchedule.Add(x.CrowdSchedule[i]);
                    }
                }
            }

            return req;
        }

        private bool CheckUpdateAvailable(Device x, string newfirmwareVersion)
        {
            if (!string.IsNullOrEmpty(newfirmwareVersion) && !string.IsNullOrEmpty(x.CurrentFirmwareVersion))
            {
                //return !newfirmwareVersion.Equals(x.CurrentFirmwareVersion.Contains(".") ? x.CurrentFirmwareVersion : $"{x.CurrentFirmwareVersion}.0") ? true : false;
                //return newfirmwareVersion.Equals(
                //    x.CurrentFirmwareVersion.Contains(".") ? 
                //    x.CurrentFirmwareVersion : $"{x.CurrentFirmwareVersion}.0") ?
                //    false : newfirmwareVersion.Split(".")[0].Equals(x.CurrentFirmwareVersion.Contains(".") ?
                //    x.CurrentFirmwareVersion.Split(".")[0] : x.CurrentFirmwareVersion) ?
                //    true : false;
                if (Convert.ToDouble(newfirmwareVersion) > Convert.ToDouble(x.CurrentFirmwareVersion))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
    }
}
