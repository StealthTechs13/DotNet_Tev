using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tev.Cosmos.IRepository;

namespace Tev.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TestController : ControllerBase
    {
        private readonly IDeviceRepo _cosmos;

        public TestController(IDeviceRepo cosmos)
        {
            _cosmos = cosmos;
        }

        [HttpGet]
        public async Task<IActionResult> GetDevice(string deviceId,string orgId)
        {
            var device = await _cosmos.GetDevice(deviceId,orgId);
            return Ok(device);
        }
        [HttpGet]
        public async Task<IActionResult> GetDeviceBySubscription(string subscriptionId)
        {
            return Ok(await _cosmos.GetDeviceBySubscription(subscriptionId));
        }
    }
}
