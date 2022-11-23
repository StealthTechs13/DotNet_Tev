/* This is no longer in use , keeping commented for future use.
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
using Tev.Cosmos.IRepository;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.HelperService;
using Tev.DAL.RepoContract;
using Tev.IotHub;
using ZohoSubscription;
using ZohoSubscription.Models;

namespace Tev.API.Controllers
{
    /// <summary>
    /// APIs for Device Replacement
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class DeviceReplacementController : TevControllerBase
    {
        private readonly IGenericRepo<DeviceReplacement> _deviceReplacementRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeviceReplacementController> _logger;
        private readonly IUserDevicePermissionService _userDevicePermissionServices;
        private readonly IZohoAuthentication _zohoAuthentication;
        private readonly IZohoSubscription _zohoSubscription;
        private readonly ITevIoTRegistry _iotHub;
        private readonly IGenericRepo<ZohoSubscriptionHistory> _subscriptionRepo;
        private readonly IGenericRepo<FeatureSubscriptionAssociation> _featureSubscriptionRepo;
        private readonly IGenericRepo<DeviceDetachedHistory> _deviceDetachedRepo;
        private readonly IDeviceRepo _deviceRepo;

        public DeviceReplacementController(
            IGenericRepo<DeviceReplacement> deviceReplacementRepo,
            IUnitOfWork unitOfWork,
            ILogger<DeviceReplacementController> logger,
            IUserDevicePermissionService userDevicePermissionServices,
            IZohoAuthentication zohoAuthentication, IZohoSubscription zohoSubscription, ITevIoTRegistry iotHub,
            IGenericRepo<ZohoSubscriptionHistory> subscriptionRepo, IGenericRepo<FeatureSubscriptionAssociation> featureSubscriptionRepo,
            IGenericRepo<DeviceDetachedHistory> deviceDetachedRepo, IDeviceRepo deviceRepo
            )
        {
            _deviceReplacementRepo = deviceReplacementRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userDevicePermissionServices = userDevicePermissionServices;
            _zohoAuthentication = zohoAuthentication;
            _zohoSubscription = zohoSubscription;
            _iotHub = iotHub;
            _subscriptionRepo = subscriptionRepo;
            _featureSubscriptionRepo = featureSubscriptionRepo;
            _deviceDetachedRepo = deviceDetachedRepo;
            _deviceRepo = deviceRepo;
        }



        /// <summary>
        /// Report a Dead Device
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("reportDeadDevice")]
        [ProducesResponseType(typeof(MMSHttpReponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateDeviceReplacement([FromBody] DeviceReplacementRequest model)
        {
            try
            {
                if ((IsOrgAdmin(CurrentApplications) || _userDevicePermissionServices.GetDeviceIdForOwner(UserEmail).Contains(model.DeviceId) || 
                    _userDevicePermissionServices.GetDeviceIdForEditor(UserEmail).Contains(model.DeviceId)))
                {

                    var data = _deviceReplacementRepo.Query(z => z.DeviceId == model.DeviceId && z.ReplaceStatus != ReplaceStatusEnum.Closed).FirstOrDefault();

                    if (data != null)
                    {
                        return Ok(new MMSHttpReponse { SuccessMessage = $"replacement request exist for the device and the current status is {data.ReplaceStatus.GetName()}" });
                    }
                    else
                    {
                        using (var transaction = _unitOfWork.BeginTransaction())
                        {
                            try
                            {
                                var resp = new DeviceReplacement()
                                {
                                    DeviceId = model.DeviceId,
                                    OrgId = OrgId,
                                    Comments = model.Comment,
                                    Email = UserEmail,
                                    ReplaceStatus = ReplaceStatusEnum.Open,
                                    CreatedBy = UserEmail,
                                    ModifiedBy = UserEmail
                                };

                                _deviceReplacementRepo.Add(resp);
                                _deviceReplacementRepo.SaveChanges();

                               
                                var device = await _deviceRepo.GetDevice(model.DeviceId, OrgId);

                                if(device == null)
                                {
                                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Device not found" });
                                }

                                var deviceDetachedHistory = new DeviceDetachedHistory
                                {
                                    LogicalDetachedDeviceId = device.LogicalDeviceId,
                                    PhysicalDetachedDeviceId = device.Id,
                                    OrgId = OrgId
                                };

                                _deviceDetachedRepo.Add(deviceDetachedHistory);
                                _deviceDetachedRepo.SaveChanges();

                                if (device.Subscription?.SubscriptionId != null)
                                {
                                    var token = await _zohoAuthentication.GetZohoToken();
                                    RetrieveSubscriptionDetailsResponse subcriptionDetails = null;
                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        var postData = new ZohoSubscriptionHistory();
                                        if(device.Subscription.SubscriptionStatus == nameof(SubscriptionStatus.live))
                                        {
                                             subcriptionDetails = await _zohoSubscription.PauseSubscription(token, device.Subscription.SubscriptionId, model.Comment);
                                        }
                                        else
                                        {
                                             subcriptionDetails = await _zohoSubscription.GetSubscription(token, device.Subscription.SubscriptionId);
                                        }

                                        if (!subcriptionDetails.message.Equals("error"))
                                        {
                                            postData = new ZohoSubscriptionHistory()
                                            {
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
                                }

                                await _iotHub.DeleteDeviceFromDeviceTwin(device.Id);
                                await _deviceRepo.DeleteDevice(device.LogicalDeviceId, OrgId);

                                transaction.Commit();

                                _logger.LogInformation("device replacement successfully added on sql");

                                return Ok(new MMSHttpReponse<int>() { ResponseBody = resp.Id, SuccessMessage = "device replacement successfully created." });
                            }
                            catch (Exception e)
                            {
                                _logger.LogError("error occured on adding device replacement on sql {exception}", e);
                                transaction.Rollback();
                                return StatusCode(StatusCodes.Status500InternalServerError);
                            }
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
                _logger.LogError("Error Occured on Add device replacement on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get Device Replacement by Device Id
        /// </summary>
        /// <param name="deviceId">DeviceId</param>
        /// <returns></returns>
        [HttpGet("getDeviceReplacementByDeviceId")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<DeviceReplacementModel>>), StatusCodes.Status200OK)]
        public IActionResult GetDeviceReplacementByDeviceId([FromQuery] string deviceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return Ok(new MMSHttpReponse<DeviceReplacementModel>() { ErrorMessage = "Invalid DeviceId" });
                }

                // (device should exist and user is org admin ) or (user is site admin and Device belongs to that site)
                if ((IsOrgAdmin(CurrentApplications) || _userDevicePermissionServices.GetDeviceIdForViewer(UserEmail).Contains(deviceId)))
                {
                    var deviceReplacements = _deviceReplacementRepo.Query(z => z.DeviceId == deviceId).Select(z => new DeviceReplacementModel()
                    {
                        DeviceReplacementId = z.Id,
                        Comments = z.Comments,
                        DeviceId = z.DeviceId,
                        Email = z.Email,
                        OrgId = z.OrgId,
                        ReplaceStatus = z.ReplaceStatus.GetName()
                    }).ToList();

                    var resp = new MMSHttpReponse<List<DeviceReplacementModel>>()
                    {
                        ResponseBody = deviceReplacements
                    };
                    return Ok(resp);
                }
                else
                {
                    return Ok(Forbid());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on get device replacement by Id {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
*/



