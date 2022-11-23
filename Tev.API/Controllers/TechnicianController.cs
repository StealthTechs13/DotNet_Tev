using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Extensions;
using MMSConstants;
using Tev.API.Models;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class TechnicianController : ControllerBase
    {
        private readonly IGenericRepo<Technician> _technicianRepo;
        private readonly IGenericRepo<TechnicianDevices> _technicianDevicesRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TechnicianController> _logger;

        public TechnicianController(IUnitOfWork unitOfWork, IGenericRepo<Technician> technicianRepository, ILogger<TechnicianController> logger,
            IGenericRepo<TechnicianDevices> technicianDevicesRepo)
        {
            _unitOfWork = unitOfWork;
            _technicianRepo = technicianRepository;
            _technicianDevicesRepo = technicianDevicesRepo;
            _logger = logger;
        }

        /// <summary>
        /// create Technician post method
        /// </summary>
        /// <param name="model">model contains name,email,phone,latitude,longitude values, in this model technicianId not required.</param>
        /// <returns>it returns technician Id.</returns>
        [HttpPost("createTechnician")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult CreateTechnician([FromBody] TechnicianModel model)
        {
            try
            {
                foreach (var items in model.DeviceTypes)
                {
                    Applications applications;
                    var EnumValidate = Enum.TryParse(items, out applications);
                    if (!EnumValidate)
                    {
                        return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "Please Enter Valid Device Name" });
                    }
                }

                var isValidTechnicianType = Enum.TryParse<TechnicianTypeEnum>(model.TechnicianType, out var technicianType);

                if (!isValidTechnicianType) 
                {
                    return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "Invalid Technician Type Found"});
                }


                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var TechModel = new Technician()
                        {
                            TechnicianId = Guid.NewGuid().ToString(),
                            Name = model.Name,
                            Email = model.Email,
                            Phone = model.Phone,
                            TechnicianType = technicianType,
                            Latitude = model.Latitude,
                            Longitude = model.Longitude,
                            CreatedDate = DateTime.UtcNow.Ticks,
                            ModifiedDate = DateTime.UtcNow.Ticks,
                            Address=model.Address,
                            TechnicianDevices = model.DeviceTypes.Select(q => new TechnicianDevices()
                            {
                                TechnicianDeviceId = Guid.NewGuid().ToString(),
                                DeviceType = (Applications)Enum.Parse(typeof(Applications), q.ToString())
                            }).ToList()
                        };

                        _technicianRepo.Add(TechModel);
                        _technicianRepo.SaveChanges();

                        transaction.Commit();
                        _logger.LogInformation("Technician successfully Added on Sql");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = TechModel.TechnicianId, SuccessMessage = "Technician Successfully Registered." });
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _logger.LogError("Error Occured on Create Technician on Sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Create Technician on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update Technician data
        /// </summary>
        /// <param name="model">model contains technicianId,name,email,phone,latitude,longitude values.</param>
        /// <returns>it returns success message.</returns>
        [HttpPost("updateTechnician")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult UpdateTechnician([FromBody] TechnicianModel model)
        {
            try
            {
                var technician = _technicianRepo.Query(z => z.TechnicianId == model.TechnicianId).Include(q => q.TechnicianDevices).FirstOrDefault();

                if (technician == null)
                {
                    return NotFound(new MMSHttpReponse<string>() { ErrorMessage = "Requested technician not found"});
                }

                foreach (var items in model.DeviceTypes)
                {
                    Applications applications;
                    var EnumValidate = Enum.TryParse(items, out applications);
                    if (!EnumValidate)
                    {
                        return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "Please Enter Valid Device Name" });
                    }
                }

                var isValidTechnicianType = Enum.TryParse<TechnicianTypeEnum>(model.TechnicianType, out var technicianType);

                if (!isValidTechnicianType)
                {
                    return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "Invalid Technician Type Found"});
                }

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        #region TechDeviceUpdate
                        var DeletedDevice = new List<string>();
                        var AddDevicelist = new List<string>();
                        var DbDevice = technician.TechnicianDevices.Select(q => q.DeviceType.GetName()).ToList();
                        DeletedDevice = DbDevice.Where(z => !model.DeviceTypes.Contains(z)).ToList();
                        AddDevicelist = model.DeviceTypes.Where(z => DbDevice.Contains(z)).ToList();

                        foreach (var item in DeletedDevice)
                        {
                            var TechDevice = (Applications)Enum.Parse(typeof(Applications), item);
                            var technicianDeviceMap = _technicianDevicesRepo.Query(q => q.TechnicianId == model.TechnicianId
                                                && q.DeviceType == TechDevice).FirstOrDefault();
                            _technicianDevicesRepo.Remove(technicianDeviceMap);
                        }
                        _technicianDevicesRepo.SaveChanges();

                        foreach (var item in AddDevicelist)
                        {
                            var TechDevice = (Applications)Enum.Parse(typeof(Applications), item);
                            var available = _technicianDevicesRepo.Query(q => q.TechnicianId == model.TechnicianId && q.DeviceType == TechDevice).Any();
                            if (!available)
                            {
                                _technicianDevicesRepo.Add(new TechnicianDevices()
                                {
                                    TechnicianId = model.TechnicianId,
                                    DeviceType = TechDevice
                                });
                            }
                        }
                        _technicianDevicesRepo.SaveChanges();

                        #endregion

                        technician.Name = model.Name;
                        technician.Email = model.Email;
                        technician.Phone = model.Phone;
                        technician.TechnicianType = technicianType;
                        technician.Latitude = model.Latitude;
                        technician.Longitude = model.Longitude;
                        technician.Address = model.Address;
                        technician.ModifiedDate = DateTime.UtcNow.Ticks;
                        _technicianRepo.Update(technician);
                        _technicianRepo.SaveChanges();

                        transaction.Commit();
                        _logger.LogInformation("Technician Successfully Updated on Sql");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = "Update Technician Successfully", SuccessMessage = "Technician Successfully Registered." });
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _logger.LogError("Error Occured on Update Technician on Sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Update Technician on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get technician by technician Id
        /// </summary>
        /// <param name="technicianId">string technicianId</param>
        /// <returns>it returns single technician details.</returns>
        [HttpGet("getTechnicianById")]
        [ProducesResponseType(typeof(MMSHttpReponse<TechnicianModel>), StatusCodes.Status200OK)]
        public IActionResult GetTechnicianById([FromQuery] string technicianId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(technicianId))
                {
                    return BadRequest(new MMSHttpReponse<TechnicianModel>() { ErrorMessage = "Invalid technician Id"});
                }

                var technician = _technicianRepo.Query(z => z.TechnicianId == technicianId).Include(q => q.TechnicianDevices).FirstOrDefault();

                if (technician == null)
                {
                    return NotFound(new MMSHttpReponse<TechnicianModel>() { ErrorMessage = "Requested technician not found" });
                }

                var resp = new MMSHttpReponse<TechnicianModel>()
                {
                    ResponseBody = new TechnicianModel()
                    {
                        TechnicianId = technician.TechnicianId,
                        Email = technician.Email,
                        Latitude = technician.Latitude,
                        TechnicianType = technician.TechnicianType.GetName(),
                        Longitude = technician.Longitude,
                        Name = technician.Name,
                        Phone = technician.Phone,
                        DeviceTypes = technician.TechnicianDevices.Select(q => q.DeviceType.GetName()).ToList()
                    }
                };

                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Technician By Id  {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get all technician from database.
        /// </summary>
        /// <returns>it returns all technicians from db</returns>
        [HttpGet("getAllTechnician")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<TechnicianModel>>), StatusCodes.Status200OK)]
        public IActionResult GetAllTechnician()
        {
            try
            {
                var allTechnicians = _technicianRepo.GetAll().Include(q => q.TechnicianDevices).Select(tech => new TechnicianModel()
                {
                    TechnicianId = tech.TechnicianId,
                    Email = tech.Email,
                    Latitude = tech.Latitude,
                    Longitude = tech.Longitude,
                    TechnicianType = tech.TechnicianType.GetName(),
                    Name = tech.Name,
                    Phone = tech.Phone,
                    DeviceTypes = tech.TechnicianDevices.Select(q => q.DeviceType.GetName()).ToList(),
                    Address=tech.Address
                }).ToList();
                return Ok(new MMSHttpReponse<List<TechnicianModel>>() { ResponseBody = allTechnicians });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Technicians from Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// delete technician by Id
        /// </summary>
        /// <param name="technicianId">string technicianId</param>
        /// <returns>it returns success Message</returns>
        [HttpDelete("deleteTechnician")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult DeleteTechnician([FromQuery] string technicianId)
        {
            try
            {
                var technician = _technicianRepo.Query(z => z.TechnicianId == technicianId).FirstOrDefault();

                if (technician == null)
                {
                    return NotFound(new MMSHttpReponse<string>() { ErrorMessage = "Requested technician not found"});
                }

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var technicianDevice = _technicianDevicesRepo.Query(q => q.TechnicianId == technicianId).ToList();
                        foreach (var items in technicianDevice)
                        {
                            _technicianDevicesRepo.Remove(items);
                        }
                        _technicianDevicesRepo.SaveChanges();

                        _technicianRepo.Remove(technician);
                        _technicianRepo.SaveChanges();
                        transaction.Commit();
                        _logger.LogInformation("Technician Successfully deleted");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = "Delete Technician Successfully", SuccessMessage = "Technician Successfully Deleted." });
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _logger.LogError("Error Occured on Delete Technician on Sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Delete Technician on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get all technician paginated
        /// </summary>
        /// <param name="req">request contains pageNo,perPageRecordCount,sortBy,sortOrder,search</param>
        /// <returns>it returns all technician paginated response</returns>
        [HttpPost("getAllTechnicianPaginated")]
        [ProducesResponseType(typeof(MMSHttpReponse<GetAllPaginateResponse<TechnicianModel>>), StatusCodes.Status200OK)]
        public IActionResult GetAllTechnicianPaginated([FromBody]GetAllTechnicianPaginateReq req)
        {
            try
            {
                var technician = _technicianRepo.GetAll();

                if (!string.IsNullOrWhiteSpace(req.Search))
                {
                    technician = technician.Where(z => z.Name.Contains(req.Search) || z.Email.Contains(req.Search) || z.Phone.Contains(req.Search));
                }

                if (string.IsNullOrWhiteSpace(req.SortBy))
                {
                    req.SortBy = "ModifiedDate";
                }

                var allCount = technician.Count();

                switch (req.SortBy.ToUpper())
                {
                    case "NAME":
                        {
                            technician = req.SortOrder == "asc" ? technician.OrderBy(z => z.Name) : technician.OrderByDescending(z => z.Name);
                            break;
                        }

                    case "EMAIL":
                        {
                            technician = req.SortOrder == "asc" ? technician.OrderBy(z => z.Email) : technician.OrderByDescending(z => z.Email);
                            break;
                        }

                    case "PHONE":
                        {
                            technician = req.SortOrder == "asc" ? technician.OrderBy(z => z.Phone) : technician.OrderByDescending(z => z.Phone);
                            break;
                        }

                    default:
                        {
                            technician = technician.OrderByDescending(z => z.ModifiedDate);
                            break;
                        }
                }

                var data = technician.Skip(((req.PageNo - 1) * req.PageSize)).Take(req.PageSize).Select(tech => new TechnicianModel()
                {
                    TechnicianId = tech.TechnicianId,
                    Email = tech.Email,
                    Latitude = tech.Latitude,
                    TechnicianType = tech.TechnicianType.GetName(),
                    Longitude = tech.Longitude,
                    Name = tech.Name,
                    Phone = tech.Phone
                }).ToList();

                var resp = new MMSHttpReponse<GetAllPaginateResponse<TechnicianModel>>()
                {
                    ResponseBody = new GetAllPaginateResponse<TechnicianModel>()
                    {
                        AllRecordCount = allCount,
                        Data = data,
                        PageNo = req.PageNo,
                        PageSize = req.PageSize,
                        SortBy = req.SortBy,
                        SortOrder = req.SortOrder,
                    }
                };


                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Update Technician on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        
        [HttpGet("getTechnicianTypes")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<GetAllDisplayResponse>>), StatusCodes.Status200OK)]
        public IActionResult GetTechnicianTypes()
        {
            try
            {
                var allTechnicianTypes = Enum.GetValues(typeof(TechnicianTypeEnum)).Cast<TechnicianTypeEnum>().ToList();

                var resp = allTechnicianTypes.Select(z => new GetAllDisplayResponse()
                {
                    DisplayName = z.GetName(),
                    Value = z.GetName()
                }).ToList();

                return Ok(new MMSHttpReponse<List<GetAllDisplayResponse>>() { ResponseBody = resp});
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Technicians Types {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
