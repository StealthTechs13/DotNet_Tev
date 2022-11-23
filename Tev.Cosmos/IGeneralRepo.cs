using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tev.Cosmos
{
    public interface IGeneralRepo
    {
        Task<List<(string DeviceId, int Count)>> GetUnacknowledgedAlerts(List<string> deviceIds, string orgId);
    }
}
