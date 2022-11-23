using MMSConstants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Configuration
{
    public static class RequestRateLimitTEVConfig
    {
        public static string GetRateLimitConfig()
        {
            var ratelimtConfig = new RequestRateLimit
            {
                EnableEndpointRateLimiting = true,
                StackBlockedRequests = false,
                RealIpHeader = "X-Real-IP",
                ClientIdHeader = "X-ClientId",
                HttpStatusCode = 429,
                GeneralRules = new List<GeneralRules> {
                    new GeneralRules
                    {
                        Endpoint = "*:/api/*",
                        Period = "60s",
                        Limit = 60,
                    }
                }
            };

            return JsonConvert.SerializeObject(ratelimtConfig);
        }
    }
}
