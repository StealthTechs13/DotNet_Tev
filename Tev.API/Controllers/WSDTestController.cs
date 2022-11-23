using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Models;
using Tev.Cosmos;
using Tev.Cosmos.Entity;
using Tev.Cosmos.IRepository;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;
using Tev.IotHub.Models;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class WSDTestController : TevControllerBase
    {
        private readonly ITevIoTRegistry _iotHub;
        private readonly IGenericRepo<WSDTest> _wsdTestRepo;
        private readonly IWSDSummaryAlertRepo _cosmos;
        private readonly ILogger<WSDTestController> _logger;
        private readonly IDeviceRepo _deviceRepo;
        private readonly IGenericRepo<DeviceFactoryData> _deviceFactoryDataRepo;
        private readonly IConfiguration _configuration;

        public WSDTestController(ITevIoTRegistry iotHub, IGenericRepo<WSDTest> wsdTestRepo, IWSDSummaryAlertRepo cosmos, ILogger<WSDTestController> logger, IDeviceRepo deviceRepo, IGenericRepo<DeviceFactoryData> deviceFactoryDataRepo, IConfiguration configuration)
        {
            _iotHub = iotHub;
            _wsdTestRepo = wsdTestRepo;
            _cosmos = cosmos;
            _logger = logger;
            _deviceRepo = deviceRepo;
            _deviceFactoryDataRepo = deviceFactoryDataRepo;
            _configuration = configuration;
        }

    
        [HttpGet("summaryAlerts/{deviceId}")]
        public async Task<IActionResult> GetWSDSummaryAlert(string deviceId)
        {
            try
            {
                var result = await _cosmos.GetDeviceSummaryAlerts(OrgId, deviceId);
                
                return Ok(new MMSHttpReponse<List<WSDSummaryEntity>> {ResponseBody=result});
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured while geeting WSD alert {exception}", ex);
                return StatusCode(500);
            }
        }

        [HttpGet("summaryData/{deviceId}")]
        public async Task<IActionResult> GetWSDSummaryData(string deviceId,string deviceName)
        {
            try
            {
                var result = await _cosmos.GetDeviceSummaryAlerts(OrgId, deviceId);
                var res = new MMSHttpReponse<List<WSDSummaryDataEntity>> { ResponseBody = null };
                var wsdList = new List<WSDSummaryDataEntity>();
                var device = await _deviceRepo.GetDevice(deviceId, OrgId);
                var testButton = false;
                var smokeValue = false;
                var mspCcVersion = false;
                
                var WsdCcFactoryVersion = _configuration.GetSection("WsdCcFactoryVersion").Value;
                var WsdMspFactoryVersion = _configuration.GetSection("WsdMspFactoryVersion").Value;
                if (WsdCcFactoryVersion == device.CurrentFirmwareVersion && WsdMspFactoryVersion == device.mspVersion)
                {
                    mspCcVersion = true;
                }
               foreach (var d in result)
                {
                    var wsdData = new WSDSummaryDataEntity
                    {
                        SmokeValue = d.SmokeValue,
                        SmokeStatus = d.SmokeStatus,
                        BatteryStatus = d.BatteryStatus,
                        BatteryValue = d.BatteryValue,
                        EnqueuedTimestamp = d.EnqueuedTimestamp,
                        TestId = d.TestId,
                        AlertType = d.AlertType,
                        MspVersionTargeted = WsdMspFactoryVersion,
                        MspVersionDevice = device.mspVersion,
                        CcVersionDevice = device.CurrentFirmwareVersion,
                        CcVersionTargeted = WsdCcFactoryVersion,
                        CertificateId = device.Id,
                        LogicalDeviceId = device.LogicalDeviceId,
                    };
                    if(d.AlertType == 101)
                    {
                        testButton = true;
                    }
                    if(d.AlertType == null)
                    {
                       if(d.SmokeValue >= 950 && d.SmokeValue <= 1150)
                        {
                            smokeValue = true;
                        }
                    }
                    wsdList.Add(wsdData);
                }
                res.ResponseBody = wsdList;
               var factoryData =  _deviceFactoryDataRepo.GetAll().Where(f => f.DeviceName == deviceName).FirstOrDefault();
                if(factoryData != null)
                {
                    factoryData.CertificateID = device.Id;
                    factoryData.CcDeviceversion = device.CurrentFirmwareVersion;
                    factoryData.CcTargetedversion = WsdCcFactoryVersion;
                    factoryData.LogicalDeviceID = device.LogicalDeviceId;
                    factoryData.MspDeviceversion = device.mspVersion;
                    factoryData.MspTargetedversion = WsdMspFactoryVersion;
                    factoryData.Result = testButton && smokeValue && mspCcVersion == true ? true : false;
                    factoryData.FailureReasons = factoryData.Result == true ? "" : "Testbutton :" + testButton.ToString() + ", smokeValue :" + smokeValue.ToString() + ",mspCcVersion :" + mspCcVersion.ToString();
                }
                _deviceFactoryDataRepo.Update(factoryData);
                _deviceFactoryDataRepo.SaveChanges();
                return Ok(res);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured while geeting WSD alert {exception}", ex);
                return StatusCode(500);
            }
        }

        [HttpGet("testdata/{deviceId}")]
        public IActionResult GetTestData(string deviceId)
        {
            var result = _wsdTestRepo.Query(x => x.DeviceId == deviceId).ToList();
            return Ok(new MMSHttpReponse<List<WSDTest>> { ResponseBody=result});

        }
      
        [HttpPost("send/{deviceId}")]
        public async Task<IActionResult> SendDatatoDevice(string deviceId,[FromBody] WSDTestData reqBody)
        {
            if (reqBody != null)
            {
                var entity = new WSDTest
                {
                    DeviceId = deviceId,
                    GTemperatureSensorOffset2 = reqBody.GTemperatureSensorOffset2,
                    GTemperatureSensorOffset = reqBody.GTemperatureSensorOffset,
                    ClearAir = reqBody.ClearAir,
                    IREDCalibration = reqBody.IREDCalibration,
                    PhotoOffset = reqBody.PhotoOffset,
                    DriftLimit = reqBody.DriftLimit,
                    DriftBypass = reqBody.DriftBypass,
                    TransmitResolution = reqBody.TransmitResolution,
                    TransmitThreshold = reqBody.TransmitThreshold,
                    SmokeThreshold = reqBody.SmokeThreshold
                };
                var createdEntity = await _wsdTestRepo.AddAsync(entity);
                _wsdTestRepo.SaveChanges();
                reqBody.TestId = createdEntity.Id;
                await _iotHub.SendDataToDevice(deviceId, reqBody);
                return Ok();
            }
            else
            {
                return BadRequest();
            }
           
        }
    }
}
