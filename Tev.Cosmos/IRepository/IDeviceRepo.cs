using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos.IRepository
{
    public interface IDeviceRepo
    {
        Task<Device> GetDevice(string logicalDeviceId, string orgId);
        Task<List<Device>> GetDevicesByLocation(string locationId, string orgId);
        Task UpdateDevice(string orgId, Device device);
        Task<List<Device>> GetDeviceByOrgId(string orgId);
        Task<List<Device>> GetTev2DeviceByDeviceIds(List<string> logicalDeviceIds, string orgId);
        Task<List<Device>> GetTev2DeviceByOrgId(string orgId);
        Task<List<Device>> GetDeviceByDeviceIds(List<string> logicalDeviceIds, string orgId);
        Task DeleteDevice(string logicalDeviceId, string orgId);
        Task<bool> DeviceBelongsToOrg(string logicalDeviceId, string orgId);
        Task<FeatureConfig> GetDeviceFeatureConfiguration(string logicalDeviceId, string orgId);
        Task<Device> GetDeviceBySubscription(string subscriptionId);
        Task<string> GetLatestFirmwareVersion(string deviceType);
    }
}
