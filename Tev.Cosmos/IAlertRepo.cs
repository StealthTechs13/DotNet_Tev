using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tev.Cosmos
{
    public interface IAlertRepo
    {
        /// <summary>
        /// Get a list of all the alerts belonging to the org and based on what the logged in user is supposed to see
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="take"></param>
        /// <param name="skip"></param>
        /// <param name="alertType"></param>
        /// <param name="acknowledged"></param>
        /// <returns></returns>
        Task<List<Alert>> GetAlertsByOrg(string orgId, int take, int skip, List<int> alertType = null, bool? acknowledged = null,
        bool? isBookMarked = null, bool? isCorrect = null, long? startDate = null, long? endDate = null, string deviceType = null);
        Task<List<Alert>> GetAlertsByLocation(List<string> locationIds, string orgId, int take, int skip, List<int> alertType = null,
        bool? acknowledged = null, bool? isBookMarked = null, bool? isCorrect = null, long? startDate = null, long? endDate = null);

        Task<List<Alert>> GetAlertsByDevice(List<string> deviceIds, string orgId, int take, int skip, List<int> alertType = null,
        bool? acknowledged = null, bool? isBookMarked = null, bool? isCorrect = null, long? startDate = null, long? endDate = null, string deviceType = null);

        Task ReportIncorrect(string alertId, string comment, string orgId);

        Task AcknowledgeAlert(string alertId, string orgId, bool acknowledge);

        Task Bookmark(string alertId, string orgId, bool bookmark);

        Task Delete(string alertId, string orgId);

        Task UpdateDefaultDeviceName(string deviceId, string orgId, string deviceName, string locationName);
        Task <string> GetDeviceIdOfAlert(string alertId, string orgId);
        Task getAlerts(string deviceId, string orgId);
    }
}
