using Google.Apis.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Models;
using Tev.Cosmos.IRepository;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.HelperService;
using Tev.DAL.RepoContract;
using Tev.IotHub;

namespace Tev.API.Controllers
{
    /// <summary>
    /// APIs for location management
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class LocationController : TevControllerBase
    {


        private readonly IUserDevicePermissionService _userDevicePermissionService;
        private readonly ITevIoTRegistry _iotHub;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepo<Location> _locationRepo;
        private readonly ILogger<LocationController> _logger;
        private readonly IDeviceRepo _deviceRepo;

        public LocationController(IUserDevicePermissionService userDevicePermissionService, ITevIoTRegistry iotHub, IGenericRepo<Location> locationRepo,
            IUnitOfWork unitOfWork, ILogger<LocationController> logger, IDeviceRepo deviceRepo)
        {

            _userDevicePermissionService = userDevicePermissionService;
            _iotHub = iotHub;
            _unitOfWork = unitOfWork;
            _locationRepo = locationRepo;
            _logger = logger;
            _deviceRepo = deviceRepo;
        }

        /// <summary>
        /// Get all the locations of a logged in user
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(MMSHttpReponse<List<LocationResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public IActionResult GetLocations()
        {
            try
            {
                var locations = _locationRepo.Query(x => x.OrgId == OrgId).ToList();
                var ret = locations.Select(x =>
                {
                    return new LocationResponse
                    {
                        Id = x.Id,
                        Name = x.Name

                    };
                }).ToList();
                return Ok(new MMSHttpReponse<List<LocationResponse>> { ResponseBody = ret });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while getting locations {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Adds a location to the logged in user's org
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("add")]
        [ProducesResponseType(typeof(MMSHttpReponse<UpdateLocationRequest>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddLocation([FromBody] AddLocationRequest reqBody)
        {
            // If not org admin check if the user has permission to add site to the parent site or not
            if (!IsOrgAdmin(CurrentApplications))
            {

                return Forbid();

            }
            try
            {
                // check if the name already exists
                var locationWithSameName = _locationRepo.Query(x => x.Name.ToLower() == reqBody.LocationName.ToLower() && x.OrgId == OrgId).FirstOrDefault();
                if (locationWithSameName != null)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "Location name already exist" });
                }
                var id = Guid.NewGuid().ToString();
                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var location = new Location
                        {
                            CreatedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds(),
                            OrgId = OrgId,
                            CreatedBy = UserEmail,
                            Name = reqBody.LocationName,
                            Id = id
                        };
                        await _locationRepo.AddAsync(location);
                        _locationRepo.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {


                        transaction.Rollback();
                        _logger.LogError("Error Occured while creating location {exception}", ex);
                        return StatusCode(StatusCodes.Status500InternalServerError);

                    }
                }
                return Ok(new MMSHttpReponse<UpdateLocationRequest> { SuccessMessage = "Location added successfully.", 
                    ResponseBody = new UpdateLocationRequest { LocationId = id,LocationName=reqBody.LocationName} });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured while creating location {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates the location name of provided locationId
        /// </summary>
        /// <param name="reqBody"></param>
        /// <returns></returns>
        [HttpPost("update")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public IActionResult UpdateLocation([FromBody] UpdateLocationRequest reqBody)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!IsOrgAdmin(CurrentApplications))
                    {
                        Forbid();
                    }

                    var availableLocation = _locationRepo.Query(z => z.Id == reqBody.LocationId && z.OrgId == OrgId).FirstOrDefault();


                    if (availableLocation == null)
                    {
                        return Ok(new MMSHttpReponse { ErrorMessage = "Location not found for update." });
                    }
                    // check if the name already exists
                    var locationWithSameName = _locationRepo.Query(x => x.Name.ToLower() == reqBody.LocationName.ToLower() && x.OrgId == OrgId).FirstOrDefault();
                    if (locationWithSameName != null)
                    {
                        return BadRequest(new MMSHttpReponse { ErrorMessage = "Location name already exist" });
                    }

                    _iotHub.UpdateDeviceTwinPropertyLocation(reqBody.LocationId, reqBody.LocationName);

                    //Update Cosmos Device Data
                    var deviceReplica = _deviceRepo.GetDevicesByLocation(reqBody.LocationId, OrgId).Result;
                    foreach (var item in deviceReplica)
                    {
                        item.LocationName = reqBody.LocationName;
                        item.TwinChangeStatus =  TwinChangeStatus.Default;
                        _deviceRepo.UpdateDevice(OrgId, item);
                    }
                 
                    using (var transaction = _unitOfWork.BeginTransaction())
                    {

                        try
                        {
                            availableLocation.Name = reqBody.LocationName;
                            availableLocation.ModifiedBy = UserEmail;

                            _locationRepo.Update(availableLocation);
                            _locationRepo.SaveChanges();

                            transaction.Commit();
                            _logger.LogInformation("Location updated successfully");
                            return Ok(new MMSHttpReponse { SuccessMessage = "Location updated successfully." });
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("Error occured on update location Sql {exception}", e);
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error occured on Update Location {exception}", e);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                return BadRequest();
            }
          
        }

        /// <summary>
        /// Deletes the provided location
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        [HttpDelete("delete")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        public IActionResult DeleteLocation([FromQuery] string locationId)
        {
            try
            {
                if (!IsOrgAdmin(CurrentApplications))
                {
                    return Forbid();
                }

                if (string.IsNullOrWhiteSpace(locationId))
                {
                    return Ok(new MMSHttpReponse { ErrorMessage = "Invalid LocationId Found." });
                }


                var availableLocation = _locationRepo.Query(z => z.Id == locationId && z.OrgId == OrgId).FirstOrDefault();

                if (availableLocation == null)
                {
                    return Ok(new MMSHttpReponse { ErrorMessage = "Location not found for delete." });
                }

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        _locationRepo.Remove(availableLocation);
                        _locationRepo.SaveChanges();
                        transaction.Commit();
                        _logger.LogInformation("Location Successfully deleted");
                        return Ok(new MMSHttpReponse { SuccessMessage = "Location deleted successfully." });
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Error occured on deleted location Sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured on deleted Location {exception}", e);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
