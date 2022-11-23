using Gremlin.Net.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tev.Gremlin
{
    public interface IDeviceDb
    {
        Task<ResultSet<dynamic>> AddDevice(string logicalDeviceId, string siteId, string orgId);
        Task<ResultSet<dynamic>> GetDevices(string parentSite, bool recursive = false);
    }
}
