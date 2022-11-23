using System.Collections.Generic;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos
{
    public interface IWSDSummaryAlertRepo
    {
        Task<List<WSDSummaryEntity>> GetDeviceSummaryAlerts(string orgId, string deviceId);
    }
}
