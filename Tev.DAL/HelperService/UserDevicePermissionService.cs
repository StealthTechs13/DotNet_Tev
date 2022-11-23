using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;

namespace Tev.DAL.HelperService
{
    public interface IUserDevicePermissionService
    {
        List<string> GetDeviceIdForViewer(string userEmail);
        List<string> GetDeviceIdForEditor(string userEmail);
        List<string> GetDeviceIdForOwner(string userEmail);
        List<UserDevicePermission> GetDeviceIdAndPermission(string userEmail, DevicePermissionEnum? permission = null);
        List<UserDevicePermission> GetDeviceIdAndPermissionByPermission(DevicePermissionEnum? permission = null);

    }

    public class UserDevicePermissionService : IUserDevicePermissionService
    {
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        public UserDevicePermissionService(IGenericRepo<UserDevicePermission> userDevicePermissionRepo)
        {
            _userDevicePermissionRepo = userDevicePermissionRepo;
        }

        /// <summary>
        /// it returns all deviceId that email has view permission
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        public List<string> GetDeviceIdForViewer(string userEmail)
        {
            return _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == userEmail.ToLower() && z.DevicePermission != DevicePermissionEnum.NoAccess).Select(z => z.DeviceId).ToList();
        }

        /// <summary>
        /// it returns deviceId and permissions
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        public List<UserDevicePermission> GetDeviceIdAndPermission(string userEmail, DevicePermissionEnum? permission = null)
        {
            if (permission == null)
            {
                return _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == userEmail.ToLower()).ToList();
            }
            else 
            {
                return _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == userEmail.ToLower() && z.DevicePermission == permission).ToList();
            }
        }

        /// <summary>
        /// it returns deviceId and permissions
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        public List<UserDevicePermission> GetDeviceIdAndPermissionByPermission(DevicePermissionEnum? permission = null)
        {
            if (permission == null)
            {
                return _userDevicePermissionRepo.GetAll().ToList();
            }
            else
            {
                return _userDevicePermissionRepo.Query(z => z.DevicePermission == permission).ToList();
            }
        }


        /// <summary>
        /// it returns all deviceId that email has edit permission
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        public List<string> GetDeviceIdForEditor(string userEmail)
        {
            return _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == userEmail.ToLower() 
            && z.DevicePermission == MMSConstants.DevicePermissionEnum.Editor).Select(z => z.DeviceId).ToList();
        }

        /// <summary>
        /// it returns all deviceId that email has owner permission
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        public List<string> GetDeviceIdForOwner(string userEmail)
        {
            return _userDevicePermissionRepo.Query(z => z.UserEmail.ToLower() == userEmail.ToLower() && z.DevicePermission == MMSConstants.DevicePermissionEnum.Owner).Select(z => z.DeviceId).ToList();
        }
    }
}
