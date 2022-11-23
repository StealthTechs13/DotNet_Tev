using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tev.Cosmos
{
    public class AlertRepo : IAlertRepo
    {
        private readonly ICosmosDbService<Alert> _cosmosClient;

        public AlertRepo(ICosmosDbService<Alert> cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }
       
        public async Task<List<Alert>> GetAlertsByOrg(string orgId, int take, int skip, List<int> alertType = null, bool? acknowledged = null,
            bool? isBookMarked = null,bool? isCorrect=null, long? startDate=null, long? endDate=null, string deviceType=null)
        {
            var queryText = new StringBuilder();
            queryText.Append("select * from c where c.orgId=@orgId");

            if(startDate.HasValue && endDate.HasValue)
            {
                queryText.Append(" and (c.occurenceTimestamp between @startDate and @endDate)");
            }
            var subQuery = new List<string>();
            if (acknowledged.HasValue)
            {
                subQuery.Add("c.acknowledged=@acknowledged");
            }
            if (isBookMarked.HasValue)
            {
                subQuery.Add("c.isBookmarked=@isBookmarked");
            }
            if (isCorrect.HasValue)
            {
                subQuery.Add("c.isCorrect=@isCorrect");
            }

            if (subQuery.Count == 1)
            {
                queryText.Append($" and {subQuery[0]}");
            }
            else if(subQuery.Count>1)
            {
                queryText.Append(" and (");
                var tempStr = string.Join(" or ", subQuery);
                queryText.Append(tempStr);
                queryText.Append(")");
            }
            

            if (alertType != null)
            {
                queryText.Append(" and ARRAY_CONTAINS(@alertType,c.alertType)");
            }
            queryText.Append(" and c.telemetryType='violation'");
            queryText.Append(" and c.alertType <> 2 ");
            queryText.Append(" and (NOT is_defined(c.isDeleted) OR c.isDeleted = false OR ISNULL(c.isDeleted ) = true) ");

            if (!string.IsNullOrEmpty(deviceType))
            {
                if (deviceType == "TEV")
                {
                    queryText.Append("and (NOT IS_DEFINED(c.deviceType) or c.deviceType = 'TEV')");
                }
                else
                {
                    queryText.Append($" and c.deviceType='{deviceType}'");
                }
            }
            queryText.Append(" order by c.occurenceTimestamp desc offset @skip limit @take");
            QueryDefinition queryDef = new QueryDefinition(queryText.ToString())
                                            .WithParameter("@orgId", orgId)
                                            .WithParameter("@alertType", alertType)
                                            .WithParameter("@acknowledged", acknowledged)
                                            .WithParameter("@isBookmarked", isBookMarked)
                                            .WithParameter("@isCorrect", isCorrect)
                                            .WithParameter("@take", take)
                                            .WithParameter("@skip", skip)
                                            .WithParameter("@startDate", startDate)
                                            .WithParameter("@endDate", endDate);
            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return (List<Alert>)result;
        }

        public async Task<List<Alert>> GetAlertsByLocation(List<string> locationIds, string orgId, int take, int skip, List<int> alertType = null, 
            bool? acknowledged = null, bool? isBookMarked = null, bool? isCorrect = null, long? startDate = null, long? endDate = null)
        {
            var queryText = new StringBuilder();
            queryText.Append("select * from c where c.orgId=@orgId and ARRAY_CONTAINS(@locationIds,c.locationId)");
            if (startDate.HasValue && endDate.HasValue)
            {
                queryText.Append(" and (c.occurenceTimestamp between @startDate and @endDate)");
            }
            var subQuery = new List<string>();
            if (acknowledged.HasValue)
            {
                subQuery.Add("c.acknowledged=@acknowledged");
            }
            if (isBookMarked.HasValue)
            {
                subQuery.Add("c.isBookmarked=@isBookmarked");
            }
            if (isCorrect.HasValue)
            {
                subQuery.Add("c.isCorrect=@isCorrect");
            }

            if (subQuery.Count == 1)
            {
                queryText.Append($" and {subQuery[0]}");
            }
            else if (subQuery.Count > 1)
            {
                queryText.Append(" and (");
                var tempStr = string.Join(" or ", subQuery);
                queryText.Append(tempStr);
                queryText.Append(")");
            }

            if (alertType != null)
            {
                queryText.Append(" and ARRAY_CONTAINS(@alertType,c.alertType)");
            }
            queryText.Append(" and c.telemetryType='violation'");
            queryText.Append(" and c.alertType <> 2 ");
            queryText.Append(" and (NOT is_defined(c.isDeleted) OR c.isDeleted = false OR ISNULL(c.isDeleted ) = true) ");
            queryText.Append(" order by c.occurenceTimestamp desc offset @skip limit @take");
            QueryDefinition queryDef = new QueryDefinition(queryText.ToString())
                                            .WithParameter("@orgId", orgId)
                                            .WithParameter("@locationIds", locationIds)
                                            .WithParameter("@alertType", alertType)
                                            .WithParameter("@acknowledged", acknowledged)
                                            .WithParameter("@isBookmarked", isBookMarked)
                                            .WithParameter("@isCorrect", isCorrect)
                                            .WithParameter("@take", take)
                                            .WithParameter("@skip", skip)
                                            .WithParameter("@startDate", startDate)
                                            .WithParameter("@endDate", endDate);

            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return (List<Alert>)result;
        }

        public async Task<List<Alert>> GetAlertsByDevice(List<string> deviceIds, string orgId, int take, int skip, List<int> alertType = null, 
            bool? acknowledged = null, bool? isBookMarked = null, bool? isCorrect = null, long? startDate = null, long? endDate = null,string deviceType = null)
        {
            var queryText = new StringBuilder();
            queryText.Append("select * from c where c.orgId=@orgId and ARRAY_CONTAINS(@deviceIds,c.deviceId)");
            if (startDate.HasValue && endDate.HasValue)
            {
                queryText.Append(" and (c.occurenceTimestamp between @startDate and @endDate)");
            }
            var subQuery = new List<string>();
            if (acknowledged.HasValue)
            {
                subQuery.Add("c.acknowledged=@acknowledged");
            }
            if (isBookMarked.HasValue)
            {
                subQuery.Add("c.isBookmarked=@isBookmarked");
            }
            if (isCorrect.HasValue)
            {
                subQuery.Add("c.isCorrect=@isCorrect");
            }

            if (subQuery.Count == 1)
            {
                queryText.Append($" and {subQuery[0]}");
            }
            else if (subQuery.Count > 1)
            {
                queryText.Append(" and (");
                var tempStr = string.Join(" or ", subQuery);
                queryText.Append(tempStr);
                queryText.Append(")");
            }

            if (alertType != null)
            {
                queryText.Append(" and ARRAY_CONTAINS(@alertType,c.alertType)");
            }
            queryText.Append(" and c.telemetryType='violation'");
            queryText.Append(" and c.alertType <> 2 ");
            queryText.Append(" and (NOT is_defined(c.isDeleted) OR c.isDeleted = false OR ISNULL(c.isDeleted ) = true) ");

            if (!string.IsNullOrEmpty(deviceType))
            {
                if (deviceType == "TEV")
                {
                    queryText.Append("and (NOT IS_DEFINED(c.deviceType) or c.deviceType = 'TEV')");
                }
                else
                {
                    queryText.Append($" and c.deviceType='{deviceType}'");
                }
            }
            

            queryText.Append(" order by c.occurenceTimestamp desc offset @skip limit @take");
            QueryDefinition queryDef = new QueryDefinition(queryText.ToString())
                                            .WithParameter("@orgId", orgId)
                                            .WithParameter("@deviceIds", deviceIds)
                                            .WithParameter("@alertType", alertType)
                                            .WithParameter("@acknowledged", acknowledged)
                                            .WithParameter("@isBookmarked", isBookMarked)
                                            .WithParameter("@isCorrect", isCorrect)
                                            .WithParameter("@take", take)
                                            .WithParameter("@skip", skip)
                                            .WithParameter("@startDate", startDate)
                                            .WithParameter("@endDate", endDate);

            var result = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            return (List<Alert>)result;
        }

        public async Task ReportIncorrect(string alertId, string comment, string orgId)
        {
            var alert = await _cosmosClient.GetItemAsync(alertId, orgId).ConfigureAwait(false);
            alert.Comment = comment;
            alert.IsCorrect = false;
            alert.Acknowledged = true;
            await _cosmosClient.UpdateItemAsync(alert, orgId).ConfigureAwait(false);
        }

        public async Task Bookmark(string alertId, string orgId, bool bookmark)
        {
            var alert = await _cosmosClient.GetItemAsync(alertId, orgId).ConfigureAwait(false);
            alert.IsBookmarked = bookmark;
            await _cosmosClient.UpdateItemAsync(alert, orgId).ConfigureAwait(false);
        }

        public async Task AcknowledgeAlert(string alertId, string orgId, bool acknowledge)
        {
            var alert = await _cosmosClient.GetItemAsync(alertId, orgId).ConfigureAwait(false);
            alert.Acknowledged = acknowledge;
            await _cosmosClient.UpdateItemAsync(alert, orgId).ConfigureAwait(false);
        }

        public async Task Delete(string alertId, string orgId)
        {
            var alert = await _cosmosClient.GetItemAsync(alertId, orgId).ConfigureAwait(false);
            if(alert != null)
            {
                alert.IsDeleted = true;
                await _cosmosClient.UpdateItemAsync(alert, orgId).ConfigureAwait(false);
            }
        }

        public async Task UpdateDefaultDeviceName(string deviceId, string orgId , string deviceName , string locationName)
        {
            var queryText = new StringBuilder();
            queryText.Append("SELECT * FROM c where c.deviceId = '"+ deviceId + "' and c.deviceName = 'Smart AI Camera' and c.alertType = 51  order by c.occurenceTimestamp desc offset 0 limit 1");
            QueryDefinition queryDef = new QueryDefinition(queryText.ToString());
            var alert = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);
            if(alert != null)
            {
                var updated = alert.FirstOrDefault();
                if (updated != null)
                {
                    if (updated.DeviceName == "Smart AI Camera")
                    {
                        updated.DeviceName = deviceName;
                        updated.LocationName = locationName;
                        await _cosmosClient.UpdateItemAsync(updated, orgId).ConfigureAwait(false);
                    }

                }
            }
           
        }

        public async Task<string> GetDeviceIdOfAlert(string alertId, string orgId)
        {
            var alert = await _cosmosClient.GetItemAsync(alertId, orgId).ConfigureAwait(false);
            return alert.DeviceId;
        }

        public async Task getAlerts(string deviceId, string orgId)
        {
            var queryText = new StringBuilder();
            queryText.Append($"select * from c where c.DeviceId = '{deviceId}'"); ;
            QueryDefinition queryDef = new QueryDefinition(queryText.ToString());
            var alert = await _cosmosClient.GetItemsAsync(queryDef, orgId).ConfigureAwait(false);

        }



    }
}
