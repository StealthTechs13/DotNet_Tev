using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMSConstants;
using Tev.Cosmos.Entity;
using Tev.Cosmos.IRepository;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;
using Tev.IotHub.Models;

namespace Tev.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TevControllerBase : ControllerBase
    {
       

        protected string OrgId
        {
            get
            {
                return HttpContext.User.Claims.First(x => x.Type == MMSClaimTypes.OrgId).Value;
            }
        }


        protected string ZohoId
        {
            get
            {
                return HttpContext.User.Claims.First(x => x.Type == MMSClaimTypes.ZohoId).Value;
            }
        }

        /// <summary>
        /// get true if user is organization admin
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        protected bool IsOrgAdmin(Applications app)
        {
            var orgAdminClaimValue = Helpers.GetRoleName(Roles.OrgAdmin, app);
            return HttpContext.User.Claims.Any(x => x.Type == MMSClaimTypes.Role && x.Value.Contains("OrgAdmin"));
        }

        protected string UserEmail
        {
            get
            {
                return HttpContext.User.Claims.First(x => x.Type == MMSClaimTypes.Email).Value;
            }
        }

        protected Applications CurrentApplications
        {
            get
            {
                var app = HttpContext.User.Claims.First(x => x.Type == MMSClaimTypes.Application).Value;
                Enum.TryParse<Applications>(app, true, out var resp);
                return resp;
            }
        }

        protected async Task<bool> IsDeviceAuthorizedAsAdmin(string deviceId, IDeviceRepo cosmosRepo,IGenericRepo<UserDevicePermission> userDevicePermissionRepo)
        {
            if (IsOrgAdmin(Applications.TEV) || IsOrgAdmin(Applications.TEV2))
            {
                if(cosmosRepo != null)
                {
                    var result = await cosmosRepo.DeviceBelongsToOrg(deviceId,OrgId);
                    return result;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if(userDevicePermissionRepo != null)
                {
                    var permisison = userDevicePermissionRepo.Query(x => x.UserEmail == UserEmail && x.DeviceId == deviceId && x.DevicePermission == DevicePermissionEnum.Owner).FirstOrDefault();
                    return permisison != null ? true : false;
                }
                else
                {
                    return false;
                }
            }
        }

        protected  bool IsDeviceAuthorizedAsAdmin(Device device, IGenericRepo<UserDevicePermission> userDevicePermissionRepo)
        {
            if (IsOrgAdmin(Applications.TEV) || IsOrgAdmin(Applications.TEV2))
            {
                if (device.OrgId == OrgId)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (userDevicePermissionRepo != null)
                {
                    var permisison = userDevicePermissionRepo.Query(x => x.UserEmail == UserEmail && x.DeviceId == device.LogicalDeviceId 
                    && x.DevicePermission == DevicePermissionEnum.Owner).FirstOrDefault();
                    return permisison != null ? true : false;
                }
                else
                {
                    return false;
                }
            }
        }


    }
}
