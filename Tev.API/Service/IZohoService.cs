using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZohoSubscription.Models;

namespace Tev.API.Service
{
    public interface IZohoService
    {
        Task<SubscriptionDetails> HostedPage(string token, string hostedpage_id, string deviceId);
    }
}
