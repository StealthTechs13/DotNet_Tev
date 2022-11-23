using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos
{
    public interface IDeviceSetupRepo
    {
        Task<DeviceSetup> GetDeviceSetupStatus(string logicalDeviceId, string orgId);
    }
}

