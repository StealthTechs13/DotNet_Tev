using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Models;
using Tev.Cosmos.Entity;
using Tev.Cosmos.IRepository;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;
using Tev.IotHub.Models;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ZohoWebhookController : TevControllerBase
    {
        private readonly IGenericRepo<ZohoSubscriptionHistory> _subscriptionRepo;
        private readonly IGenericRepo<FeatureSubscriptionAssociation> _featureSubscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ZohoWebhookController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITevIoTRegistry _tevIotRegistry;
        private readonly IGenericRepo<PaymentHistory> _payementRepo;
        private readonly IGenericRepo<PayementInvoiceAssociation> _invoiceRepo;
        private readonly IGenericRepo<InvoiceSubscriptionAssociation> _invoiceSubscriptionRepo;
        private readonly IGenericRepo<InvoiceHistory> _invoiceHistoryRepo;
        private readonly IGenericRepo<InvoiceHistoryPayment> _paymentInvoiceHistoryRepo;
        private readonly IGenericRepo<InvoiceHistorySubscription> _invoiceHistorySubscriptionRepo;
        private readonly IGenericRepo<InvoiceHistoryItem> _invoiceItemRepo;
        private readonly IDeviceRepo _deviceRepo;

        public ZohoWebhookController(IGenericRepo<ZohoSubscriptionHistory> subscriptionRepo,
             IGenericRepo<FeatureSubscriptionAssociation> featureSubscriptionRepo, IUnitOfWork unitOfWork,
             ILogger<ZohoWebhookController> logger, IConfiguration configuration,ITevIoTRegistry tevIotRegistry, 
             IGenericRepo<PaymentHistory> payementRepo, IGenericRepo<PayementInvoiceAssociation> invoiceRepo,
             IGenericRepo<InvoiceSubscriptionAssociation> invoiceSubscriptionRepo, IGenericRepo<InvoiceHistory> invoiceHistoryRepo,
             IGenericRepo<InvoiceHistoryPayment> paymentInvoiceHistoryRepo, IGenericRepo<InvoiceHistorySubscription> invoiceHistorySubscriptionRepo,
             IGenericRepo<InvoiceHistoryItem> invoiceItemRepo, IDeviceRepo deviceRepo)
        {
            _subscriptionRepo = subscriptionRepo;
            _featureSubscriptionRepo = featureSubscriptionRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _tevIotRegistry = tevIotRegistry;
            _payementRepo = payementRepo;
            _invoiceRepo = invoiceRepo;
            _invoiceSubscriptionRepo = invoiceSubscriptionRepo;
            _invoiceHistoryRepo = invoiceHistoryRepo;
            _paymentInvoiceHistoryRepo = paymentInvoiceHistoryRepo;
            _invoiceHistorySubscriptionRepo = invoiceHistorySubscriptionRepo;
            _invoiceItemRepo = invoiceItemRepo;
            _deviceRepo = deviceRepo;
        }

        #region ZohoWebhooks

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("SubscriptionHistory/{key}")]
        [AllowAnonymous]
        public async Task SubscriptionHistory(string key, [FromBody] ZohoSubscriptionWebHookRequestModel subscriptionWebHookRequestModel)
        {
            if (!string.IsNullOrEmpty(key) && key.Equals(this._configuration.GetValue<string>("Zoho:WebhookAuthenticationKey")) && subscriptionWebHookRequestModel != null)
            {
                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        TevDevice device;

                        var deviceInfo = subscriptionWebHookRequestModel.data.subscription.custom_fields.Find(x => x.label == "DeviceId");

                        var orgInfo = subscriptionWebHookRequestModel.data.subscription.custom_fields.Find(x => x.label == "OrgId");

                        var tevDeviceext = await _deviceRepo.GetDevice(deviceInfo.value, orgInfo.value);

                        device = new TevDevice
                        {
                            OrgId = tevDeviceext?.OrgId,
                            DeviceId = tevDeviceext?.Id,
                            DeviceName = tevDeviceext?.DeviceName
                        };

                        var subDetials = _subscriptionRepo.Query(x => x.DeviceId == deviceInfo.value).OrderByDescending(x => x.CreatedDate).FirstOrDefault();

                        var postData = new ZohoSubscriptionHistory()
                        {
                            SubscriptionNumber = subscriptionWebHookRequestModel.data.subscription.subscription_number,
                            OrgId = orgInfo?.value,
                            SubscriptionId = subscriptionWebHookRequestModel.data.subscription.subscription_id,
                            DeviceId =  deviceInfo.value,
                            DeviceName = (device == null || device.DeviceName == null) ? subDetials.DeviceName : device.DeviceName,
                            PlanCode = subscriptionWebHookRequestModel.data.subscription.plan.plan_code,
                            CreatedTime = Convert.ToDateTime(subscriptionWebHookRequestModel.data.subscription.created_time),
                            ProductName = subscriptionWebHookRequestModel.data.subscription.product_name,
                            PlanName = subscriptionWebHookRequestModel.data.subscription.plan.name,
                            Status = subscriptionWebHookRequestModel.data.subscription.status,
                            EventType = subscriptionWebHookRequestModel.event_type,
                            PlanPrice = subscriptionWebHookRequestModel.data.subscription.plan.price,
                            CreatedDate = DateTime.UtcNow.Ticks,
                            ModifiedDate = DateTime.UtcNow.Ticks,
                            CGSTName = "CGST",
                            CGSTAmount = subscriptionWebHookRequestModel.data.subscription.taxes[0].tax_amount,
                            SGSTName = "SGST",
                            SGSTAmount = subscriptionWebHookRequestModel.data.subscription.taxes[0].tax_amount,
                            TaxPercentage = subscriptionWebHookRequestModel.data.subscription.plan.tax_percentage,
                            Amount = subscriptionWebHookRequestModel.data.subscription.amount,
                            SubTotal = subscriptionWebHookRequestModel.data.subscription.sub_total,
                            Email = subscriptionWebHookRequestModel.data.subscription.customer.email,
                            //todo email here is always the email of the 1st org admin and not the one who actually added the subscription
                            CompanyName = subscriptionWebHookRequestModel.data.subscription.customer.company_name,
                            CreatedBy = subscriptionWebHookRequestModel.data.subscription.customer.email,
                            ModifiedBy = subscriptionWebHookRequestModel.data.subscription.customer.email,
                            InvoiceId = subscriptionWebHookRequestModel.data.subscription.child_invoice_id,
                            Interval = subscriptionWebHookRequestModel.data.subscription.interval,
                            IntervalUnit = subscriptionWebHookRequestModel.data.subscription.interval_unit,
                            Currency = subscriptionWebHookRequestModel.data.subscription.currency_code,
                            NextBillingAt = subscriptionWebHookRequestModel.data.subscription.current_term_ends_at,
                            PhysicalDeviceId = tevDeviceext.Id

                        };

                        if (subscriptionWebHookRequestModel.event_type == nameof(SubscriptionEventType.subscription_activation))
                        {
                            postData.EventType = nameof(SubscriptionEventType.subscription_created);
                        }

                        _subscriptionRepo.Add(postData);
                        _subscriptionRepo.SaveChanges();

                        var sub = _subscriptionRepo.Query(z => z.Id == postData.Id).FirstOrDefault();

                        foreach (var item in subscriptionWebHookRequestModel.data.subscription.addons)
                        {
                            var subFeature = new FeatureSubscriptionAssociation()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Code = item.addon_code,
                                Name = item.name,
                                Price = item.price,
                                CreatedBy = subscriptionWebHookRequestModel.data.subscription.customer.email,
                                ModifiedBy = subscriptionWebHookRequestModel.data.subscription.customer.email,
                                CreatedDate = DateTime.UtcNow.Ticks,
                                ModifiedDate = DateTime.UtcNow.Ticks,
                                ZohoSubscriptionHistory = sub
                                //ZohoSubscriptionHistoryFK = sub.Id
                            };
                            _featureSubscriptionRepo.Add(subFeature);
                            _featureSubscriptionRepo.SaveChanges();
                        }


                        TwinChangeStatus twinChange = TwinChangeStatus.Default;

                        if (postData.Status == nameof(SubscriptionStatus.live))
                        {
                            var ret = new List<int>();

                            subscriptionWebHookRequestModel.data.subscription.addons.ForEach(x => ret.Add(Helper.GetAlertType(x.addon_code)));

                            switch (subscriptionWebHookRequestModel.event_type)
                            {
                                case nameof(SubscriptionEventType.subscription_renewed):
                                    await _tevIotRegistry.UpdateTwin(subscriptionWebHookRequestModel.data.subscription.current_term_ends_at, true,
                                postData.DeviceId, postData.SubscriptionId, ret.ToArray(), postData.Status, SubscriptionEventType.subscription_renewed);
                                    twinChange = TwinChangeStatus.SubscriptionRenewed;
                                    break;
                                case nameof(SubscriptionEventType.subscription_activation):
                                    await _tevIotRegistry.UpdateTwin(subscriptionWebHookRequestModel.data.subscription.current_term_ends_at, true,
                                postData.DeviceId, postData.SubscriptionId, ret.ToArray(), postData.Status, SubscriptionEventType.subscription_activation);
                                    twinChange = TwinChangeStatus.NewSubscription;
                                    break;
                                case nameof(SubscriptionEventType.subscription_upgraded):
                                    await _tevIotRegistry.UpdateTwin(subscriptionWebHookRequestModel.data.subscription.current_term_ends_at, true,
                                postData.DeviceId, postData.SubscriptionId, ret.ToArray(), postData.Status, SubscriptionEventType.subscription_upgraded);
                                    twinChange = TwinChangeStatus.UpgradeSubscription;
                                    break;
                                case nameof(SubscriptionEventType.subscription_downgraded):
                                    break;
                                case nameof(SubscriptionEventType.subscription_scheduled_cancellation_removed):
                                    await _tevIotRegistry.UpdateDeviceSubscriptionStatus(device.DeviceId, postData.Status);
                                    twinChange = TwinChangeStatus.SubscriptionReactivated;
                                    break;
                            }

                            //Update Cosmos Device Data
                           
                            if(subscriptionWebHookRequestModel.event_type == nameof(SubscriptionEventType.subscription_activation))
                            {
                                if(tevDeviceext != null)
                                {
                                    tevDeviceext.Subscription = new Tev.Cosmos.Entity.Subscription
                                    {
                                        SubscriptionId = postData.SubscriptionId,
                                        PlanName = postData.PlanName,
                                        Amount = Convert.ToString(postData.Amount),
                                        SubscriptionExpiryDate = Convert.ToDateTime(subscriptionWebHookRequestModel.data.subscription.current_term_ends_at).ToString("yyyy-MM-dd"),
                                        SubscriptionStatus = postData.Status,
                                        AvailableFeatures = ret.ToArray(),
                                    };
                                    tevDeviceext.TwinChangeStatus = twinChange;
                                    await _deviceRepo.UpdateDevice(postData.OrgId, tevDeviceext);
                                }
                            }
                            else
                            {
                                if(tevDeviceext != null)
                                {
                                    tevDeviceext.Subscription.SubscriptionStatus = postData.Status;
                                    tevDeviceext.Subscription.PlanName = postData.PlanName;
                                    tevDeviceext.Subscription.Amount = Convert.ToString(postData.Amount);
                                    tevDeviceext.Subscription.SubscriptionExpiryDate = subscriptionWebHookRequestModel.data.subscription.current_term_ends_at;
                                    tevDeviceext.Subscription.AvailableFeatures = ret.ToArray();

                                    tevDeviceext.TwinChangeStatus = twinChange;
                                    await _deviceRepo.UpdateDevice(postData.OrgId, tevDeviceext);
                                }
                            }
                           
                        }

                        if (postData.Status == nameof(SubscriptionStatus.expired))
                        {
                            await _tevIotRegistry.UpdateDeviceSubscriptionStatus(device.DeviceId, postData.Status);
                            //Update Cosmos Device Data
                            if (tevDeviceext != null)
                            {
                                tevDeviceext.Subscription.SubscriptionStatus = postData.Status;
                                tevDeviceext.TwinChangeStatus = twinChange;
                                await _deviceRepo.UpdateDevice(postData.OrgId, tevDeviceext);
                            }
                        }

                        //subscription_cancellation_scheduled
                        if (postData.Status == nameof(SubscriptionStatus.non_renewing))
                        {
                            await _tevIotRegistry.UpdateDeviceSubscriptionStatus(device.DeviceId, postData.Status);
                            if (tevDeviceext != null)
                            {
                                //Update Cosmos Device Data
                                tevDeviceext.Subscription.SubscriptionStatus = postData.Status;
                                if(tevDeviceext != null)
                                {
                                    tevDeviceext.TwinChangeStatus = twinChange;
                                    await _deviceRepo.UpdateDevice(postData.OrgId, tevDeviceext);
                                }
                              
                            }
                        }
                        
                        //subscription_cancelled    
                        if (postData.Status == nameof(SubscriptionStatus.cancelled))
                        {
                            var deviceTwinData = await _tevIotRegistry.UpdateDeviceSubscriptionStatus(device.DeviceId, postData.Status);
                            //Update Cosmos Device Data
                            if (tevDeviceext != null)
                            {
                                tevDeviceext.Subscription.SubscriptionStatus = postData.Status;
                                tevDeviceext.TwinChangeStatus = twinChange;
                                await _deviceRepo.UpdateDevice(postData.OrgId, tevDeviceext);
                            }
                            
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {

                        transaction.Rollback();
                        _logger.LogError("Following are the exception occured on ZohoSubscriptionController action method SubscriptionHistory :- {e}", ex);
                    }
                }
            }
            else
            {
                _logger.LogError("Failed to Update Subscription History. The key didnt matched or empty");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("PaymentHistory/{key}")]
        [AllowAnonymous]
        public void PaymentHistory(string key, [FromBody] ZohoPurchaseWebhookRequestModel zohoPurchaseWebhookRequestModel)
        {
            if (!string.IsNullOrEmpty(key) && key.Equals(this._configuration.GetValue<string>("Zoho:WebhookAuthenticationKey")) && zohoPurchaseWebhookRequestModel != null)
            {
                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var paymentHistory = new PaymentHistory
                        {
                            PaymentId = zohoPurchaseWebhookRequestModel.data.payment.payment_id,
                            PaymentNumber = zohoPurchaseWebhookRequestModel.data.payment.payment_number,
                            PayedAmount = zohoPurchaseWebhookRequestModel.data.payment.amount,
                            PaymentDate = Convert.ToDateTime(zohoPurchaseWebhookRequestModel.data.payment.date),
                            CreatedBy = zohoPurchaseWebhookRequestModel.data.payment.email,
                            CreatedDate = DateTime.UtcNow.Ticks,
                            ModifiedDate = DateTime.UtcNow.Ticks,
                            Description = zohoPurchaseWebhookRequestModel.data.payment.description,
                            CurrencyCode = zohoPurchaseWebhookRequestModel.data.payment.currency_code,
                            CustomerId = zohoPurchaseWebhookRequestModel.data.payment.customer_id,
                            Email = zohoPurchaseWebhookRequestModel.data.payment.email,
                            PaymentCreatedTime = Convert.ToDateTime(zohoPurchaseWebhookRequestModel.created_time),
                            EventType = zohoPurchaseWebhookRequestModel.event_type,
                            ModifiedBy = zohoPurchaseWebhookRequestModel.data.payment.email,
                            PaymentStatus = zohoPurchaseWebhookRequestModel.data.payment.status
                        };

                        _payementRepo.Add(paymentHistory);
                        _payementRepo.SaveChanges();

                        var paymentDetails = _payementRepo.Query(z => z.Id == paymentHistory.Id).FirstOrDefault();

                        foreach (var item in zohoPurchaseWebhookRequestModel.data.payment.invoices)
                        {
                            var invoice = new PayementInvoiceAssociation
                            {
                                InvoiceId = item.invoice_id,
                                InvoiceNumber = item.invoice_number,
                                InvoiceAmount = item.invoice_amount,
                                InvoiceDate = Convert.ToDateTime(item.date),
                                AmountApplied = item.amount_applied,
                                BalanceAmount = item.balance_amount,
                                TransactionType = item.transaction_type,
                                PaymentHistory = paymentDetails,
                                CreatedBy = zohoPurchaseWebhookRequestModel.data.payment.email,
                                CreatedDate = DateTime.UtcNow.Ticks,
                                ModifiedDate = DateTime.UtcNow.Ticks,
                                ModifiedBy = zohoPurchaseWebhookRequestModel.data.payment.email
                            };
                            _invoiceRepo.Add(invoice);
                            _invoiceRepo.SaveChanges();

                            var invoiceDetails = _invoiceRepo.Query(z => z.Id == paymentHistory.Id).FirstOrDefault();

                            for (int i = 0; i < item.subscription_ids.Length; i++)
                            {
                                var subscription = new InvoiceSubscriptionAssociation
                                {
                                    SubscriptionId = item.subscription_ids[i],
                                    InvoiceId = invoiceDetails.InvoiceId,
                                    PayementInvoiceAssociation = invoiceDetails,
                                    CreatedBy = zohoPurchaseWebhookRequestModel.data.payment.email,
                                    CreatedDate = DateTime.UtcNow.Ticks,
                                    ModifiedDate = DateTime.UtcNow.Ticks,
                                    ModifiedBy = zohoPurchaseWebhookRequestModel.data.payment.email
                                };

                                _invoiceSubscriptionRepo.Add(subscription);
                                _invoiceSubscriptionRepo.SaveChanges();
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError("Payment history webhook failed & following are the exception : - {e}", ex);
                    }
                }
            }
            else
            {
                _logger.LogError("Payment history webhook failed. wrong authentication key is passed");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("InvoiceHistory/{key}")]
        [AllowAnonymous]
        public async Task InvoiceHistory(string key, [FromBody] ZohoInvoiceWebhookRequestModel zohoInvoiceWebhookRequestModel)
        {
            if (!string.IsNullOrEmpty(key) && key.Equals(this._configuration.GetValue<string>("Zoho:WebhookAuthenticationKey")) && zohoInvoiceWebhookRequestModel != null)
            {
                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        Device device;
                        var deviceDetail = zohoInvoiceWebhookRequestModel.data.invoice.custom_fields.Find(x => x.label == "DeviceId");
                        var orgDetail = zohoInvoiceWebhookRequestModel.data.invoice.custom_fields.Find(x => x.label == "OrgId");

                        var tevDeviceext = await _deviceRepo.GetDevice(deviceDetail.value,orgDetail.value);
                        device = new Device
                        {
                            OrgId = tevDeviceext.OrgId,
                            LogicalDeviceId = tevDeviceext.LogicalDeviceId,
                            DeviceName = tevDeviceext.DeviceName
                        };

                        var invoice = new InvoiceHistory
                        {
                            OrgId = device.OrgId,
                            EventType = zohoInvoiceWebhookRequestModel.event_type,
                            CreatedTime = Convert.ToDateTime(zohoInvoiceWebhookRequestModel.created_time),
                            InvoiceId = zohoInvoiceWebhookRequestModel.data.invoice.invoice_id,
                            InvoiceNumber = zohoInvoiceWebhookRequestModel.data.invoice.number,
                            Total = zohoInvoiceWebhookRequestModel.data.invoice.total,
                            Email = zohoInvoiceWebhookRequestModel.data.invoice.email,
                            CustomerId = zohoInvoiceWebhookRequestModel.data.invoice.customer_id,
                            CustomerName = zohoInvoiceWebhookRequestModel.data.invoice.customer_name,
                            Balance = zohoInvoiceWebhookRequestModel.data.invoice.balance,
                            CurrencyCode = zohoInvoiceWebhookRequestModel.data.invoice.currency_code,
                            InvoiceDate = Convert.ToDateTime(zohoInvoiceWebhookRequestModel.data.invoice.updated_time),
                            CreatedDate = DateTime.UtcNow.Ticks,
                            ModifiedDate = DateTime.UtcNow.Ticks,
                            CreatedBy = zohoInvoiceWebhookRequestModel.data.invoice.email,
                            ModifiedBy = zohoInvoiceWebhookRequestModel.data.invoice.email
                        };

                        _invoiceHistoryRepo.Add(invoice);
                        _invoiceHistoryRepo.SaveChanges();

                        var invoiceDetail = _invoiceHistoryRepo.Query(x => x.Id == invoice.Id).FirstOrDefault();

                        foreach (var item in zohoInvoiceWebhookRequestModel.data.invoice.invoice_items)
                        {
                            var invoiceItem = new InvoiceHistoryItem
                            {
                                ItemCode = item.code,
                                ItemId = item.item_id,
                                Name = item.name,
                                Description = item.description,
                                Price = item.price,
                                Quantity = item.quantity,
                                CreatedDate = DateTime.UtcNow.Ticks,
                                ModifiedDate = DateTime.UtcNow.Ticks,
                                CreatedBy = zohoInvoiceWebhookRequestModel.data.invoice.email,
                                ModifiedBy = zohoInvoiceWebhookRequestModel.data.invoice.email,
                                InvoiceHistory = invoiceDetail
                            };

                            _invoiceItemRepo.Add(invoiceItem);
                            _invoiceItemRepo.SaveChanges();
                        }

                        foreach (var paymentItem in zohoInvoiceWebhookRequestModel.data.invoice.payments)
                        {
                            var payment = new InvoiceHistoryPayment
                            {
                                PaymentId = paymentItem.payment_id,
                                Amount = paymentItem.amount,
                                AmountRefunded = paymentItem.amount_refunded,
                                Description = paymentItem.description,
                                BankCharges = paymentItem.bank_charges,
                                CreatedDate = DateTime.UtcNow.Ticks,
                                ModifiedDate = DateTime.UtcNow.Ticks,
                                CreatedBy = zohoInvoiceWebhookRequestModel.data.invoice.email,
                                ModifiedBy = zohoInvoiceWebhookRequestModel.data.invoice.email,
                                InvoiceHistory = invoiceDetail
                            };
                            _paymentInvoiceHistoryRepo.Add(payment);
                            _paymentInvoiceHistoryRepo.SaveChanges();
                        }

                        foreach (var subscription in zohoInvoiceWebhookRequestModel.data.invoice.subscriptions)
                        {
                            var sub = new InvoiceHistorySubscription
                            {
                                SubscriptionId = subscription.subscription_id,
                                CreatedDate = DateTime.UtcNow.Ticks,
                                ModifiedDate = DateTime.UtcNow.Ticks,
                                CreatedBy = zohoInvoiceWebhookRequestModel.data.invoice.email,
                                ModifiedBy = zohoInvoiceWebhookRequestModel.data.invoice.email,
                                InvoiceHistory = invoiceDetail
                            };

                            _invoiceHistorySubscriptionRepo.Add(sub);
                            _invoiceHistorySubscriptionRepo.SaveChanges();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError("Invoice history webhook failed & following are the exception : - {e}", ex);
                    }
                }
            }
            else
            {
                _logger.LogError("Payment history webhook failed. wrong authentication key is passed");
            }
        }

        #endregion
    }
}
