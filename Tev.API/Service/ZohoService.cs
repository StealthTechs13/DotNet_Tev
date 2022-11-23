using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ZohoSubscription.Models;

namespace Tev.API.Service
{
    public class ZohoService:IZohoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ZohoService> _logger;

        public ZohoService(HttpClient httpClient, IConfiguration configuration, ILogger<ZohoService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<SubscriptionDetails> HostedPage(string token, string hostedpage_id, string deviceId)
        {
            this._httpClient.DefaultRequestHeaders.Add("Authorization", $"Zoho-oauthtoken {token}");

            var data = new SubscriptionDetails();

            var response = await this._httpClient.GetAsync("hostedpages/" + hostedpage_id).ConfigureAwait(false);


            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                data.SubscriptionId = Convert.ToString(JsonConvert.DeserializeObject<dynamic>(result).data.subscription.subscription_id);
                data.DeviceId = deviceId;
                data.Expiry = Convert.ToString(JsonConvert.DeserializeObject<dynamic>(result).data.subscription.next_billing_at);
                data.Status = Convert.ToString(JsonConvert.DeserializeObject<dynamic>(result).data.subscription.status);
                data.Amount = Convert.ToDouble(JsonConvert.DeserializeObject<dynamic>(result).data.subscription.sub_total);

                var addOns = JsonConvert.DeserializeObject<HostedPageResponse>(result).data.subscription.addons;
                var ret = new List<int>();

                addOns.ForEach(x => ret.Add(Helper.GetAlertType(x.addon_code)));

                data.Features = ret.ToArray();

            }
            else
            {
                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Error in ZohoService HostedPage method : -{0}", result);
            }
            return data;
        }
    }
}
