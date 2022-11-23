/*  Not in use kept for future use.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
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
    public class EscalationMatrixController : ControllerBase
    {
        private readonly IGenericRepo<EscalationMatrix> _escalationMetrixRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EscalationMatrixController> _logger;
        public EscalationMatrixController(IGenericRepo<EscalationMatrix> escalationMetrixRepo, IUnitOfWork unitOfWork, ILogger<EscalationMatrixController> logger)
        {
            _escalationMetrixRepo = escalationMetrixRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Create Escalation matrix
        /// </summary>
        /// <param name="model">create escalation matrix model that containes organizationId,deviceId,receiverName,receiverDescription,attentionTime etc.</param>
        /// <returns>it returns string entity Id</returns>
        [HttpPost("createEscalationMetrix")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult CreateEscalationMetrix([FromBody] EscalationMatrixModel model)
        {
            try
            {
                var Validate = ValidateEscalationMetrixModel(model);
                if (Validate.Any())
                {
                    return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = Validate.FirstOrDefault().ToString()});
                }
                var escalationLevel = (EscalationLevelEnum)Enum.Parse(typeof(EscalationLevelEnum), model.EscalationLevel);
                var alreadyAvailable = _escalationMetrixRepo.Query(z => z.DeviceId == model.DeviceId && z.OrganizationId == model.OrganizationId && 
                z.EscalationLevel == escalationLevel).Any();

                if (alreadyAvailable)
                {
                    return BadRequest(new MMSHttpReponse<string>()
                    {
                        ErrorMessage = "Escalation is already available for this device."});
                }
                using (var transation = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var escalationMatrixModel = new EscalationMatrix()
                        {
                            EscalationMatrixId = Guid.NewGuid().ToString(),
                            AttentionTime = model.AttentionTime,
                            DeviceId = model.DeviceId,
                            EscalationLevel = escalationLevel,
                            CreatedDate = DateTime.UtcNow.Ticks,
                            ModifiedDate = DateTime.UtcNow.Ticks,
                            OrganizationId = model.OrganizationId,
                            ReceiverName = model.ReceiverName,
                            ReceiverDescription = model.ReceiverDescription,
                            ReceiverPhone = model.ReceiverPhone,
                            SenderPhone = model.SenderPhone,
                            SmokeStatus = (SmokeStatusEnum)Enum.Parse(typeof(SmokeStatusEnum), model.SmokeStatus),
                            SmokeValue = (SmokeValueEnum)Enum.Parse(typeof(SmokeValueEnum), model.SmokeValue),
                        };

                        _escalationMetrixRepo.Add(escalationMatrixModel);
                        _escalationMetrixRepo.SaveChanges();

                        transation.Commit();
                        _logger.LogInformation("Escalation metrix successfully added on sql");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = escalationMatrixModel.EscalationMatrixId, SuccessMessage = "Escalation Successfully created" });
                    }
                    catch (Exception e)
                    {
                        transation.Rollback();
                        _logger.LogError("Error Occured on Create Escalation Matrix on sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Create Escalation Matrix on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private List<string> ValidateEscalationMetrixModel(EscalationMatrixModel model)
        {
            var errors = new List<string>();
           
            EscalationLevelEnum escalationLevel;
            if (!Enum.TryParse<EscalationLevelEnum>(model.EscalationLevel, out escalationLevel))
            {
                   errors.Add("Invalid Escalation Level found.");
            }

            SmokeStatusEnum smokeStatus;
            if (Enum.TryParse<SmokeStatusEnum>(model.EscalationLevel, out smokeStatus))
            {
                errors.Add("Invalid Smoke Status found.");
            }

            SmokeValueEnum smokeValue;
            if (Enum.TryParse<SmokeValueEnum>(model.EscalationLevel, out smokeValue))
            {
                errors.Add("Invalid Smoke Value found.");
            }

            return errors;
        }

        /// <summary>
        /// Update Escalation Matrix
        /// </summary>
        /// <param name="model">update escalation matrix model that containes escalationMatrixId,organizationId,deviceId,receiverName,receiverDescription,attentionTime etc.</param>
        /// <returns></returns>
        [HttpPost("updateEscalationMetrix")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult UpdateEscalationMetrix([FromBody] EscalationMatrixModel model)
        {
            try
            {
                var Validate = ValidateEscalationMetrixModel(model);
                if (Validate.Any())
                {
                    return BadRequest(new MMSHttpReponse<string>() {ErrorMessage = Validate.FirstOrDefault().ToString() });
                }

                if (string.IsNullOrWhiteSpace(model.EscalationMatrixId))
                {
                    return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "Escalation Matrix Id invalid." });
                }

                var escalationLevel = (EscalationLevelEnum)Enum.Parse(typeof(EscalationLevelEnum), model.EscalationLevel);
                var alreadyAvailable = _escalationMetrixRepo.Query(z => z.EscalationMatrixId != model.EscalationMatrixId && z.DeviceId == model.DeviceId
                                                                        && z.OrganizationId == model.OrganizationId && z.EscalationLevel == escalationLevel).Any();

                if (alreadyAvailable)
                {
                    return BadRequest(new MMSHttpReponse<string>()
                    {
                        ErrorMessage = "Escalation is already available for this device."
                    });
                }

                var availableEscalationMetrix = _escalationMetrixRepo.Query(z => z.EscalationMatrixId == model.EscalationMatrixId).FirstOrDefault();

                if (availableEscalationMetrix == null)
                {
                    return NotFound(new MMSHttpReponse<string>() {ErrorMessage = "Escalation Not found." });
                }

                using (var transation = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        availableEscalationMetrix.AttentionTime = model.AttentionTime;
                        availableEscalationMetrix.DeviceId = model.DeviceId;
                        availableEscalationMetrix.EscalationLevel = escalationLevel;
                        availableEscalationMetrix.CreatedDate = DateTime.UtcNow.Ticks;
                        availableEscalationMetrix.ModifiedDate = DateTime.UtcNow.Ticks;
                        availableEscalationMetrix.OrganizationId = model.OrganizationId;
                        availableEscalationMetrix.ReceiverName = model.ReceiverName;
                        availableEscalationMetrix.ReceiverDescription = model.ReceiverDescription;
                        availableEscalationMetrix.ReceiverPhone = model.ReceiverPhone;
                        availableEscalationMetrix.SenderPhone = model.SenderPhone;
                        availableEscalationMetrix.SmokeStatus = (SmokeStatusEnum)Enum.Parse(typeof(SmokeStatusEnum), model.SmokeStatus);
                        availableEscalationMetrix.SmokeValue = (SmokeValueEnum)Enum.Parse(typeof(SmokeValueEnum), model.SmokeValue);

                        _escalationMetrixRepo.Update(availableEscalationMetrix);
                        _escalationMetrixRepo.SaveChanges();

                        transation.Commit();
                        _logger.LogInformation("Escalation Matrix Successfully Updated");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = "Escalation Successfully updated", SuccessMessage = "Escalation Successfully updated" });
                    }
                    catch (Exception e)
                    {
                        transation.Rollback();
                        _logger.LogError("Error Occured on Update Escalation Matrix on sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Update Escalation Matrix on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// delete escalation Matrix
        /// </summary>
        /// <param name="escalationMatrixId">escalation Matrix Id</param>
        /// <returns>it returns string success message if successfully deleted.</returns>
        [HttpDelete("deleteEscalationMatrix")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult DeleteEscalationMatrix([FromQuery]string escalationMatrixId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(escalationMatrixId))
                {
                    return BadRequest(new MMSHttpReponse<string>() {ErrorMessage = "Invalid Escalation Matrix Id." });
                }

                var availableEscalationMetrix = _escalationMetrixRepo.Query(z => z.EscalationMatrixId == escalationMatrixId).FirstOrDefault();

                if (availableEscalationMetrix == null)
                {
                    return NotFound(new MMSHttpReponse<string>() {ErrorMessage = "Escalation Matrix Not found." });
                }

                using (var transation = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        _escalationMetrixRepo.Remove(availableEscalationMetrix);
                        _escalationMetrixRepo.SaveChanges();

                        transation.Commit();
                        _logger.LogInformation("Escalation Matrix successfully deleted");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = "Escalation Successfully Deleted", SuccessMessage = "Escalation Successfully Deleted" });
                    }
                    catch (Exception e)
                    {
                        transation.Rollback();
                        _logger.LogError("Error Occured on Delete Escalation Matrix on sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Delete Escalation Matrix on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// get Escalation by Id
        /// </summary>
        /// <param name="escalationMatrixId">escalation matrix Id.</param>
        /// <returns>ite returns escalation model associated with this id.</returns>
        [HttpGet("getEscalationMatrixById")]
        [ProducesResponseType(typeof(MMSHttpReponse<EscalationMatrixModel>), StatusCodes.Status200OK)]
        public IActionResult GetEscalationMatrixById([FromQuery] string escalationMatrixId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(escalationMatrixId))
                {
                    return BadRequest(new MMSHttpReponse<EscalationMatrixModel>()
                    {
                        ErrorMessage = "Invalid Escalation Matrix Id."
                    });
                }

                var availableEscalationMetrix = _escalationMetrixRepo.Query(z => z.EscalationMatrixId == escalationMatrixId).FirstOrDefault();

                if (availableEscalationMetrix == null)
                {
                    return BadRequest(new MMSHttpReponse<EscalationMatrixModel>()
                    {
                        ErrorMessage = "Escalation Matrix Not found."
                    });
                }

                var respModel = new EscalationMatrixModel()
                {
                    EscalationMatrixId = availableEscalationMetrix.EscalationMatrixId,
                    AttentionTime = availableEscalationMetrix.AttentionTime,
                    DeviceId = availableEscalationMetrix.DeviceId,
                    EscalationLevel = availableEscalationMetrix.EscalationLevel.GetName(),
                    OrganizationId = availableEscalationMetrix.OrganizationId,
                    ReceiverDescription = availableEscalationMetrix.ReceiverDescription,
                    ReceiverName = availableEscalationMetrix.ReceiverName,
                    ReceiverPhone = availableEscalationMetrix.ReceiverPhone,
                    SenderPhone = availableEscalationMetrix.SenderPhone,
                    SmokeStatus = availableEscalationMetrix.SmokeStatus.GetName(),
                    SmokeValue = availableEscalationMetrix.SmokeValue.GetName(),
                };

                return Ok(new MMSHttpReponse<EscalationMatrixModel>() { ResponseBody = respModel });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on get Escalation Matrix by id on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// get escalation matrix paginated 
        /// </summary>
        /// <param name="req">this containes pageNo,perPageRecord,sortBy,sortOrder,organizationId(filter),deviceId(filter).</param>
        /// <returns>it returns escalation matrix paginated response.</returns>
        [HttpPost("getEscalationMatrixPaginated")]
        [ProducesResponseType(typeof(MMSHttpReponse<GetAllPaginateResponse<EscalationMatrixModel>>), StatusCodes.Status200OK)]
        public IActionResult GetEscalationMatrixPaginated([FromBody] GetAllEscalationPaginateReq req)
        {
            try
            {
                var allescalations = _escalationMetrixRepo.GetAll();

                if (!string.IsNullOrWhiteSpace(req.Search))
                {
                    allescalations = allescalations.Where(z => z.ReceiverName.Contains(req.Search)
                                                            || z.ReceiverDescription.Contains(req.Search));
                }

                if (string.IsNullOrWhiteSpace(req.SortBy))
                {
                    req.SortBy = "ModifiedDate";
                }

                if (req.OrganizationId != null && req.OrganizationId != 0)
                {
                    allescalations = allescalations.Where(z => z.OrganizationId == req.OrganizationId);
                }

                if (!string.IsNullOrWhiteSpace(req.DeviceId))
                {
                    allescalations = allescalations.Where(z => z.DeviceId == req.DeviceId);
                }

                var allCount = allescalations.Count();

                switch (req.SortBy.ToUpper())
                {
                    case "NAME":
                        {
                            allescalations = req.SortOrder == "asc" ? allescalations.OrderBy(z => z.ReceiverName) : allescalations.OrderByDescending(z => z.ReceiverName);
                            break;
                        }

                    case "DESCRIPTION":
                        {
                            allescalations = req.SortOrder == "asc" ? allescalations.OrderBy(z => z.ReceiverDescription) : allescalations.OrderByDescending(z => z.ReceiverDescription);
                            break;
                        }

                    case "ATTENTIONTIME":
                        {
                            allescalations = req.SortOrder == "asc" ? allescalations.OrderBy(z => z.AttentionTime) : allescalations.OrderByDescending(z => z.AttentionTime);
                            break;
                        }

                    default:
                        {
                            allescalations = allescalations.OrderByDescending(z => z.ModifiedDate);
                            break;
                        }
                }

                var data = allescalations.Skip(((req.PageNo - 1) * req.PageSize)).Take(req.PageSize).Select(availableEscalationMetrix => new EscalationMatrixModel()
                {
                    EscalationMatrixId = availableEscalationMetrix.EscalationMatrixId,
                    AttentionTime = availableEscalationMetrix.AttentionTime,
                    DeviceId = availableEscalationMetrix.DeviceId,
                    EscalationLevel = availableEscalationMetrix.EscalationLevel.GetName(),
                    OrganizationId = availableEscalationMetrix.OrganizationId,
                    ReceiverDescription = availableEscalationMetrix.ReceiverDescription,
                    ReceiverName = availableEscalationMetrix.ReceiverName,
                    ReceiverPhone = availableEscalationMetrix.ReceiverPhone,
                    SenderPhone = availableEscalationMetrix.SenderPhone,
                    SmokeStatus = availableEscalationMetrix.SmokeStatus.GetName(),
                    SmokeValue = availableEscalationMetrix.SmokeValue.GetName(),
                }).ToList();

                var resp = new MMSHttpReponse<GetAllPaginateResponse<EscalationMatrixModel>>()
                {
                    ResponseBody = new GetAllPaginateResponse<EscalationMatrixModel>()
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
                _logger.LogError("Error Occured on Get Escalation Matrix Paginate on sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// get escalation matrix Levels
        /// </summary>
        /// <returns>it returns display name and it's values</returns>
        [HttpGet("getAllEscalationLevels")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<GetAllDisplayResponse>>), StatusCodes.Status200OK)]
        public IActionResult GetAllEscalationLevels()
        {
            try
            {
                var AllEnumValues = Enum.GetValues(typeof(EscalationLevelEnum)).Cast<EscalationLevelEnum>().ToList();

                var response = AllEnumValues.Select(z => new GetAllDisplayResponse()
                {
                    DisplayName = z.GetDescription(),
                    Value = z.GetName()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Escalation Matrix Level {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// get all smoke status
        /// </summary>
        /// <returns>it returns display name and it's values</returns>
        [HttpGet("getAllSmockStatus")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<GetAllDisplayResponse>>), StatusCodes.Status200OK)]
        public IActionResult GetAllSmockStatus()
        {
            try
            {
                var AllEnumValues = Enum.GetValues(typeof(SmokeStatusEnum)).Cast<SmokeStatusEnum>().ToList();

                var response = AllEnumValues.Select(z => new GetAllDisplayResponse()
                {
                    DisplayName = z.GetDescription(),
                    Value = z.GetName()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Escalation Matrix Smock Status {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// get all smoke status
        /// </summary>
        /// <returns>it returns display name and it's values</returns>
        [HttpGet("getAllSmockValues")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<GetAllDisplayResponse>>), StatusCodes.Status200OK)]
        public IActionResult GetAllSmockValues()
        {
            try
            {
                var AllEnumValues = Enum.GetValues(typeof(SmokeValueEnum)).Cast<SmokeValueEnum>().ToList();

                var response = AllEnumValues.Select(z => new GetAllDisplayResponse()
                {
                    DisplayName = z.GetDescription(),
                    Value = z.GetName()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Escalation Matrix Smock Values {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
*/