using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using ZohoSubscription;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using Tev.API.Models;
using ZohoSubscription.Models;
using Tev.IotHub;
using Tev.API.Service;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.DAL;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using MMSConstants;
using Tev.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Tev.IotHub.Models;
using System.ComponentModel.DataAnnotations;
using Tev.Cosmos.IRepository;
using Tev.Cosmos.Entity;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class ZohoSubscriptionController : TevControllerBase
    {
        private readonly IZohoAuthentication _zohoAuthentication;
        private readonly IConfiguration _configuration;
        private readonly IZohoSubscription _zohoSubscription;
        private readonly ITevIoTRegistry _tevIotRegistry;
        private readonly IZohoService _zohoService;
        private readonly IGenericRepo<ZohoSubscriptionHistory> _subscriptionRepo;
        private readonly IGenericRepo<FeatureSubscriptionAssociation> _featureSubscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ZohoSubscriptionController> _logger;
       
        private readonly IMemoryCache _memoryCache;
        private readonly IGenericRepo<InvoiceHistory> _invoiceHistoryRepo;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly IGenericRepo<DeviceDetachedHistory> _deviceDetachedRepo;
        private readonly IDeviceRepo _deviceRepo;

        public ZohoSubscriptionController(IZohoAuthentication zohoAuthentication, 
            IConfiguration configuration,IZohoSubscription zohoSubscription, ITevIoTRegistry tevIoTRegistry, IZohoService zohoService,
            IUnitOfWork unitOfWork, ILogger<ZohoSubscriptionController> logger, IMemoryCache memoryCache,
            IGenericRepo<ZohoSubscriptionHistory> subscriptionRepo, IGenericRepo<FeatureSubscriptionAssociation> featureSubscriptionRepo,
            IGenericRepo<DeviceDetachedHistory> deviceDetachedRepo,IGenericRepo<UserDevicePermission> userDevicePermissionRepo, 
            IGenericRepo<InvoiceHistory> invoiceHistoryRepo, IDeviceRepo deviceRepo)
        {
            _zohoAuthentication = zohoAuthentication;
            _configuration = configuration;
            _zohoSubscription = zohoSubscription;
            _tevIotRegistry = tevIoTRegistry;
            _zohoService = zohoService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _memoryCache = memoryCache;
            _subscriptionRepo = subscriptionRepo;
            _featureSubscriptionRepo = featureSubscriptionRepo;
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _deviceDetachedRepo = deviceDetachedRepo;
            _invoiceHistoryRepo = invoiceHistoryRepo;
            _deviceRepo = deviceRepo;
        }





        /// <summary>
        /// Get all active plan for a product and device 
        ///   1. Purchase New - get all plans for that product 
        ///   2. Modify - get the current plan.
        ///   3. Renew - Get all the plans for that product.
        /// </summary>
        /// <param name="product">TEV or WSD</param>
        /// <param name="deviceId">Logical device id</param>
        /// <returns></returns>
        [HttpGet("GetPlans")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(MMSHttpReponse<ZohoSubscription.Models.Plan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPlans([Required]string product,[Required]string deviceId)
        {
            try
            {

                if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Access Denied" });
                }

                var planStatus = GetSubscriptionStatus(deviceId);

                if(planStatus.Item1 == PlanStatus.NonRenewing)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage= "Please try after sometime" });
                }

                var token = await _zohoAuthentication.GetZohoToken();
               
                if (!string.IsNullOrEmpty(token))
                {
                    var products = await this.GetZohoProducts(token);

                    var plans = new List<ZohoSubscription.Models.Plan>();

                    var productObj = products.Find(x => x.name == product);

                    if(productObj == null)
                    {
                        return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = plans });
                    }

                    var productId = productObj.product_id;
                    var promoPlanCodeList = _configuration.GetSection("Zoho").GetSection("PromoPlanCode").Value.Split(',');
                    var wsdFreeOneYearPlanCode = _configuration.GetSection("Zoho").GetSection("WSDFreeOneYearPlanCode").Value;
                    var testPlanCodeList = _configuration.GetSection("Zoho").GetSection("TestPlanCode").Value.Split(',');
                    var exceptionEmails = _configuration.GetSection("Zoho").GetSection("ExceptionEmails").Value.Split(',');// exceptionEmails are the list of sales/quality people emails
                    var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                    var subscriptionHistory = _subscriptionRepo.Query(z => z.PhysicalDeviceId == device.Id && z.PlanCode == wsdFreeOneYearPlanCode).FirstOrDefault();

                    if (!_memoryCache.TryGetValue("Plans", out plans) || subscriptionHistory == null)
                    {
                        plans = await _zohoSubscription.GetPlans(token, product);

                        if (plans == null)
                        {
                            return BadRequest(new MMSHttpReponse { ErrorMessage = "error occured while retrieving plans, please try after sometime." });
                        }
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                                    .SetAbsoluteExpiration(TimeSpan.FromHours(2));

                        _memoryCache.Set("Plans", plans, cacheEntryOptions);

                        switch (planStatus.Item1)
                        {
                            case PlanStatus.Update:
                                plans = plans.Where(x => x.product_id == productId && x.plan_code == planStatus.Item2).ToList();
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = plans });
                            case PlanStatus.Renew:
                                if ((UserEmail.Split('@')[1].ToLower() == "honeywell.com" && (productObj.name == nameof(Applications.TEV) || productObj.name == nameof(Applications.TEV2))) ||
                                    (exceptionEmails.Contains(UserEmail) && productObj.name == nameof(Applications.WSD)))
                                    plans = plans.Where(x => x.product_id == productId && !wsdFreeOneYearPlanCode.Contains(x.plan_code)).ToList();
                                else
                                    plans = plans.Where(x => x.product_id == productId && !promoPlanCodeList.Contains(x.plan_code) && !testPlanCodeList.Contains(x.plan_code) && !wsdFreeOneYearPlanCode.Contains(x.plan_code)).ToList();
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = plans });
                            case PlanStatus.New:
                                if((UserEmail.Split('@')[1].ToLower() == "honeywell.com" && (productObj.name == nameof(Applications.TEV) ||  productObj.name == nameof(Applications.TEV2))) ||
                                    (exceptionEmails.Contains(UserEmail) && productObj.name == nameof(Applications.WSD)))
                                    plans = plans.Where(x => x.product_id == productId && !wsdFreeOneYearPlanCode.Contains(x.plan_code)).OrderBy(x => x.recurring_price).ToList();
                                else if((subscriptionHistory == null || subscriptionHistory.Status != nameof(SubscriptionStatus.live) || subscriptionHistory.Status == nameof(SubscriptionStatus.non_renewing)) && productObj.name == nameof(Applications.WSD))
                                    plans = plans.Where(x => x.product_id == productId && !promoPlanCodeList.Contains(x.plan_code)  && !testPlanCodeList.Contains(x.plan_code)).OrderBy(x => x.recurring_price).ToList();//Free Plans should display on Top for WSD.
                                else
                                    plans = plans.Where(x => x.product_id == productId && !promoPlanCodeList.Contains(x.plan_code) && !wsdFreeOneYearPlanCode.Contains(x.plan_code) && !testPlanCodeList.Contains(x.plan_code)).OrderBy(x => x.recurring_price).ToList();
                                
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = plans });
                            default:
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = plans });


                        }
                    }
                    else
                    {
                        var result = (List<ZohoSubscription.Models.Plan>)_memoryCache.Get("Plans");
                        var planList = new List<ZohoSubscription.Models.Plan>();

                        switch (planStatus.Item1)
                        {
                            case PlanStatus.Update:
                                planList = result.Where(x => x.product_id == productId && x.plan_code == planStatus.Item2).ToList();
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = planList });
                            case PlanStatus.Renew:
                                if ((UserEmail.Split('@')[1].ToLower() == "honeywell.com" && productObj.name == nameof(Applications.TEV) || productObj.name == nameof(Applications.TEV2)) ||
                                    (exceptionEmails.Contains(UserEmail) && productObj.name == nameof(Applications.WSD)))
                                    plans = plans.Where(x => x.product_id == productId && !wsdFreeOneYearPlanCode.Contains(x.plan_code)).ToList();
                                else
                                    plans = plans.Where(x => x.product_id == productId && !wsdFreeOneYearPlanCode.Contains(x.plan_code) && !promoPlanCodeList.Contains(x.plan_code) && !testPlanCodeList.Contains(x.plan_code)).ToList();
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = plans });
                            case PlanStatus.New:
                                if ((UserEmail.Split('@')[1].ToLower() == "honeywell.com" && productObj.name == nameof(Applications.TEV) || productObj.name == nameof(Applications.TEV2)) ||
                                    (exceptionEmails.Contains(UserEmail) && productObj.name == nameof(Applications.WSD)))
                                    plans = plans.Where(x => x.product_id == productId && !wsdFreeOneYearPlanCode.Contains(x.plan_code)).ToList();
                                else if (subscriptionHistory != null && productObj.name == nameof(Applications.WSD))
                                    plans = plans.Where(x => x.product_id == productId && !promoPlanCodeList.Contains(x.plan_code)  && !wsdFreeOneYearPlanCode.Contains(x.plan_code) && !testPlanCodeList.Contains(x.plan_code)).ToList();
                                else
                                    plans = plans.Where(x => x.product_id == productId && !promoPlanCodeList.Contains(x.plan_code) && !wsdFreeOneYearPlanCode.Contains(x.plan_code) && !testPlanCodeList.Contains(x.plan_code)).ToList();
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = plans });
                            default:
                                return Ok(new MMSHttpReponse<List<ZohoSubscription.Models.Plan>> { ResponseBody = planList });
                              
                        }
                        
                    }
                }
                else
                {
                    _logger.LogError($"error in GetPlans action method in ZohoSubscription controller :- zoho access token is empty");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in GetPlans api in ZohoSubscriptionController {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Create new subscription online - returns a zoho payment hosted page url
        /// </summary>
        /// <param name="zohoSubscriptionRequest"></param>
        /// <returns>zoho payment hosted page url</returns>
        [HttpPost("CreateNewSubscription")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateNewSubscription([FromBody] CreateNewSubscriptionRequest zohoSubscriptionRequest)
        {
            
            try
            {
                if (zohoSubscriptionRequest == null || string.IsNullOrEmpty(zohoSubscriptionRequest.DeviceId) 
                    || string.IsNullOrEmpty(zohoSubscriptionRequest.PlanCode)
                    || zohoSubscriptionRequest.Addons.Length <= 0)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Ivalid input, please make sure you have selected atleast one addon"});
                }

                if (!await IsDeviceAuthorizedAsAdmin(zohoSubscriptionRequest.DeviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Access Denied" });
                }

                var subscriptionHistory = _subscriptionRepo.Query(z => z.DeviceId == zohoSubscriptionRequest.DeviceId && z.OrgId == OrgId).OrderByDescending(z => z.CreatedDate).ToList();

                if (subscriptionHistory.Count > 0)
                {
                    return Ok(new MMSHttpReponse<string>()
                    {
                        SuccessMessage = "subscriptionexist",
                        ResponseBody = $"The selected device already has a existing subscription & the current state is - {subscriptionHistory[0].Status} , " +
                        "So please close the browser and go back to the mobile app."
                    });
                }

                var device = await _deviceRepo.GetDevice(zohoSubscriptionRequest.DeviceId,OrgId);
                _logger.LogInformation($"Device data retrived ");
                var token = await _zohoAuthentication.GetZohoToken();
                var couponCode = await GetCoupenToApply(token, zohoSubscriptionRequest.PlanCode, zohoSubscriptionRequest.Addons, device.DeviceType);
                _logger.LogInformation($"coupon code retrived :- {couponCode} ");
                if (!string.IsNullOrEmpty(token))
                {
                    var result = await _zohoSubscription.CreateNewSubscription(token, ZohoId, zohoSubscriptionRequest.PlanCode, zohoSubscriptionRequest.DeviceId, 
                        zohoSubscriptionRequest.Addons, zohoSubscriptionRequest.Headless, OrgId, couponCode);
                    _logger.LogInformation($"Result :- {result}");
                    return Ok(new MMSHttpReponse<string> { SuccessMessage = "success", ResponseBody = result});
                }
                else
                {
                    _logger.LogError($"zoho access token is empty");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                
            }
            catch(Exception ex)
            {
                _logger.LogError("Error in CreateNewSubscription action method in ZohoSubscriptionController {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Push Subscription Details to Device
        /// </summary>
        /// <param name="hostedpageId"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("PushSubscriptionToDevice")]
        [ProducesResponseType(typeof(MMSHttpReponse<SubscriptionDetails>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSubscriptionByHostedPageId(string hostedpageId,string deviceId)
        {
            if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
            {
                return Forbid();
            }
            try
            {
                var token = await _zohoAuthentication.GetZohoToken();
                var result = await _zohoService.HostedPage(token, hostedpageId,deviceId);

                return Ok(new MMSHttpReponse<SubscriptionDetails> { ResponseBody = result});
            }
            catch(Exception ex)
            {
                _logger.LogError("Error in PushSubscriptionToDevice api in ZohoSubscriptionController {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieve all active subscription 
        /// </summary>
        /// <returns></returns>
        [HttpGet("RetrieveAllActiveSubscription")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<CustomerSubscription>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError )]
        public async Task<IActionResult> RetrieveAllSubscription()
        {
            try
            {
                if (!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2))
                {
                    return Forbid();
                }
                var token = await _zohoAuthentication.GetZohoToken();

                if (!string.IsNullOrEmpty(token))
                {
                    var result = await _zohoSubscription.RetrieveAllSubscription(token, ZohoId);
                    return Ok(new MMSHttpReponse<List<CustomerSubscription>> { ResponseBody = result }); 
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
                   
            }
            catch(Exception ex)
            {
                _logger.LogError("Error in RetrieveAllSubscription api in ZohoSubscriptionController {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// UpdateSubscriptionOnline - returns a zoho payment hosted page url for upgrading the subscription.
        /// </summary>
        /// <param name="zohoSubscriptionRequest"></param>
        /// <returns></returns>
        [HttpPost("UpdateSubscriptionOnline")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSubscriptionOnline([FromBody] ZohoSubscriptionRequest zohoSubscriptionRequest)
        {
            try
            {
                if (zohoSubscriptionRequest == null || string.IsNullOrEmpty(zohoSubscriptionRequest.DeviceId) || string.IsNullOrEmpty(zohoSubscriptionRequest.SubscriptionId)
                    || string.IsNullOrEmpty(zohoSubscriptionRequest.PlanCode)
                    || zohoSubscriptionRequest.Addons.Length <= 0)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Ivalid input, please make sure you have selected atleast one addon" });
                }
                if (!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2) || !IsSubscriptionAuthorized(zohoSubscriptionRequest.DeviceId, zohoSubscriptionRequest.SubscriptionId))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Access Denied" });
                }

                var token = await _zohoAuthentication.GetZohoToken();

                var device = await _deviceRepo.GetDevice(zohoSubscriptionRequest.DeviceId,OrgId);

                var couponCode = string.Empty;

                if (device?.Subscription?.SubscriptionStatus == nameof(SubscriptionStatus.cancelled))
                {
                    couponCode = await GetCoupenToApply(token, zohoSubscriptionRequest.PlanCode, zohoSubscriptionRequest.Addons, device.DeviceType);
                }

                if (!string.IsNullOrEmpty(zohoSubscriptionRequest.SubscriptionId))
                {
                    var url = await _zohoSubscription.UpgradeSubscription(token, zohoSubscriptionRequest.SubscriptionId, zohoSubscriptionRequest.PlanCode, 
                        zohoSubscriptionRequest.DeviceId, zohoSubscriptionRequest.Addons, zohoSubscriptionRequest.Headless, ZohoId, OrgId, couponCode);
                    return Ok(new MMSHttpReponse<string> { ResponseBody = url });
                }
                else
                {
                    _logger.LogError("Error in UpdateSubscriptionOnline api :- subscriptionId is empty");
                    return BadRequest();
                }
                   
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UpdateSubscriptionOnline api in ZohoSubscriptionController {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// UpdateSubscriptionOffline - update the subscription offline with out collecting payment online 
        /// </summary>
        /// <param name="zohoSubscriptionRequest"></param>
        /// <returns></returns>
        [HttpPost("UpdateSubscriptionOffline")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSubscriptionOffline([FromBody] ZohoSubscriptionRequest zohoSubscriptionRequest)
        {
            try
            {
                if (zohoSubscriptionRequest == null || string.IsNullOrEmpty(zohoSubscriptionRequest.DeviceId) || string.IsNullOrEmpty(zohoSubscriptionRequest.SubscriptionId)
                    || string.IsNullOrEmpty(zohoSubscriptionRequest.PlanCode)
                    || zohoSubscriptionRequest.Addons.Length <= 0)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Invalid input, please make sure you have selected atleast one addon" });
                }

                if (!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2) || !IsSubscriptionAuthorized(zohoSubscriptionRequest.DeviceId, zohoSubscriptionRequest.SubscriptionId))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Access Denied" });
                }
                var token = await _zohoAuthentication.GetZohoToken();

                if (!string.IsNullOrEmpty(zohoSubscriptionRequest.SubscriptionId))
                {
                    var device =  await _deviceRepo.GetDevice(zohoSubscriptionRequest.DeviceId, OrgId);

                    var subcriptionDetails = await _zohoSubscription.DowngradeSubscription(token, zohoSubscriptionRequest.SubscriptionId, zohoSubscriptionRequest.PlanCode, 
                        zohoSubscriptionRequest.DeviceId, zohoSubscriptionRequest.Addons, zohoSubscriptionRequest.Headless, ZohoId, OrgId);

                    if (!subcriptionDetails.message.Equals("error"))
                    {
                        var isSuccess = UpdateSubscriptionHistory(subcriptionDetails,device.LogicalDeviceId,device.DeviceName);

                        var addOns = subcriptionDetails.subscription.addons;
                        var ret = new List<int>();
                        addOns.ForEach(x => ret.Add(Helper.GetAlertType(x.addon_code)));

                        var redirectUrl = String.Empty;

                        if (zohoSubscriptionRequest.Headless)
                        {
                            redirectUrl = //this._configuration.GetSection("TevWebUri").Value +
                              this._configuration.GetSection("Zoho").GetSection("HostedPageRedirectUriHeadlessTrue").Value +
                              zohoSubscriptionRequest.Headless + "&deviceId=" + zohoSubscriptionRequest.DeviceId;
                        }
                        else
                        {
                            redirectUrl = this._configuration.GetSection("Zoho").GetSection("HostedPageRedirectUriHeadlessTrue").Value + zohoSubscriptionRequest.Headless + "&deviceId=" + zohoSubscriptionRequest.DeviceId;
                        }
                        //Device twin update
                        await _tevIotRegistry.UpdateTwin(subcriptionDetails.subscription.current_term_ends_at, true, zohoSubscriptionRequest.DeviceId,subcriptionDetails.subscription.subscription_id,
                            ret.ToArray(), subcriptionDetails.subscription.status, SubscriptionEventType.subscription_modified);

                        //Update Cosmos Device Data
                        if(device != null)
                        {
                            device.Subscription.SubscriptionStatus = subcriptionDetails.subscription.status;
                            device.Subscription.PlanName = subcriptionDetails.subscription.plan.name;
                            device.Subscription.Amount = Convert.ToString(subcriptionDetails.subscription.amount);
                            device.Subscription.SubscriptionExpiryDate = subcriptionDetails.subscription.current_term_ends_at;
                            device.Subscription.AvailableFeatures = ret.ToArray();
                            device.TwinChangeStatus = TwinChangeStatus.SubscriptionModified;

                            await _deviceRepo.UpdateDevice(OrgId, device);
                        }
                        

                        return Ok(new MMSHttpReponse<string> { ResponseBody = redirectUrl , SuccessMessage = "success" });
                    }
                    else
                    {
                        _logger.LogError("Error in UpdateSubscriptionOffline api, DowngradeSubscription zoho api failed: = {message}", subcriptionDetails.message);
                        return BadRequest();
                    }
                }
                else
                {
                    _logger.LogError("Error in UpdateSubscriptionOffline api :- subscriptionId is empty");
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UpdateSubscriptionOffline api in ZohoSubscriptionController {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Retrieve subscription details
        /// </summary>
        /// <param name="subscriptionId">Subscription id</param>
        /// <returns></returns>
        [HttpGet("SubscriptionDetail")]
        [ProducesResponseType(typeof(MMSHttpReponse<ZohoRetrieveSubscriptionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubscriptionDetail(string subscriptionId)
        {
            try
            {
                if (!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2) || !IsSubscriptionAuthorized(subscriptionId))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Access Denied" });
                }
                if (string.IsNullOrEmpty(subscriptionId))
                {
                    return BadRequest();
                }
                var token = await _zohoAuthentication.GetZohoToken();
                if (!string.IsNullOrEmpty(token))
                {
                    var result = await _zohoSubscription.GetSubscription(token, subscriptionId);

                    if (!result.message.Equals("error"))
                    {
                        var zohoAddOns = new List<Tev.API.Models.ZohoAddon>();
                        foreach (var item in result.subscription.addons)
                        {
                            var zohoAddOn = new Tev.API.Models.ZohoAddon();
                            zohoAddOn.AddOnCode = item.addon_code;
                            zohoAddOn.Name = item.name;
                            zohoAddOn.Description = item.description;
                            zohoAddOn.Price = item.price;
                            zohoAddOn.Quantity = item.quantity;
                            zohoAddOns.Add(zohoAddOn);
                        }

                        var taxes = new List<Tev.API.Models.ZohoTax>();
                        foreach (var item in result.subscription.taxes)
                        {
                            var tax = new Tev.API.Models.ZohoTax
                            {
                                Id = item.tax_id,
                                Name = item.tax_name,
                                Amount = item.tax_amount
                            };
                            taxes.Add(tax);
                        }

                        var data = new Tev.API.Models.ZohoRetrieveSubscriptionResponse
                        {
                            SubscriptionId = result.subscription.subscription_id,
                            Name = result.subscription.name,
                            Status = result.subscription.status,
                            Amount = result.subscription.amount,
                            SubTotal = result.subscription.sub_total,
                            CreatedDate = Convert.ToDateTime(result.subscription.created_at).ToString("dd-MM-yyy"),
                            ActivatedDate = Convert.ToDateTime(result.subscription.activated_at).ToString("dd-MM-yyy"),
                            ExpiryDate = Convert.ToDateTime(result.subscription.current_term_ends_at).ToString("dd-MM-yyy"),
                            Interval = result.subscription.interval,
                            IntervalUnit = result.subscription.interval_unit,
                            BillingMode = result.subscription.billing_mode,
                            ProductId = result.subscription.product_id,
                            ProductName = result.subscription.product_name,
                            Plan = new ZohoPlan
                            {
                                PlanCode = result.subscription.plan.plan_code,
                                Name = result.subscription.plan.name,
                                Description = result.subscription.plan.description,
                                Price = result.subscription.plan.price,
                                Quantity = result.subscription.plan.quantity
                            },
                            AddOns = zohoAddOns,
                            Taxes = taxes
                        };
                        return Ok(new MMSHttpReponse<ZohoRetrieveSubscriptionResponse> { ResponseBody = data, SuccessMessage="success" });
                    }
                    else
                    {
                        _logger.LogError("error in GetSubscriptionDetials action method in ZohoSubscription controller :- {e}", result.message);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogError($"error in GetSubscriptionDetials action method in ZohoSubscription controller :- zoho access token is empty");
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("error in GetSubscriptionDetials action method in ZohoSubscription controller :- {e}",ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Order history - retrieve subscription event's related to all subscription which the end user has.
        /// </summary>
        /// <returns>List of ZohoSubscriptionHistory</returns>
        [HttpGet("SubscriptionHistory")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<ZohoSubscriptionHistory>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubscriptionHistory()
        {
            try
            {
                if (!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2))
                {
                    return Forbid();
                }

                var result = _subscriptionRepo.Query(x => x.OrgId == OrgId).OrderByDescending(x=> x.CreatedDate).Include(x => x.Features).ToList();

                if(result.Count > 0)
                {
                    var deviceData = await _deviceRepo.GetDeviceByDeviceIds(result.Select(o => o.DeviceId).Distinct().ToList(), OrgId);

                    result.ForEach(x =>
                    {
                        x.EventType = Helper.Replace_withWhiteSpaceAndMakeTheStringIndexValueUpperCase(x.EventType);
                        x.Status = (x.Status == "live") ? "Active" : (x.Status == "canceled") ? "Expired" : x.Status.Substring(0, 1).ToUpper() + x.Status.Substring(1);
                        x.DeviceName = deviceData.Find(y => y.LogicalDeviceId == x.DeviceId) == null ? x.DeviceName : deviceData.Find(y => y.LogicalDeviceId == x.DeviceId).DeviceName;
                    });
                }

                return Ok(new MMSHttpReponse<List<ZohoSubscriptionHistory>> { ResponseBody = result, SuccessMessage = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in SubscriptionHistory api :- {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("InvoiceHistory")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<InvoiceHistoryResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public IActionResult InvoiceHistory()
        {
            try
            {
                var ret = _invoiceHistoryRepo.Query(x => x.Email == UserEmail).Include(x => x.InvoiceHistorySubscriptionAssociations).Include(x => x.InvoiceItems)
                    .Include(x => x.Payments).ToList();
                var result = ret.Select(x => MapInvoiceHistoryDTO(x)).ToList();
                return Ok(new MMSHttpReponse<List<InvoiceHistoryResponse>> { ResponseBody = result, SuccessMessage = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in InvoiceHistory api :- {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Download invoice
        /// </summary>
        /// <param name="invoiceId"> Invoice id</param>
        /// <returns></returns>
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [HttpGet("DownloadInvoice/{invoiceId}")]
        public async Task<IActionResult> DownloadInvoice(string invoiceId)
        {
            try
            {
                var token = await _zohoAuthentication.GetZohoToken();

                var response = await _zohoSubscription.DownloadInvoice(token, invoiceId);

                if (response.Content == null)
                {
                    _logger.LogError($"Download invoice failed following are the reason :- invoiceId not exist");
                    return NotFound();
                }
                else
                {
                    var content = new System.IO.MemoryStream(await response.Content.ReadAsByteArrayAsync());

                    var contentType = OtherConstants.ContentTypeOctactStream;
                    return File(content, contentType, $"invoice_{invoiceId}.pdf");
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError("Download invoice failed following are the issue - {e}",ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Cancel subscription -  schedule to cancel the subscription at the end of the term. 
        /// </summary>
        /// <param name="cancelSubscriptionRequest"></param>
        /// <returns></returns>
        [HttpPost("CancelSubscription")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelSubscription(CancelOrReactivateSubscriptionRequest cancelSubscriptionRequest)
        {
            try
            {

                if(cancelSubscriptionRequest == null || cancelSubscriptionRequest.SubscriptionId == null || cancelSubscriptionRequest.DeviceId == null)
                {
                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "Something went wrong, please try again"
                    });
                }

                if (!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2) || !IsSubscriptionAuthorized(cancelSubscriptionRequest.DeviceId, cancelSubscriptionRequest.SubscriptionId))
                {
                    return Forbid();
                }

                var result = _subscriptionRepo.Query(x => x.OrgId == OrgId && x.DeviceId == cancelSubscriptionRequest.DeviceId).OrderByDescending(x => x.CreatedDate).Include(x => x.Features).ToList();

                var promoPlanCodeList = _configuration.GetSection("Zoho").GetSection("PromoPlanCode").Value.Split(',');

                if (result.Count > 0 && promoPlanCodeList.Contains(result[0].PlanCode))
                {
                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "This action is not available with promotional plan"
                    });
                }

                if (result.Count > 0 && result[0].EventType == nameof(SubscriptionEventType.subscription_cancellation_scheduled))
                {
                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "Please try again after sometime.."
                    });
                }
               
                if(result.Count > 0 && result[0].Status != nameof(SubscriptionStatus.cancelled) && result[0].Status !=  nameof(SubscriptionStatus.expired) && result[0].Status != nameof(SubscriptionStatus.non_renewing))
                {
                    var token = await _zohoAuthentication.GetZohoToken();
                    var response = await _zohoSubscription.CancelSubscription(token, cancelSubscriptionRequest.SubscriptionId, true);

                    if (!response.Equals("error"))
                    {
                        return Ok(new MMSHttpReponse<string>
                        {
                            SuccessMessage = "success",
                            ResponseBody = $"Your device - {cancelSubscriptionRequest.DeviceName} subscription will be canceled at the end of this term."
                        });
                    }
                    else
                    {
                        _logger.LogError($"error in cancel subscription :- zoho cancel api call failed");
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {

                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "Cancel subscription is possible only if the subscription status is live"
                    });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("error in CancelSubscription api - {e}",ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Re-activate subscription - remove cancel subscription scheduler 
        /// </summary>
        /// <param name="reactivateSubscriptionRequest"></param>
        /// <returns></returns>
        [HttpPost("ReactivateSubscription")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReactivateSubscription(CancelOrReactivateSubscriptionRequest reactivateSubscriptionRequest)
        {
            try
            {
                if(reactivateSubscriptionRequest == null || reactivateSubscriptionRequest.DeviceId == null || reactivateSubscriptionRequest.SubscriptionId == null)
                {
                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "Something went wrong, please try again"
                    });
                }

                if(!IsOrgAdmin(Applications.TEV) || !IsOrgAdmin(Applications.TEV2) || !IsSubscriptionAuthorized(reactivateSubscriptionRequest.DeviceId, reactivateSubscriptionRequest.SubscriptionId))
                {
                    return Forbid();
                }

                var result = _subscriptionRepo.Query(x => x.OrgId == OrgId && x.DeviceId == reactivateSubscriptionRequest.DeviceId).OrderByDescending(x => x.CreatedDate).Include(x => x.Features).ToList();

                var promoPlanCodeList = _configuration.GetSection("Zoho").GetSection("PromoPlanCode").Value.Split(',');


                if (result.Count > 0 && promoPlanCodeList.Contains(result[0].PlanCode))
                {
                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "This action is not available with promotional plan"
                    });
                }

                if (result.Count > 0 && result[0].EventType == nameof(SubscriptionEventType.subscription_scheduled_cancellation_removed))
                {
                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "Please try again after sometime.."
                    });
                }

                if(result.Count > 0 && result[0].EventType == nameof(SubscriptionEventType.subscription_cancellation_scheduled))
                {
                    var token = await _zohoAuthentication.GetZohoToken();

                    var response = await _zohoSubscription.ReactivateSubscription(token, reactivateSubscriptionRequest.SubscriptionId);
                    if (!response.Equals("error"))
                    {
                        return Ok(new MMSHttpReponse<string>
                        {
                            SuccessMessage = "success",
                            ResponseBody = $"Your device - {reactivateSubscriptionRequest.DeviceName} subscription has been reactivated successfully."
                        });
                    }
                    else
                    {
                        _logger.LogError($"error in reactivate subscription :- zoho reactivate subscription api call failed");
                        return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
                    }
                }
                else
                {
                    return BadRequest(new MMSHttpReponse
                    {
                        ErrorMessage = "Re-activate subscription is possible only if the subscription status is non_renewing"
                    });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("error in ReactivateSubscription api - {e}",ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Calculate cost - returns the amount end user needs to pay.
        /// </summary>
        /// <param name="zohoSubscriptionRequest"></param>
        /// <returns></returns>
        [HttpPost("ComputeCost")]
        [ProducesResponseType(typeof(MMSHttpReponse<double>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ComputeCost([FromBody] ComputeCostRequest zohoSubscriptionRequest)
        {
            try
            {
                if(zohoSubscriptionRequest == null || string.IsNullOrEmpty(zohoSubscriptionRequest.SubscriptionId) || string.IsNullOrEmpty(zohoSubscriptionRequest.PlanCode)
                     || zohoSubscriptionRequest.Addons.Length <= 0)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Ivalid input, please make sure you have selected atleast one addon" });
                }
                var token = await _zohoAuthentication.GetZohoToken();
                var response = await _zohoSubscription.ComputeCost(token, zohoSubscriptionRequest.SubscriptionId, zohoSubscriptionRequest.PlanCode,zohoSubscriptionRequest.Addons);
                if(response != null)
                {
                    return Ok(new MMSHttpReponse<double> { SuccessMessage = "success", ResponseBody = Convert.ToDouble(response) });
                }  
                else
                {
                    _logger.LogError($"Invalid value passed");
                    return StatusCode(StatusCodes.Status400BadRequest, new MMSHttpReponse { ErrorMessage = "Invalid value passed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("error in ComputeCost api - {e}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
            }
        }

        /// <summary>
        /// Get unused credit's of a user from zoho account.
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetUnusedCredits")]
        [ProducesResponseType(typeof(MMSHttpReponse<double>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnusedCredits()
        {
            try
            {
                var token = await _zohoAuthentication.GetZohoToken();
                var response = await _zohoSubscription.GetCustomerUnusedCredits(token,ZohoId);
                if (response != null)
                {
                    return Ok(new MMSHttpReponse<double> { SuccessMessage = "success", ResponseBody = Convert.ToDouble(response) });
                }
                else
                {
                    _logger.LogError($"error in ComputeCost zoho api, response return null value");
                    return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("error in GetUnusedCredits api - {e}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
            }
        }

        /// <summary>
        /// Create new subscription offline ie; with out collecting the payment. 
        /// </summary>
        /// <param name="zohoSubscriptionRequest"></param>
        /// <returns></returns>
        [HttpPost("CreateNewSubscriptionOffline")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateNewSubscriptionOffline([FromBody] CreateNewSubscriptionRequestExtension zohoSubscriptionRequest)
        {
            try
            {
                _logger.LogInformation($"secreate key {zohoSubscriptionRequest.SecretKey} vs secrate key {_configuration.GetSection("ActivatePromoPlanSecretKey").Value}");
                if (!zohoSubscriptionRequest.SecretKey.Equals(_configuration.GetSection("ActivatePromoPlanSecretKey").Value))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Not Authorized" });
                }

                if (zohoSubscriptionRequest == null || string.IsNullOrEmpty(zohoSubscriptionRequest.DeviceId) 
                    || string.IsNullOrEmpty(zohoSubscriptionRequest.PlanCode) 
                    || zohoSubscriptionRequest.Addons.Length <= 0)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Please select atleast one feature." });
                }

                if (!await IsDeviceAuthorizedAsAdmin(zohoSubscriptionRequest.DeviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return Forbid();
                }

                var redirectUrl = String.Empty;

                if (zohoSubscriptionRequest.Headless)
                {
                    redirectUrl = //this._configuration.GetSection("TevWebUri").Value +
                      this._configuration.GetSection("Zoho").GetSection("HostedPageRedirectUriHeadlessTrue").Value + true + "&deviceId=" + zohoSubscriptionRequest.DeviceId;
                }
                else
                {
                    redirectUrl = //this._configuration.GetSection("TevWebUri").Value +
                      this._configuration.GetSection("Zoho").GetSection("Zoho:HostedPageRedirectUriHeadlessFalse").Value + true + "&deviceId=" + zohoSubscriptionRequest.DeviceId;
                }

                var subscriptionHistory = _subscriptionRepo.Query(z => z.DeviceId == zohoSubscriptionRequest.DeviceId && z.OrgId == OrgId).OrderByDescending(z => z.CreatedDate).ToList();

                if (subscriptionHistory.Count > 0)
                {
                    return Ok(new MMSHttpReponse<string>()
                    {
                        ResponseBody = redirectUrl,
                        SuccessMessage = $"The selected device already has a existing subscription & the current state is - {subscriptionHistory[0].Status}, please click to go back to the mobile app."
                        
                    }); 
                }
                var token = await _zohoAuthentication.GetZohoToken();

                var promoPlanCodeList = _configuration.GetSection("Zoho").GetSection("PromoPlanCode").Value.Split(',');

                //if (!promoPlanCodeList.Contains(zohoSubscriptionRequest.PlanCode))
                //{
                //    return Forbid();
                //}

                var response = await _zohoSubscription.CreateNewSubscriptionOffline(token, ZohoId, zohoSubscriptionRequest.PlanCode, 
                    zohoSubscriptionRequest.DeviceId, zohoSubscriptionRequest.Addons, zohoSubscriptionRequest.Headless, OrgId);

                if (!response.message.Equals("error"))
                {
                    var zohoAddOns = new List<Tev.API.Models.ZohoAddon>();

                    var addOns = response.subscription.addons;
                    var ret = new List<int>();

                    addOns.ForEach(x => ret.Add(Helper.GetAlertType(x.addon_code)));

                    return Ok(new MMSHttpReponse<string>() { SuccessMessage = "You have successfully subscribed to selected features on the device", ResponseBody = redirectUrl });
                }
                else
                {
                    _logger.LogError("error in CreateNewSubscriptionOffline controller :- {e}", response.message);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("error in CreateNewSubscriptionOffline api - {e}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
            }
        }

        /// <summary>
        /// Activate Promotional subscription offline ie; with out collecting the payment. 
        /// </summary>
        /// <param name="promoSubscriptionRequest"></param>
        /// <returns></returns>
        [HttpPost("ActivatePromoSubscriptionForWsd")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        public async Task<IActionResult> ActivatePromoSubscriptionForWsd([FromBody] CreateNewSubscriptionRequestExtension promoSubscriptionRequest)
        {
            try
            {
                if (!promoSubscriptionRequest.SecretKey.Equals(_configuration.GetSection("ActivatePromoPlanSecretKey").Value))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Not Authorized" });
                }

                var token = await _zohoAuthentication.GetZohoToken();

                var response = await _zohoSubscription.CreateNewSubscriptionOffline(token, ZohoId, promoSubscriptionRequest.PlanCode,
                    promoSubscriptionRequest.DeviceId, promoSubscriptionRequest.Addons, promoSubscriptionRequest.Headless, OrgId);

                if (!response.message.Equals("error"))
                {
                    return Ok(new MMSHttpReponse<string>() { ResponseBody = response.subscription.subscription_id, SuccessMessage = "You have successfully subscribed to Promotional Plan on the device" });
                }
                else
                {
                    _logger.LogError("error in ActivatePromoSubscriptionForWsd controller :- {e}", response.message);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("error in ActivatePromoSubscriptionForWsd api - {e}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new MMSHttpReponse { ErrorMessage = "error" });
            }
          

        }

        /// <summary>
        /// Get all orphan subscription
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetOrphanSubscription")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<OrphanSubscriptionResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrphanSubscription()
        {
            try
            {
                //Get detached devices which are still not replaced yet!.
                var detachedDevices = _deviceDetachedRepo.Query(x => x.OrgId == OrgId && string.IsNullOrEmpty(x.NewDeviceId)).ToList();
                var response = new List<OrphanSubscriptionResponse>();

                var ListofLogicalDeviceId = detachedDevices.Select(x => x.LogicalDetachedDeviceId).ToList();

                var data = _subscriptionRepo.Query(y => y.OrgId == OrgId && ListofLogicalDeviceId.Contains(y.DeviceId))
                 .OrderByDescending(x => x.CreatedDate).Include(x => x.Features).ToList();

                var filterData = new List<ZohoSubscriptionHistory>();

                ListofLogicalDeviceId.ForEach(x=> {
                    var latestSubscriptionState = data.Where(y => y.DeviceId == x).OrderByDescending(z=> z.CreatedDate).FirstOrDefault();
                    if(latestSubscriptionState != null)
                        filterData.Add(latestSubscriptionState);
                });

                filterData.ForEach(x=> {

                    var availablefeatures = new List<AvailableFeatureResponse>();

                    foreach (var item in x.Features)
                    {
                      
                        availablefeatures.Add(new AvailableFeatureResponse
                        {
                            Code = item.Code,
                            Name = item.Name,
                            Price = item.Price
                        });
                    }

                    var rspData = new OrphanSubscriptionResponse
                    {
                        OrphanSubscriptionId = x.SubscriptionId,
                        PlanName = x.PlanName,
                        ProductName = x.ProductName,
                        Status = x.Status,
                        Amount = x.Amount,
                        NextBillingAt = x.NextBillingAt,
                        features = availablefeatures,
                        PauseDate = x.CreatedDate
                    };
                    response.Add(rspData);

                });


                return Ok(new MMSHttpReponse<List<OrphanSubscriptionResponse>> { ResponseBody = response.OrderByDescending(x => x.PauseDate).ToList().GroupBy(i=>i.OrphanSubscriptionId).Select(g=>g.First()).ToList(), SuccessMessage = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in SubscriptionHistory api :- {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Attach orphan subscription to a device  
        /// </summary>
        /// <param name="orphanSubscriptionId"> Orphan subscription id</param>
        /// <param name="deviceId">Logical device id</param>
        /// <returns>MMSHttpReponse</returns>
        [HttpGet("AttachSubscription/{orphanSubscriptionId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AttachSubscription(string orphanSubscriptionId, [Required] string deviceId)
        {
            try
            {
                if (!await IsDeviceAuthorizedAsAdmin(deviceId, _deviceRepo, _userDevicePermissionRepo))
                {
                    return Forbid();
                }

                var device = await _deviceRepo.GetDevice(deviceId, OrgId);

                //Get selected subscription data. 
                var data = _subscriptionRepo.Query(x => x.SubscriptionId == orphanSubscriptionId && x.OrgId == OrgId).OrderByDescending(x => x.CreatedDate)
                    .Include(x => x.Features).FirstOrDefault();

                if(data == null)
                {
                    return BadRequest();
                }

                var ret = new List<int>();

                foreach (var item in data.Features)
                {
                    ret.Add(Helper.GetAlertType(item.Code));
                }

                var isActive = true;

                if (data.Status == "cancelled" || data.Status == "expired")
                {
                    isActive = false;
                }

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var deviceDetachedHistory = _deviceDetachedRepo.Query(z => z.LogicalDetachedDeviceId == data.DeviceId)
                            .OrderByDescending(x => x.CreatedDate).FirstOrDefault();

                        deviceDetachedHistory.NewDeviceId = device.LogicalDeviceId;
                        deviceDetachedHistory.ModifiedBy = UserEmail;


                        _deviceDetachedRepo.Update(deviceDetachedHistory);
                        _deviceDetachedRepo.SaveChanges();

                        var token = await _zohoAuthentication.GetZohoToken();

                        RetrieveSubscriptionDetailsResponse subcriptionDetails = null;
                        PostponeRenewal postponeRenewal = null;
                        var postData = new ZohoSubscriptionHistory();

                        if (data.Status == "paused")
                        {
                            subcriptionDetails = await _zohoSubscription.ResumeSubscription(token, data.SubscriptionId, OtherConstants.ResumeSubscription);

                            if (subcriptionDetails.message == "26009")
                            {
                                subcriptionDetails = await _zohoSubscription.GetSubscription(token, data.SubscriptionId);
                            }
                            else
                            {
                                var dateTimeNow = Helper.GetISTNow().ToString("yyyy-MM-dd");

                                if (dateTimeNow != data.CreatedTime.ToString("yyyy-MM-dd"))
                                {
                                    var day = 0;
                                    if (Convert.ToDateTime(subcriptionDetails.subscription.current_term_ends_at) > Helper.GetISTNow() ||
                                        Convert.ToDateTime(subcriptionDetails.subscription.current_term_ends_at).ToString("yyyy-MM-dd") == dateTimeNow)
                                    {
                                        day = (int)(Helper.GetISTNow() - Helper.FromUnixTime(deviceDetachedHistory.CreatedDate)).TotalDays;
                                    }
                                    else
                                    {
                                        day = (int)(Convert.ToDateTime(data.NextBillingAt) - Helper.FromUnixTime(deviceDetachedHistory.CreatedDate)).TotalDays;
                                    }

                                    if (day != 0)
                                        postponeRenewal = await _zohoSubscription.PostponeRenewal(token, data.SubscriptionId, Helper.AddDayToDateTime(Convert.ToDateTime(subcriptionDetails.subscription.current_term_ends_at), day).ToString("yyyy-MM-dd"));
                                }
                            }
                           
                        }
                        else
                        {
                            subcriptionDetails = await _zohoSubscription.GetSubscription(token, data.SubscriptionId);

                        }
                        if (!subcriptionDetails.message.Equals("error"))
                        {
                            var promoPlanCodeList = _configuration.GetSection("Zoho").GetSection("PromoPlanCode").Value.Split(',');

                            var nextBillingAt = "";
                            if (!promoPlanCodeList.Contains(subcriptionDetails.subscription.plan.plan_code))
                            {
                                nextBillingAt = (string.IsNullOrEmpty(postponeRenewal?.subscription?.next_billing_at) || postponeRenewal?.subscription?.next_billing_at == "") ? subcriptionDetails.subscription.current_term_ends_at : postponeRenewal.subscription.next_billing_at;
                            }
                            else
                            {
                                nextBillingAt = (string.IsNullOrEmpty(postponeRenewal?.subscription?.expires_at) || postponeRenewal?.subscription?.expires_at == "") ? subcriptionDetails.subscription.expires_at : postponeRenewal.subscription.expires_at;
                            }

                            postData = new ZohoSubscriptionHistory()
                            {
                                SubscriptionNumber = subcriptionDetails.subscription.subscription_number,
                                OrgId = OrgId,
                                SubscriptionId = subcriptionDetails.subscription.subscription_id,
                                DeviceId = deviceId,
                                DeviceName = device.DeviceName,
                                PlanCode = subcriptionDetails.subscription.plan.plan_code,
                                CreatedTime = Convert.ToDateTime(subcriptionDetails.subscription.created_time),
                                ProductName = subcriptionDetails.subscription.product_name,
                                PlanName = subcriptionDetails.subscription.plan.name,
                                Status = subcriptionDetails.subscription.status,
                                EventType = nameof(SubscriptionEventType.subscription_resumed),
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
                                NextBillingAt = nextBillingAt
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

                        //push subscription data to device
                        var isUpdated = await _tevIotRegistry.AttachSubscriptionToDevice(postData.NextBillingAt, isActive,
                                     deviceId, data.SubscriptionId, ret.ToArray(), postData.Status, SubscriptionEventType.subscription_switched);
                        //Update Cosmos Device Data

                        if (device != null)
                        {
                            device.Subscription = new Tev.Cosmos.Entity.Subscription
                            {
                                SubscriptionId = data.SubscriptionId,
                                PlanName = data.PlanName,
                                Amount = Convert.ToString(data.Amount),
                                SubscriptionExpiryDate = Convert.ToDateTime(postData.NextBillingAt).ToString("yyyy-MM-dd"),
                                SubscriptionStatus = postData.Status,
                                AvailableFeatures = ret.ToArray(),
                            };

                            device.TwinChangeStatus = TwinChangeStatus.DeviceSwitched;
                            await _deviceRepo.UpdateDevice(OrgId, device);
                        }


                        if (isUpdated != true)
                        {
                            _logger.LogError("Error in ZohoSubscriptionController AttachSubscription Method isUpdated flag is - {0}", isUpdated);
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                        transaction.Commit();
                        await _zohoSubscription.UpdateCustomFieldForSubscritpion(token, data.SubscriptionId, deviceId);
                        return Ok(new MMSHttpReponse { SuccessMessage = "selected subscription added to the device successfully." });

                    }
                    catch (Exception e)
                    {
                        _logger.LogError("error occured on attaching subscription {exception}", e);
                        transaction.Rollback();
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }

               
            }
            catch(Exception ex)
            {
                _logger.LogError("Error in AttachSubscription {0}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("UpdateCustomValue")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCustomValueDeviceIdInZoho(string deviceId, string subscriptionId)
        {
            try
            {
                var token = await _zohoAuthentication.GetZohoToken();
                await _zohoSubscription.UpdateCustomFieldForSubscritpion(token, subscriptionId, deviceId);
                return Ok(new MMSHttpReponse { SuccessMessage = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UpdateCustomValueDeviceIdInZoho {0}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }


        #region private methods
        private InvoiceHistoryResponse MapInvoiceHistoryDTO(InvoiceHistory invoiceHistory)
        {

            var invoiceItems = new List<InvoiceHistoryItemResponse>();
            foreach (var item in invoiceHistory.InvoiceItems)
            {
                invoiceItems.Add(new InvoiceHistoryItemResponse
                {
                    ItemCode = item.ItemCode,
                    Quantity = item.Quantity,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    InvoiceHistoryId = item.InvoiceHistoryFK
                });
            }
            var payments = new List<InvoiceHistoryPaymentResponse>();
            foreach (var pay in invoiceHistory.Payments)
            {
                payments.Add(new InvoiceHistoryPaymentResponse
                {
                    PaymentId = pay.PaymentId,
                    Amount = pay.Amount,
                    AmountRefunded = pay.AmountRefunded,
                    BankCharges = pay.BankCharges,
                    Description = pay.Description, 
                    InvoiceHistoryId = pay.InvoiceHistoryFK
                });
            };

            var subcsriptions = new List<InvoiceHistorySubscriptionResponse>();
            foreach (var sub in invoiceHistory.InvoiceHistorySubscriptionAssociations)
            {
                subcsriptions.Add(new InvoiceHistorySubscriptionResponse { 
                    SubscriptionId = sub.SubscriptionId,
                    ActivatedTime = invoiceHistory.CreatedTime,
                    InvoiceHistoryId = sub.InvoiceHistoryFK
                });
            }

            return new InvoiceHistoryResponse
            {
                Id = invoiceHistory.Id,
                EventType = invoiceHistory.EventType,
                InvoiceNumber = invoiceHistory.InvoiceNumber,
                Balance = invoiceHistory.Balance,
                CurrencyCode = invoiceHistory.CurrencyCode,
                InvoiceDate = invoiceHistory.InvoiceDate,
                Email = invoiceHistory.EventType,
                CustomerName = invoiceHistory.CustomerName,
                InvoiceId = invoiceHistory.InvoiceId,
                Total = invoiceHistory.Total,
                InvoiceItems = invoiceItems,
                Payments = payments,
                Subscriptions = subcsriptions
            };
        } 
        private async Task<List<ZohoProduct>> GetZohoProducts(string token)
        {
            List<ZohoProduct> products ;
            if (!_memoryCache.TryGetValue("ZohoProducts", out products))
            {
                products = await _zohoSubscription.GetProducts(token);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromHours(24));

                _memoryCache.Set("ZohoProducts", products, cacheEntryOptions);
                return products;
            }
            else
            {
                var result = _memoryCache.Get("ZohoProducts");
                return (List<ZohoProduct>)result;
            }
        }
        private bool IsSubscriptionAuthorized(string deviceId, string subId) => _subscriptionRepo.Query(x => 
                        x.OrgId == OrgId && x.DeviceId == deviceId && x.SubscriptionId == subId).FirstOrDefault() != null ? true : false;
        private bool IsSubscriptionAuthorized(string subId) => _subscriptionRepo.Query(x => x.OrgId == OrgId && x.SubscriptionId == subId).FirstOrDefault() != null ? true : false;
        private bool UpdateSubscriptionHistory(UpdateSubscriptionResponse subcriptionDetails,string deviceId,string deviceName)
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var postData = new ZohoSubscriptionHistory()
                    {
                        SubscriptionNumber = subcriptionDetails.subscription.subscription_number,
                        OrgId = OrgId,
                        SubscriptionId = subcriptionDetails.subscription.subscription_id,
                        DeviceId = deviceId,
                        DeviceName = deviceName,
                        PlanCode = subcriptionDetails.subscription.plan.plan_code,
                        CreatedTime = Convert.ToDateTime(subcriptionDetails.subscription.created_time),
                        ProductName = subcriptionDetails.subscription.product_name,
                        PlanName = subcriptionDetails.subscription.plan.name,
                        Status = subcriptionDetails.subscription.status,
                        EventType = nameof(SubscriptionEventType.subscription_modified),
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
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError("error in UpdateToSubscriptionHistoryTbl - {0}", ex);
                    return false;
                }
            }
        }
        private bool UpdateDeviceSwitchDataToHistory(ZohoSubscriptionHistory zohoSubscriptionHistory, TevDevice tevDevice)
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var deviceDetachedHistory = _deviceDetachedRepo.Query(z => z.LogicalDetachedDeviceId == tevDevice.DeviceId).FirstOrDefault();

                    deviceDetachedHistory.NewDeviceId = tevDevice.DeviceId;
                    deviceDetachedHistory.ModifiedBy = UserEmail;


                    _deviceDetachedRepo.Update(deviceDetachedHistory);
                    _deviceDetachedRepo.SaveChanges();

                    var postData = new ZohoSubscriptionHistory()
                    {
                        SubscriptionNumber = zohoSubscriptionHistory.SubscriptionNumber,
                        OrgId = OrgId,
                        SubscriptionId = zohoSubscriptionHistory.SubscriptionId,
                        DeviceId = tevDevice.DeviceId,
                        DeviceName = tevDevice.DeviceName,
                        PlanCode = zohoSubscriptionHistory.PlanCode,
                        CreatedTime = Convert.ToDateTime(zohoSubscriptionHistory.CreatedTime),
                        ProductName = zohoSubscriptionHistory.ProductName,
                        PlanName = zohoSubscriptionHistory.PlanName,
                        Status = zohoSubscriptionHistory.Status,
                        EventType = nameof(SubscriptionEventType.subscription_switched),
                        PlanPrice = zohoSubscriptionHistory.PlanPrice,
                        CreatedDate = DateTime.UtcNow.Ticks,
                        ModifiedDate = DateTime.UtcNow.Ticks,
                        CGSTName = "CGST",
                        CGSTAmount = zohoSubscriptionHistory.CGSTAmount,
                        SGSTName = "SGST",
                        SGSTAmount = zohoSubscriptionHistory.SGSTAmount,
                        TaxPercentage = zohoSubscriptionHistory.TaxPercentage,
                        Amount = zohoSubscriptionHistory.Amount,
                        SubTotal = zohoSubscriptionHistory.SubTotal,
                        Email = zohoSubscriptionHistory.Email,
                        //todo email here is always the email of the 1st org admin and not the one who actually added the subscription
                        CompanyName = zohoSubscriptionHistory.CompanyName,
                        CreatedBy = UserEmail,
                        ModifiedBy = UserEmail,
                        InvoiceId = null, //  subscription switch - invoice wont generate 
                        Interval = zohoSubscriptionHistory.Interval,
                        IntervalUnit = zohoSubscriptionHistory.IntervalUnit,
                        Currency = zohoSubscriptionHistory.Currency,
                        NextBillingAt = zohoSubscriptionHistory.NextBillingAt

                    };

                    _subscriptionRepo.Add(postData);
                    _subscriptionRepo.SaveChanges();

                    var sub = _subscriptionRepo.Query(z => z.Id == postData.Id).FirstOrDefault();

                    foreach (var item in zohoSubscriptionHistory.Features)
                    {
                        var subFeature = new FeatureSubscriptionAssociation()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Code = item.Code,
                            Name = item.Name,
                            Price = item.Price,
                            CreatedBy = UserEmail,
                            ModifiedBy = UserEmail,
                            CreatedDate = DateTime.UtcNow.Ticks,
                            ModifiedDate = DateTime.UtcNow.Ticks,
                            ZohoSubscriptionHistory = sub
                            //ZohoSubscriptionHistoryFK = sub.Id
                        };
                        _featureSubscriptionRepo.Add(subFeature);
                        _featureSubscriptionRepo.SaveChanges();
                    }

                    transaction.Commit();
                    return true;
                }
                catch(Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError("error in UpdateToSubscriptionHistoryTbl - {0}", ex);
                    return false;
                }
            }
        }
        private (PlanStatus, string) GetSubscriptionStatus(string deviceId)
        {
            var result = _subscriptionRepo.Query(x => x.DeviceId == deviceId).OrderByDescending(x => x.CreatedDate).FirstOrDefault();

            if (result == null)
            {
                return (PlanStatus.New, null);
            }
            else
            {
                switch (result.Status)
                {
                    case nameof(SubscriptionStatus.live):
                        return (PlanStatus.Update, result.PlanCode);

                    case nameof(SubscriptionStatus.cancelled):
                        return (PlanStatus.Renew, result.PlanCode);

                    case nameof(SubscriptionStatus.expired):
                        return (PlanStatus.Renew, result.PlanCode);
                    case nameof(SubscriptionStatus.non_renewing):
                        return (PlanStatus.NonRenewing, null);
                    default:
                        return (PlanStatus.New, result.PlanCode);
                }
            }
        }

        private async Task<string> GetCoupenToApply(string token, string planCode, string[] addons,string product)
        {
            try
            {
                var coupons = new List<Coupon>();
                var coupon = new Coupon();
                var planResult = (List<ZohoSubscription.Models.Plan>)_memoryCache.Get("Plans");
                var plan = planResult?.Where(x => x.plan_code == planCode).FirstOrDefault();
                _logger.LogInformation($"GetCoupenToApply Plan :- {plan}");
                // To be removed after testing 
                var plans = new List<ZohoSubscription.Models.Plan>();
                if (planResult == null)
                {
                    plans = await _zohoSubscription.GetPlans(token, product);
                    plan = plans?.Where(x => x.plan_code == planCode).FirstOrDefault();
                }
                var amt = 0.0;

                if (!_memoryCache.TryGetValue("Coupons", out coupons))
                {
                    coupons = await _zohoSubscription.GetCoupons(token);

                    if (coupons != null)
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                               .SetAbsoluteExpiration(TimeSpan.FromHours(2));

                        _memoryCache.Set("Coupons", coupons, cacheEntryOptions);

                        var data = coupons.Where(c => c.product_id == plan.product_id).ToList();

                        for (int i = 0; i < addons.Length; i++)
                        {
                            var _addon = plan.addons.Where(x => x.addon_code == addons[i].ToString()).FirstOrDefault();
                            if (_addon.price > amt)
                            {
                                amt = _addon.price;
                                coupon = data.Where(x => x.coupon_code == _addon.addon_code).FirstOrDefault();
                            }
                        }
                    }
                }
                else
                {
                    coupons = (List<Coupon>)_memoryCache.Get("Coupons");
                    _logger.LogInformation($"coupans from cache :- {coupons}");
                    var data = coupons.Where(c => c.product_id == plan.product_id).ToList();

                    for (int i = 0; i < addons.Length; i++)
                    {
                        var zohoaddon = plan.addons.Where(x => x.addon_code == addons[i].ToString()).FirstOrDefault();
                        if (zohoaddon.price > amt)
                        {
                            amt = zohoaddon.price;
                            coupon = data.Where(x => x.coupon_code == zohoaddon.addon_code).FirstOrDefault();
                        }
                    }

                }

                return coupon?.coupon_code;
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error occured while fetching the GetCoupenToApply :- {ex}");
                return null;
            }
        }
        #endregion

    }
}