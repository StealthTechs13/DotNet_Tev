using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MMSConstants;
using Tev.API.Mocks;
using Tev.API.Models;
using Tev.Cosmos.Entity;
using Tev.Cosmos.IRepository;
using Tev.IotHub;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class FirmwareController : TevControllerBase
    {
        private readonly IMockData _mockData;
        private readonly ITevIoTRegistry _tevIotRegistry;
        private readonly ILogger<FirmwareController> _logger;
        private readonly IFirmwareUpdateHistoryRepo _firmwareUpdateRepo;
        private readonly IDeviceRepo _deviceRepo;

        public FirmwareController(IMockData mockData, ITevIoTRegistry tevIoTRegistry, ILogger<FirmwareController> logger,
            IFirmwareUpdateHistoryRepo firmwareUpdateRepo, IDeviceRepo deviceRepo)
        {
            _mockData = mockData;
            _tevIotRegistry = tevIoTRegistry;
            _logger = logger;
            _firmwareUpdateRepo = firmwareUpdateRepo;
            _deviceRepo = deviceRepo;
        }
        [HttpGet("history/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<FirmwareVersionHistoryResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFirwareVersionHistory(string deviceId)
        {
            try
            {
                var result = await _firmwareUpdateRepo.GetFirmwareUpdateHisotry(deviceId,OrgId);
                var response = new List<FirmwareVersionHistoryResponse>();

                if (result.Count > 0)
                {
                    result.ForEach(x => {
                        response.Add(new FirmwareVersionHistoryResponse
                        {
                            Version = x.Version.Contains(".")? x.Version: $"{x.Version}.0",
                            InstalledOn = x.UpdatedTime
                        });
                    });
                }
               
                return Ok(new MMSHttpReponse<List<FirmwareVersionHistoryResponse>> { ResponseBody = response, SuccessMessage = "success" });
                
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("update/{deviceId}")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateFirmware(string deviceId)
        {
            try
            {
                var device = await _deviceRepo.GetDevice(deviceId,OrgId);
                if (device == null || string.IsNullOrEmpty(device.LogicalDeviceId))
                {
                    return NotFound();
                }
                var newFirmwareVersionAvailableToOrg = string.Empty;
                var firmwareVersion = string.Empty;
                var newfirmwareVersion = _deviceRepo.GetLatestFirmwareVersion(device.DeviceType).GetAwaiter().GetResult(); //Geting New Firmware version avilable for all Orgs from Org Id 0 

                if (device.CurrentFirmwareVersion != null)
                {
                    firmwareVersion = device.CurrentFirmwareVersion.Contains(".") ? device.CurrentFirmwareVersion : $"{device.CurrentFirmwareVersion}.0";
                }

                if (newfirmwareVersion != null)
                {
                    newfirmwareVersion = newfirmwareVersion.Contains(".") ? newfirmwareVersion : $"{newfirmwareVersion}.0";
                    newFirmwareVersionAvailableToOrg = newfirmwareVersion;
                }
                if (device.Firmware?.NewFirmwareVersion != null) //Getting latest firmware version whichever is greatest among individual org and all org
                {
                    newFirmwareVersionAvailableToOrg = Convert.ToDouble(device.Firmware.NewFirmwareVersion) > Convert.ToDouble(newfirmwareVersion) ? device.Firmware.NewFirmwareVersion : newfirmwareVersion;
                }
                if (newfirmwareVersion == null || newFirmwareVersionAvailableToOrg.Equals(firmwareVersion))
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "No new firmware update available for this device." });
                }
                //if(device.Firmware.UserApproved == true)
                //{
                //    return Ok(new MMSHttpReponse { SuccessMessage = "Device scheduled for update, you will get a notification after the update." });
                //}
                var deviceTwin = await _tevIotRegistry.GetDeviceById(deviceId).ConfigureAwait(false);
                if (Convert.ToDouble(deviceTwin.FirmwareVersion) >= Convert.ToDouble(newfirmwareVersion))
                {
                    device.CurrentFirmwareVersion = deviceTwin.FirmwareVersion;
                    await _deviceRepo.UpdateDevice(OrgId, device);
                    return Ok(new MMSHttpReponse { SuccessMessage = "Device software updated successfully." });
                }
                var isUpdate = await _tevIotRegistry.UpdateFirmware(deviceId);

                //Update Cosmos Device Data
                if(device != null)
                {
                    device.Firmware.UserApproved = true;
                    device.TwinChangeStatus = TwinChangeStatus.DesiredPropFirmwareUpdate;
                    await _deviceRepo.UpdateDevice(OrgId, device);
                }
                if (isUpdate)
                {
                    return Ok(new MMSHttpReponse { SuccessMessage = "Device software update is in progress. Your alert services will be affected during this period." });
                }
                else
                {
                    _logger.LogError("Error while updating firmware update deive twin, isUpdate valud is {0}", isUpdate);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Error in FirmwareController method - UpdateFirmware : -  {0}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
