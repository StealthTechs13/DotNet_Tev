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
using Tev.Cosmos;
using Tev.Cosmos.IRepository;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class UserDevicePermissionController : TevControllerBase
    {
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly ILogger<UserDevicePermission> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITevIoTRegistry _iotHub;
        private readonly IDeviceRepo _deviceRepo;
        public UserDevicePermissionController(IGenericRepo<UserDevicePermission> userDevicePermissionRepo, ILogger<UserDevicePermission> logger, 
            IUnitOfWork unitOfWork, ITevIoTRegistry iotHub, IDeviceRepo deviceRepo)
        {
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _iotHub = iotHub;
            _deviceRepo = deviceRepo;
        }

        /// <summary>
        /// Create User Device Permission
        /// </summary>
        /// <param name="model">model contains deviceId,Username and permissions</param>
        /// <returns></returns>
        [HttpPost("createUserDevicePermission")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateUserDevicePermission([FromBody] UserDevicePermissionModel model)
        {
            try
            {
                if(model == null)
                {
                    return BadRequest();
                }
                if (!IsOrgAdmin(CurrentApplications))
                {
                    return Forbid();
                }

                if (!model.DevicePermission.Any())
                {
                    return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "No Device Found for Permission creation."});
                }

                var allDeviceIds = model.DevicePermission.Select(z => z.DeviceId).ToList();

                var alreadyAvailable = _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == model.UserEmail.ToLower() && allDeviceIds.Contains(z.DeviceId));

                if (alreadyAvailable.Any())
                {
                    return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "This user already has permission for the device." });
                }

                var notParsablePermissions = new List<string>();
                model.DevicePermission.ForEach(per =>
                {
                    var validPer = Enum.TryParse<DevicePermissionEnum>(per.Permission, out var outper);

                    if (!validPer)
                    {
                        notParsablePermissions.Add(per.Permission);
                    }
                });

                if (notParsablePermissions.Any())
                {
                    return BadRequest(new MMSHttpReponse<string>() { 
                        ErrorMessage = $"There are permission ' {string.Join(", ", notParsablePermissions) } ' that not recognized for our system."});
                }


                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        var dbModels = model.DevicePermission.Select(z => new UserDevicePermission()
                        {
                            CreatedBy = UserEmail,
                            UserEmail = model.UserEmail,
                            DeviceId = z.DeviceId,
                            DevicePermission = Enum.Parse<DevicePermissionEnum>(z.Permission),
                            DeviceType = CurrentApplications,
                            ModifiedBy = UserEmail,
                            UserDevicePermissionId = Guid.NewGuid().ToString(),
                        }).ToList();

                        await _userDevicePermissionRepo.AddRangeAsync(dbModels);
                        _userDevicePermissionRepo.SaveChanges();
                        transaction.Commit();
                        _logger.LogInformation("User Device permission successfully added on Sql");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = "User Device Permission successfully Added", 
                            SuccessMessage = "User Device Permission successfully Added" });
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _logger.LogError("Error Occured on Create User Device Permission on Sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Create User Device Permission on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update User Device Permissions
        /// </summary>
        /// <param name="model">model contains deviceId,Username and permissions</param>
        /// <returns></returns>
        [HttpPost("updateUserDevicePermission")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public  IActionResult UpdateUserDevicePermission([FromBody] UserDevicePermissionModel model)
        {
            try
            {
                if (!IsOrgAdmin(CurrentApplications))
                {
                    return Forbid();
                }

                if (!model.DevicePermission.Any())
                {
                    return NotFound(new MMSHttpReponse<string>() { ErrorMessage = "No Device Found for Permission creation." });
                }

                var allDeviceIds = model.DevicePermission.Select(z => z.DeviceId).ToList();

                var notParsablePermissions = new List<string>();
                model.DevicePermission.ForEach(per =>
                {
                    var validPer = Enum.TryParse<DevicePermissionEnum>(per.Permission, out var outper);

                    if (!validPer)
                    {
                        notParsablePermissions.Add(per.Permission);
                    }
                });

                if (notParsablePermissions.Any())
                {
                    return BadRequest(new MMSHttpReponse<string>() { 
                        ErrorMessage = $"There are permission ' {string.Join(", ", notParsablePermissions) } ' that not recognized for our system."});
                }

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        //permission For Update
                        var permissionForUpdate = model.DevicePermission.Where(z => z.Permission.ToLower() != DevicePermissionEnum.NoAccess.GetName().ToLower()).ToList();
                        foreach (var modelPermission in permissionForUpdate)
                        {

                            var alreadyAvailable = _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == model.UserEmail.ToLower() && 
                            z.DeviceId == modelPermission.DeviceId && z.UserDevicePermissionId != modelPermission.UserDevicePermissionId);

                            if (alreadyAvailable.Any())
                            {
                                transaction.Rollback();
                                return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "There are some devices that already available Permission Assigned for this User."});
                            }

                            var availablePermission = _userDevicePermissionRepo.Query(z => z.UserDevicePermissionId == modelPermission.UserDevicePermissionId).FirstOrDefault();

                            if (availablePermission == null)
                            {
                                transaction.Rollback();
                                return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = $"User Permission not Found For Update."});
                            }


                            availablePermission.UserEmail = model.UserEmail;
                            availablePermission.DeviceId = modelPermission.DeviceId;
                            availablePermission.DevicePermission = Enum.Parse<DevicePermissionEnum>(modelPermission.Permission);
                            availablePermission.DeviceType = CurrentApplications;
                            availablePermission.ModifiedBy = UserEmail;
                            _userDevicePermissionRepo.Update(availablePermission);
                            _userDevicePermissionRepo.SaveChanges();
                        }

                        //permission For Delete
                        var permissionForDelete = model.DevicePermission.Where(z => z.Permission.ToLower() == DevicePermissionEnum.NoAccess.GetName().ToLower()).ToList();
                        foreach (var modelPermission in permissionForDelete)
                        {
                            var availablePermission = _userDevicePermissionRepo.Query(z => z.UserDevicePermissionId == modelPermission.UserDevicePermissionId).FirstOrDefault();

                            if (availablePermission == null)
                            {
                                transaction.Rollback();
                                return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = $"User Permission not Found For Update."});
                            }
                            _userDevicePermissionRepo.Remove(availablePermission);
                            _userDevicePermissionRepo.SaveChanges();
                        }


                        transaction.Commit();
                        _logger.LogInformation("User Device permission successfully updated on Sql");
                        return Ok(new MMSHttpReponse<string>() { ResponseBody = "User Device Permission successfully Updated", 
                            SuccessMessage = "User Device Permission successfully Updated" });
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _logger.LogError("Error Occured on Create User Device Permission on Sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Update User Device Permission on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// it returns all Device permission by user email
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        [HttpGet("getUserDevicePermissionByUserId")]
        [ProducesResponseType(typeof(MMSHttpReponse<UserDevicePermissionModel>), StatusCodes.Status200OK)]
        public IActionResult GetUserDevicePermissionByUserEmail([FromQuery] string userEmail)
        {
            try
            {
                if (!IsOrgAdmin(CurrentApplications))
                {
                    return Forbid();
                }
                var permissions = _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == userEmail.ToLower()).Select(z => new DevicePermissionModel()
                {
                    DeviceId = z.DeviceId,
                    Permission = z.DevicePermission.GetName(),
                    UserDevicePermissionId = z.UserDevicePermissionId
                }).ToList();

                var respModel = new UserDevicePermissionModel()
                {
                    DevicePermission = permissions,
                    UserEmail = userEmail
                };
                return Ok(new MMSHttpReponse<UserDevicePermissionModel>() { ResponseBody = respModel });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get User Device Permission By Id on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// it returns all Device permission by user email
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("getDevicePermissionByUserEmails")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<UserDevicePermissionListModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevicePermissionByUserEmails([FromBody] GetUserDevicePermissionReq model)
        {
            try
            {
                if(model == null)
                {
                    return BadRequest();
                }

                if (!IsOrgAdmin(CurrentApplications))
                {
                    return Forbid();
                }

                var allEmails = model.UserEmails.Select(z => z.ToLower()).ToList();

                var permissions = _userDevicePermissionRepo.Query(z => allEmails.Contains(z.UserEmail.ToLower())).Select(z => new 
                {
                    DeviceId = z.DeviceId,
                    Permission = z.DevicePermission.GetName(),
                    UserDevicePermissionId = z.UserDevicePermissionId,
                    Email = z.UserEmail
                }).ToList();

                if (permissions.Count == 0)
                {
                    return Ok(new MMSHttpReponse<List<UserDevicePermissionListModel>> { ResponseBody = new List<UserDevicePermissionListModel>() });
                }
                var allDeviceIds = permissions.Select(z => z.DeviceId).ToList();

                var allDevices = await _deviceRepo.GetDeviceByDeviceIds(allDeviceIds, OrgId);


                var groupByPermission = permissions.GroupBy(z => z.Email).Select(z => new UserDevicePermissionListModel()
                {
                    UserEmail = z.Key,
                    DevicePermission = z.Select(q => new DevicePermissionListModel()
                    {
                        UserDevicePermissionId = q.UserDevicePermissionId,
                        DeviceId = q.DeviceId,
                        Permission = q.Permission,
                        DeviceName = allDevices.Where(z=>z.LogicalDeviceId == q.DeviceId).Select(z=>z.DeviceName).FirstOrDefault()
                    }).ToList()
                }).ToList();

                return Ok(new MMSHttpReponse<List<UserDevicePermissionListModel>>() { ResponseBody = groupByPermission });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get User Device Permission By Id on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }


        /// <summary>
        /// Delete User permission By id
        /// </summary>
        /// <param name="userDevicePermissionId">user device permission Id</param>
        /// <returns></returns>
        [HttpDelete("deleteUserPermissionById")]
        [ProducesResponseType(typeof(MMSHttpReponse<string>), StatusCodes.Status200OK)]
        public IActionResult DeleteUserPermissionById([FromQuery] string userDevicePermissionId)
        {
            try
            {
                if(userDevicePermissionId == null)
                {
                    return BadRequest(new MMSHttpReponse { ErrorMessage = "User device permission id is required" });
                }
                if (!IsOrgAdmin(CurrentApplications))
                {
                    return Forbid();
                }

                var userPermissionDevice = _userDevicePermissionRepo.Query(z => z.UserDevicePermissionId == userDevicePermissionId).FirstOrDefault();

                if (userPermissionDevice == null)
                {
                    return BadRequest(new MMSHttpReponse<string>() { ErrorMessage = "User device Permission not found."});
                }

                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        _userDevicePermissionRepo.Remove(userPermissionDevice);
                        _userDevicePermissionRepo.SaveChanges();

                        transaction.Commit();
                        return Ok(new MMSHttpReponse<string>() { ResponseBody= "User Device Permission successfully deleted",
                            SuccessMessage = "User Device Permission successfully deleted" });
                    }
                    catch (Exception e) 
                    {
                        transaction.Rollback();
                        _logger.LogError("Error Occured on Delete User Device Permission By Id on Sql {exception}", e);
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Delete User Device Permission By Id on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get Device Permissions
        /// </summary>
        /// <returns></returns>
        [HttpGet("getDevicePermissions")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<GetAllDisplayResponse>>), StatusCodes.Status200OK)]
        public IActionResult GetDevicePermissions()
        {
            try
            {
                var allDevicePermissions = Enum.GetValues(typeof(DevicePermissionEnum)).Cast<DevicePermissionEnum>();

                var resp = allDevicePermissions.Select(z => new GetAllDisplayResponse()
                {
                    DisplayName = z.GetName(),
                    Value = z.GetName()
                }).ToList();

                return Ok(new MMSHttpReponse<List<GetAllDisplayResponse>>() { ResponseBody = resp });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured on Get Device Permissions on Sql {exception}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }


    }
}
