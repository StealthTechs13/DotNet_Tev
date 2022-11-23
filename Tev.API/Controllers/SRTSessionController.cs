using Azure.Storage.Queues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tev.API.Models;
using Tev.Cosmos.IRepository;
using Tev.DAL;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;
using Tev.IotHub;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Tev.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "IMPACT")]
    public class SRTSessionController : TevControllerBase
    {
        private readonly IGenericRepo<SrtSessionDetail> _srtSessionRepo;
        private readonly IGenericRepo<SRTRoutes> _srtRoutesRepo;
        private readonly ILogger<UserDevicePermission> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITevIoTRegistry _iotHub;
        private readonly IDeviceRepo _deviceRepo;
        private readonly IConfiguration _configuration;
        private readonly IGenericRepo<UserDevicePermission> _userDevicePermissionRepo;
        private readonly IGenericRepo<DeviceStreamingTypeManagement> _deviceStreamingTypeManagementRepo;
        private readonly IGenericRepo<SDCardHistory> _cardHistoryRepo;
        public SRTSessionController(IConfiguration configuration, IGenericRepo<SrtSessionDetail> srtSessionRepo, ILogger<UserDevicePermission> logger, IUnitOfWork unitOfWork, ITevIoTRegistry iotHub, IDeviceRepo deviceRepo, IGenericRepo<SRTRoutes> srtRoutesRepo, IGenericRepo<UserDevicePermission> userDevicePermissionRepo
            , IGenericRepo<DeviceStreamingTypeManagement> deviceStreamingTypeManagement, IGenericRepo<SDCardHistory> cardHistoryRepo)
        {
            _srtSessionRepo = srtSessionRepo;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _iotHub = iotHub;
            _srtRoutesRepo = srtRoutesRepo;
            _deviceRepo = deviceRepo;
            _userDevicePermissionRepo = userDevicePermissionRepo;
            _configuration = configuration;
            _deviceStreamingTypeManagementRepo = deviceStreamingTypeManagement;
            _cardHistoryRepo = cardHistoryRepo;
        }



        /// <summary>
        /// SRT Authentication
        /// </summary>
        /// <returns></returns>

        [HttpGet("getSessionId")]
        [ProducesResponseType(typeof(MMSHttpReponse<SrtSessionDetail>), StatusCodes.Status200OK)]
        public async Task<string> getSessionId()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            CookieContainer cookieContainer = new CookieContainer();
            SRTAuthResponse srtAuthResponse = new SRTAuthResponse();
            try
            {

                string srtServerIP = _configuration.GetSection("SRTServerCredentials").GetSection("serverIP").Value;
                string username = _configuration.GetSection("SRTServerCredentials").GetSection("username").Value;
                string password = _configuration.GetSection("SRTServerCredentials").GetSection("password").Value;

                string sessionId = "";
                string gatewayUrl = "https://" + srtServerIP + "/api";
                var srtAuthentication = new SRTAuthentication
                {
                    username = username,
                    password = password,
                };
                var chkSession = _srtSessionRepo.GetAll().FirstOrDefault();
                if (chkSession != null)
                {
                    DateTimeOffset sessionTime = DateTimeOffset.FromUnixTimeMilliseconds(chkSession.expireAt);
                    DateTime datTime = sessionTime.DateTime;
                    if (datTime < DateTime.UtcNow)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                        var client = new HttpClient(clientHandler);
                        var srtAuth = System.Text.Json.JsonSerializer.Serialize(srtAuthentication);
                        var requestContent = new StringContent(srtAuth, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(gatewayUrl + "/session", requestContent);
                        string responseMessage = response.Content.ReadAsStringAsync().Result;
                        var result = System.Text.Json.JsonSerializer.Deserialize<SRTAuthResponse>(responseMessage);

                        cookieContainer.Add(new Uri(gatewayUrl + "/session"), new Cookie("sessionID", result.response.sessionID));
                        var sesResponse = await client.GetAsync(gatewayUrl + "/session");
                        string sesMessage = sesResponse.Content.ReadAsStringAsync().Result;
                        var sesResult = System.Text.Json.JsonSerializer.Deserialize<SRTSessionDetailResponse>(sesMessage);
                        chkSession.sessionID = sesResult.sessionID;
                        chkSession.expireAt = sesResult.expireAt;
                        chkSession.isLicensed = sesResult.isLicensed;
                        sessionId = sesResult.sessionID;
                        _srtSessionRepo.SaveChanges();
                    }
                    else
                    {
                        sessionId = chkSession.sessionID;
                    }
                }
                else
                {
                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                    var client = new HttpClient(clientHandler);
                    var srtAuth = System.Text.Json.JsonSerializer.Serialize(srtAuthentication);
                    var requestContent = new StringContent(srtAuth, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(gatewayUrl + "/session", requestContent);
                    string responseMessage = response.Content.ReadAsStringAsync().Result;
                    var result = System.Text.Json.JsonSerializer.Deserialize<SRTAuthResponse>(responseMessage);

                    cookieContainer.Add(new Uri(gatewayUrl + "/session"), new Cookie("sessionID", result.response.sessionID));
                    var sesResponse = await client.GetAsync(gatewayUrl + "/session");
                    string sesMessage = sesResponse.Content.ReadAsStringAsync().Result;
                    var sesResult = System.Text.Json.JsonSerializer.Deserialize<SRTSessionDetailResponse>(sesMessage);
                    using (var transaction = _unitOfWork.BeginTransaction())
                    {
                        try
                        {
                            SrtSessionDetail srtSessionDetail = new SrtSessionDetail()
                            {
                                sessionID = sesResult.sessionID,
                                displayName = sesResult.displayName,
                                startAt = sesResult.startAt,
                                expireAt = sesResult.expireAt,
                                isLicensed = sesResult.isLicensed,
                                CreatedBy = sesResult.email,
                                CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                ModifiedBy = sesResult.email,
                                Id = Guid.NewGuid().ToString(),
                            };
                            sessionId = sesResult.sessionID;
                            await _srtSessionRepo.AddAsync(srtSessionDetail);
                            _srtSessionRepo.SaveChanges();
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError("Error Occured while creating SrtSession {exception}", ex);
                            return ex.Message;
                        }
                    }
                }
                return sessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occured while connecting to SRT Server {exception}", ex);
                return ex.Message;
            }
        }

        /// <summary>
        /// Return device primary user details
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("GetDevicePrimaryUserDetails")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDevicePrimaryUserDetails([FromHeader(Name = "uid")] string uid, String deviceId)
        {
            StreamResponse streamResponse = new StreamResponse();
            String requestedUserTokenId = String.Empty;

            try
            {
                requestedUserTokenId = Request.Headers.FirstOrDefault(x => x.Key == "uid").Value.FirstOrDefault();
                if (requestedUserTokenId == null)
                {
                    streamResponse.Status = false;
                    streamResponse.Message = _configuration.GetValue<string>("UidFieldErrorMessage");
                    return NotFound(streamResponse);
                }
                var activeDeviceStreamingTypeList = _deviceStreamingTypeManagementRepo.GetAll()
                                                .Where(e => e.LogicalDeviceId == deviceId && e.IsUserStreaming == true)
                                                .ToList<DeviceStreamingTypeManagement>();

                if (activeDeviceStreamingTypeList != null && activeDeviceStreamingTypeList.Count > 0)
                {
                    streamResponse.PrimaryUserDeviceId = activeDeviceStreamingTypeList.OrderBy(e => e.Id).FirstOrDefault().LogicalDeviceId;
                    streamResponse.PrimaryUserTokenId = activeDeviceStreamingTypeList.OrderBy(e => e.Id).FirstOrDefault().UserTokenId;
                    streamResponse.PrimaryUserLiveStreamingState = activeDeviceStreamingTypeList.OrderBy(e => e.Id).FirstOrDefault().LiveStreamingActive;
                    streamResponse.PrimaryUserPlayBackStreamingState = activeDeviceStreamingTypeList.OrderBy(e => e.Id).FirstOrDefault().PlaybackStreamingActive;
                    streamResponse.IsPrimaryUserPresent = true;
                    streamResponse.StreamChangeAlertMessage = _configuration.GetValue<String>("AlertPlaybackStreamShiftedMessage"); 
                }
                else
                {
                    streamResponse.IsPrimaryUserPresent = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetDevicePrimaryUserDetails method failed", ex);
            }

            return Ok(streamResponse);
        }

        /// <summary>
        /// Get the free Routes from SRT Server and assign to device and mobile app
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        [HttpPost("GetRoutes")]
        [ProducesResponseType(typeof(MMSHttpReponse<StreamResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoutes([FromHeader(Name = "uid")] string uid, DeviceList deviceIds, String view = "")
        {
            StreamResponse streamResponse = new StreamResponse();
            StreamResponse streamTypeResponseResult = new StreamResponse();

            #region Added condtion to check multiuser streaming swaping case
            string requestedUserTokenId = String.Empty;
            requestedUserTokenId = Request.Headers.FirstOrDefault(x => x.Key == "uid").Value.FirstOrDefault();
            if (requestedUserTokenId == null)
            {
                streamResponse.Status = false;
                streamResponse.Message = _configuration.GetValue<string>("UidFieldErrorMessage");
                return NotFound(streamResponse);
            }

            if (view.Trim().ToUpper() == "SINGLE")
            {
                
                streamTypeResponseResult = await AddUpdateDeviceStreamTypeStatus(deviceIds, "GETROUTES", string.Empty);
                streamResponse.ErrorCode = streamTypeResponseResult.ErrorCode;
                streamResponse.PrimaryUserLiveStreamingState = streamTypeResponseResult.PrimaryUserLiveStreamingState;
                streamResponse.PrimaryUserPlayBackStreamingState = streamTypeResponseResult.PrimaryUserPlayBackStreamingState;
                streamResponse.PrimaryUserDeviceId = streamTypeResponseResult.PrimaryUserDeviceId;
                streamResponse.PrimaryUserTokenId = streamTypeResponseResult.PrimaryUserTokenId;
                streamResponse.StreamChangeStatus = streamTypeResponseResult.StreamChangeStatus;
                streamResponse.StreamChangeAlertMessage = streamTypeResponseResult.StreamChangeAlertMessage;
            }
            #endregion

            if (!streamTypeResponseResult.PrimaryUserPlayBackStreamingState)
            {
                #region MyRegion
                var routes = await _srtRoutesRepo.GetAll().ToListAsync();
                string srtServerIP = _configuration.GetSection("SRTServerCredentials").GetSection("serverIP").Value;
                if (routes != null && routes.Count == 0)
                {
                    await CreateRoutes();
                }

                List<StreamData> streamResponseLst = new List<StreamData>();
                List<SrcStreamData> streamSrcResponseLst = new List<SrcStreamData>();
                foreach (var item in deviceIds.setCameraIds)
                {
                    var data = await _deviceRepo.GetDevice(item.deviceId, OrgId);
                    if (data != null)
                    {
                        var chkDeviceRoute = await _srtRoutesRepo.Query(x => x.DeviceId == item.deviceId).FirstOrDefaultAsync();
                        if (chkDeviceRoute == null)
                        {
                            var chkFreeRoute = await _srtRoutesRepo.Query(x => x.DeviceId == null).FirstOrDefaultAsync();
                            if (chkFreeRoute != null)
                            {
                                chkFreeRoute.DeviceId = item.deviceId;
                                chkFreeRoute.ModifiedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                                chkDeviceRoute = chkFreeRoute;
                                _srtRoutesRepo.SaveChanges();
                            }
                            else
                            {
                                // check for the disconnected routes
                                CookieContainer cookieContainer = new CookieContainer();
                                string gatewayUrl = "https://" + srtServerIP + "/api";
                                string sessionId = "";
                                sessionId = await getSessionId();
                                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                                using (var client = new HttpClient(handler)
                                {
                                    BaseAddress = new Uri(gatewayUrl)
                                })
                                {
                                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                                    cookieContainer.Add(new Uri(gatewayUrl), new Cookie("sessionID", sessionId));

                                    var divResponse = await client.GetAsync(gatewayUrl + "/devices");
                                    var divMessage = divResponse.Content.ReadAsStringAsync().Result;
                                    var divResult = System.Text.Json.JsonSerializer.Deserialize<List<SRTDevices>>(divMessage);

                                    var response = await client.GetAsync(gatewayUrl + "/gateway/" + divResult[0]._id + "/routes?page=1&pageSize=500");
                                    var responseMessage = response.Content.ReadAsStringAsync().Result;
                                    var result = System.Text.Json.JsonSerializer.Deserialize<SRTRouterList>(responseMessage);
                                    if (result != null)
                                    {
                                        if (result.data.Count > 0)
                                        {
                                            var chkExRoute = await _srtRoutesRepo.Query(x => x.RouteId == null).ToListAsync();
                                            if (chkExRoute.Count > 0)
                                            {
                                                foreach (var chk in chkExRoute)
                                                {
                                                    var chkOnSrt = result.data.Where(x => x.source.port == chk.Sor_Port).FirstOrDefault();
                                                    if (chkOnSrt != null)
                                                    {
                                                        chk.RouteId = chkOnSrt.id;
                                                    }
                                                }
                                                _srtRoutesRepo.SaveChanges();
                                            }
                                            var chkFreeRoutes = result.data.Where(x => x.source.state == "disconnected" && x.destinations[0].state == "disconnected").ToList();
                                            if (chkFreeRoutes.Count > 0)
                                            {
                                                int r = 0;
                                                foreach (var rt in chkFreeRoutes)
                                                {
                                                    var existingAssignRoute = streamResponseLst.Where(x => x.destinationPort == rt.destinations[0].port);
                                                    if (existingAssignRoute == null)
                                                    {
                                                        r = 1;
                                                        var chkPrevDevRoute = await _srtRoutesRepo.Query(x => x.Des_Port == rt.destinations[0].port).FirstOrDefaultAsync();
                                                        if (chkPrevDevRoute != null)
                                                        {
                                                            chkPrevDevRoute.DeviceId = item.deviceId;
                                                            chkPrevDevRoute.ModifiedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                                                            chkDeviceRoute = chkPrevDevRoute;
                                                            _srtRoutesRepo.SaveChanges();
                                                        }
                                                        else
                                                        {
                                                            SRTRoutes sRTRoutes = new SRTRoutes()
                                                            {
                                                                Id = Guid.NewGuid().ToString(),
                                                                RouteId = rt.id,
                                                                Des_Port = rt.destinations[0].port,
                                                                Des_PortName = rt.destinations[0].name,
                                                                Sor_Port = rt.source.port,
                                                                Sor_PortName = rt.source.name,
                                                                GatewayIP = srtServerIP,
                                                                State = rt.state,
                                                                DeviceId = item.deviceId,
                                                                PassPhrase = rt.source.srtPassPhrase,
                                                                CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                                                CreatedBy = "",
                                                                ModifiedBy = "",
                                                                RouteName = rt.name
                                                            };
                                                            _srtRoutesRepo.Add(sRTRoutes);
                                                            chkDeviceRoute = sRTRoutes;
                                                            _srtRoutesRepo.SaveChanges();
                                                        }
                                                        break;
                                                    }
                                                }
                                                if (r == 0)
                                                {
                                                    if (result.data.Count < Convert.ToInt32(_configuration.GetValue<int>("SrtRouetMaxCount")))
                                                    {
                                                        var srtRT = await dynamicRouteCreation(sessionId, divResult[0]._id, item.deviceId, result);
                                                        if (srtRT.RouteName != null)
                                                        {
                                                            chkDeviceRoute = srtRT;
                                                        }
                                                    }
                                                }
                                            }
                                            else if (result.data.Count < Convert.ToInt32(_configuration.GetValue<int>("SrtRouetMaxCount")))
                                            {
                                                var srtRT = await dynamicRouteCreation(sessionId, divResult[0]._id, item.deviceId, result);
                                                if (srtRT.RouteName != null)
                                                {
                                                    chkDeviceRoute = srtRT;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (chkDeviceRoute != null)
                        {
                            string resol = "480p";
                            if (deviceIds.setCameraIds.Count == 1)
                            {
                                resol = data.FeatureConfig.VideoResolution == null ? "480p" : data.FeatureConfig.VideoResolution.livestream == null ? "480p" : data.FeatureConfig.VideoResolution.livestream;
                            }
                            var jsonDesParam = new StreamData
                            {
                                deviceId = item.deviceId,
                                gateway_IP = srtServerIP,
                                destinationPort = chkDeviceRoute.Des_Port,
                                passphrase = chkDeviceRoute.PassPhrase,
                                state = chkDeviceRoute.State,
                                resolution = resol,
                            };
                            streamResponseLst.Add(jsonDesParam);

                            var jsonSorParam = new SrcStreamData
                            {
                                gateway_IP = srtServerIP,
                                sourcePort = chkDeviceRoute.Sor_Port,
                                passphrase = chkDeviceRoute.PassPhrase,
                                resolution = resol,
                            };
                            streamSrcResponseLst.Add(jsonSorParam);
                        }
                    }
                }
                if (streamResponseLst.Count > 0)
                {
                    int j = 0;
                    foreach (var item in streamResponseLst)
                    {
                    Loop:
                        try
                        {
                            if (j == streamResponseLst.Count)
                            {
                                break;
                            }
                            else
                            {
                                j++;
                                var data = await _deviceRepo.GetDevice(streamResponseLst[j - 1].deviceId, OrgId);
                                var res = await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "live_stream_start", 5, _logger, JsonConvert.SerializeObject(streamSrcResponseLst[j - 1]));
                                if (Convert.ToInt16(res.Status) == 200)
                                {
                                    streamResponseLst[j - 1].code = 200;
                                    streamResponseLst[j - 1].message = "Success";
                                }
                                else
                                {
                                    streamResponseLst[j - 1].code = 400;
                                    streamResponseLst[j - 1].message = "Failed";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            streamResponseLst[j - 1].code = 400;
                            if (ex.Message.Contains(":404103,"))
                            {
                                _logger.LogError($"Device is not online Exception :- {ex}");
                                streamResponseLst[j - 1].message = "Device is not online";
                            }
                            else if (ex.Message.Contains(":504101,"))
                            {
                                _logger.LogError($"Request time out Exception :- {ex}");
                                streamResponseLst[j - 1].message = "Request time out Exception";
                            }
                            else if (ex.Message.Contains(":404001,"))
                            {
                                _logger.LogError($"Device not found  Exception :- {ex}");
                                streamResponseLst[j - 1].message = "Device not found";
                            }
                            else
                            {
                                _logger.LogError($"Exception in DeviceValidationCheck Exception :- {ex}");
                                streamResponseLst[j - 1].message = "Exception in DeviceValidationCheck";
                            }
                            goto Loop;
                        }
                    }
                }

                streamResponse.Status = true;
                streamResponse.Message = "Routes";
                streamResponse.StreamData = streamResponseLst;
                #endregion
            }

            return Ok(new MMSHttpReponse<StreamResponse> { ResponseBody = streamResponse });
        }


        /// <summary>
        /// dynamic route create
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="srtDeviceId"></param>
        /// <param name="deviceId"></param>
        /// <param name="srtRouterList"></param>
        /// <returns></returns>
        [HttpGet("dynamicRouteCreation")]
        public async Task<SRTRoutes> dynamicRouteCreation(string sessionId, string srtDeviceId, string deviceId, SRTRouterList srtRouterList)
        {
            SRTRoutes srtRoute = new SRTRoutes();
            try
            {
                var routes = await _srtRoutesRepo.GetAll().OrderByDescending(x => x.Des_Port).ToListAsync();
                string srtServerIP = _configuration.GetSection("SRTServerCredentials").GetSection("serverIP").Value;
                if (routes != null && routes.Count > 0)
                {
                    var lastRoute = routes[0];
                    int lastRouteNo = Convert.ToInt32(lastRoute.RouteName.Split("HNYI")[1]) + 1;
                    List<Destination> destinations = new List<Destination>();
                    Source source = new Source()
                    {
                        name = "Cam" + lastRouteNo.ToString(),
                        id = "",
                        address = "0.0.0.0",
                        protocol = "srt",
                        port = lastRoute.Sor_Port + 2,
                        networkInterface = "",
                        srtPassPhrase = "",
                        srtLatency = 125,
                        srtMode = "listener",
                        srtGroupMode = "none",
                        srtNetworkBondingParams = "",
                        srtRcvBuf = 1024000
                    };
                    Destination destination = new Destination()
                    {
                        name = "dest" + lastRouteNo.ToString(),
                        id = "",
                        protocol = "srt",
                        port = lastRoute.Des_Port + 2,
                        address = "0.0.0.0",
                        networkInterface = "",
                        action = "start",
                        srtEncryption = "none",
                        srtPassPhrase = "",
                        srtLatency = 125,
                        srtMode = "listener",
                        srtGroupMode = "none",
                        srtNetworkBondingParams = ""
                    };
                    destinations.Add(destination);

                    Routes fields = new Routes()
                    {
                        name = "HNYI" + lastRouteNo.ToString(),
                        startRoute = true,
                        source = source,
                        destinations = destinations
                    };
                    CreateRoutes srt_routes = new CreateRoutes()
                    {
                        action = "create",
                        deviceID = srtDeviceId,
                        elementType = "route",
                        fields = fields
                    };
                    var alreayExistRoute = srtRouterList.data.Where(x => x.name == fields.name || x.source.port == fields.source.port).FirstOrDefault();
                    if (alreayExistRoute == null)
                    {

                        string gatewayUrl = "https://" + srtServerIP + "/api";
                        CookieContainer cookieContainer = new CookieContainer();
                        using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                        using (var client = new HttpClient(handler)
                        {
                            BaseAddress = new Uri(gatewayUrl)
                        })
                        {
                            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                            var srtAuth = System.Text.Json.JsonSerializer.Serialize(srt_routes);
                            cookieContainer.Add(new Uri(gatewayUrl + "/devices/" + srtDeviceId + "/updates"), new Cookie("sessionID", sessionId));

                            var requestContent = new StringContent(srtAuth, Encoding.UTF8, "application/json");
                            var res = await client.PostAsync(gatewayUrl + "/devices/" + srtDeviceId + "/updates", requestContent);
                            string responseMsg = res.Content.ReadAsStringAsync().Result;
                            var resul = System.Text.Json.JsonSerializer.Deserialize<SRTAuthResponse>(responseMsg);

                            if (responseMsg != null)
                            {
                                var response = await client.GetAsync(gatewayUrl + "/gateway/" + srtDeviceId + "/routes?page=1&pageSize=500");
                                var responseMessage = response.Content.ReadAsStringAsync().Result;
                                var result = System.Text.Json.JsonSerializer.Deserialize<SRTRouterList>(responseMessage);
                                if (result != null && result.data.Count > 0)
                                {
                                    var chknewRoutes = result.data.Where(x => x.name == srt_routes.fields.name).FirstOrDefault();
                                    if (chknewRoutes != null)
                                    {
                                        SRTRoutes sRTRoutes = new SRTRoutes()
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            RouteId = chknewRoutes.id,
                                            Des_Port = chknewRoutes.destinations[0].port,
                                            Des_PortName = chknewRoutes.destinations[0].name,
                                            Sor_Port = chknewRoutes.source.port,
                                            Sor_PortName = chknewRoutes.source.name,
                                            GatewayIP = srtServerIP,
                                            State = chknewRoutes.state,
                                            DeviceId = deviceId,
                                            PassPhrase = chknewRoutes.source.srtPassPhrase,
                                            CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                            CreatedBy = "",
                                            ModifiedBy = "",
                                            RouteName = chknewRoutes.name,
                                        };
                                        _srtRoutesRepo.Add(sRTRoutes);
                                        _srtRoutesRepo.SaveChanges();
                                        srtRoute = sRTRoutes;
                                    }
                                    else
                                    {
                                        var chkdb = _srtRoutesRepo.Query(x => x.RouteName == srt_routes.fields.name).FirstOrDefault();
                                        if (chkdb == null)
                                        {
                                            SRTRoutes sRTRoutes = new SRTRoutes()
                                            {
                                                Id = Guid.NewGuid().ToString(),
                                                RouteId = null,
                                                Des_Port = srt_routes.fields.destinations[0].port,
                                                Des_PortName = srt_routes.fields.destinations[0].name,
                                                Sor_Port = srt_routes.fields.source.port,
                                                Sor_PortName = srt_routes.fields.source.name,
                                                GatewayIP = srtServerIP,
                                                State = "running",
                                                DeviceId = deviceId,
                                                PassPhrase = "",
                                                CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                                CreatedBy = "",
                                                ModifiedBy = "",
                                                RouteName = srt_routes.fields.name,
                                            };
                                            _srtRoutesRepo.Add(sRTRoutes);
                                            _srtRoutesRepo.SaveChanges();
                                            srtRoute = sRTRoutes;
                                        }
                                    }
                                }

                            }

                        }

                    }
                    else
                    {
                        var chkdb = _srtRoutesRepo.Query(x => x.RouteName == alreayExistRoute.name).FirstOrDefault();
                        if (chkdb == null)
                        {
                            SRTRoutes sRTRoutes = new SRTRoutes()
                            {
                                Id = Guid.NewGuid().ToString(),
                                RouteId = alreayExistRoute.id,
                                Des_Port = alreayExistRoute.destinations[0].port,
                                Des_PortName = alreayExistRoute.destinations[0].name,
                                Sor_Port = alreayExistRoute.source.port,
                                Sor_PortName = alreayExistRoute.source.name,
                                GatewayIP = srtServerIP,
                                State = alreayExistRoute.state,
                                DeviceId = deviceId,
                                PassPhrase = alreayExistRoute.source.srtPassPhrase,
                                CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                CreatedBy = "",
                                ModifiedBy = "",
                                RouteName = alreayExistRoute.name,
                            };
                            _srtRoutesRepo.Add(sRTRoutes);
                            _srtRoutesRepo.SaveChanges();
                            srtRoute = sRTRoutes;
                        }
                        else
                        {
                            chkdb.DeviceId = deviceId;
                            chkdb.ModifiedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                            srtRoute = chkdb;
                            _srtRoutesRepo.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return srtRoute;
        }


        [HttpPost("StartStreaming")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartStreaming([FromHeader(Name = "uid")] string uid, string type, string timestamp, string start_time, DeviceList deviceIds, double offSet)
        {
            StreamResponse streamResponse = new StreamResponse();
            StreamResponse streamSwapTypeResponse = new StreamResponse();
            List<StreamData> streamResponseLst = new List<StreamData>();
            streamSwapTypeResponse = await AddUpdateDeviceStreamTypeStatus(deviceIds, "STARTSTREAMING", type);
            int dbResult = 0;
            string requestedUserTokenId = String.Empty;
            long epochDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

            #region Multiuser stream change
            streamResponse.PrimaryUserDeviceId = streamSwapTypeResponse.PrimaryUserDeviceId;
            streamResponse.PrimaryUserTokenId = streamSwapTypeResponse.PrimaryUserTokenId;
            streamResponse.PrimaryUserLiveStreamingState = streamSwapTypeResponse.PrimaryUserLiveStreamingState;
            streamResponse.PrimaryUserPlayBackStreamingState = streamSwapTypeResponse.PrimaryUserPlayBackStreamingState;
            streamResponse.StreamChangeAlertMessage = streamSwapTypeResponse.StreamChangeAlertMessage;
            streamResponse.ErrorCode = streamSwapTypeResponse.ErrorCode;
            streamResponse.StreamChangeStatus = streamSwapTypeResponse.StreamChangeStatus;
            #endregion


            if (!streamSwapTypeResponse.StreamChangeStatus)
            {
                //requestedUserTokenId = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer", String.Empty);
                requestedUserTokenId = Request.Headers.FirstOrDefault(x => x.Key == "uid").Value.FirstOrDefault();
                if (requestedUserTokenId == null)
                {
                    streamResponse.Status = false;
                    streamResponse.Message = _configuration.GetValue<string>("UidFieldErrorMessage");
                    return NotFound(streamResponse);
                }

                DeviceStreamingTypeManagement deviceStreamingTypeManagementException = new DeviceStreamingTypeManagement();
                string srtServerIP = _configuration.GetSection("SRTServerCredentials").GetSection("serverIP").Value;

                if (deviceIds.setCameraIds.Count > 0)
                {
                    if (type == "sd_card_stream")
                    {
                        deviceStreamingTypeManagementException = new DeviceStreamingTypeManagement
                        {
                            UserTokenId = requestedUserTokenId,
                            LogicalDeviceId = deviceIds.setCameraIds[0].deviceId,
                            LiveStreamingActive = true,
                            PlaybackStreamingActive = false,
                            ModifiedDate = epochDate
                        };


                        try
                        {
                            if (!string.IsNullOrWhiteSpace(deviceIds.setCameraIds[0].deviceId))
                            {
                                string date = Convert.ToDateTime(start_time).ToString("yyyy-MM-dd HH:mm:ss");
                                string seekTime = Convert.ToDateTime(timestamp).ToString("yyyy-MM-dd HH:mm:ss").Split(' ')[1].Replace(':', '-');
                                var data = await _deviceRepo.GetDevice(deviceIds.setCameraIds[0].deviceId, OrgId);
                                if (data != null)
                                {
                                    string chunkDate = date.Split(' ')[0];
                                    string chuntStartTime = date.Split(' ')[1];
                                    var chkDeviceRoute = await _srtRoutesRepo.Query(x => x.DeviceId == deviceIds.setCameraIds[0].deviceId).FirstOrDefaultAsync();
                                    var chkRecordingType = await _cardHistoryRepo.Query(x => x.deviceId == deviceIds.setCameraIds[0].deviceId && x.date == chunkDate && x.startTime == chuntStartTime).FirstOrDefaultAsync();
                                    if (chkRecordingType != null)
                                    {
                                        var jsonSorParam = new
                                        {
                                            gateway_IP = srtServerIP,
                                            sourcePort = chkDeviceRoute.Sor_Port,
                                            passphrase = chkDeviceRoute.PassPhrase,
                                            date = date.Split(' ')[0],
                                            start_time = date.Split(' ')[1].Replace(':', '-'),
                                            seek_time = seekTime,
                                            recording_type = chkRecordingType.type == 1 ? 2 : chkRecordingType.type == 2 ? 1 : chkRecordingType.type
                                        };


                                        try
                                        {
                                            var res = await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "playback_streaming", 12, _logger, JsonConvert.SerializeObject(jsonSorParam));
                                            if (Convert.ToInt16(res.Status) == 500)
                                            {
                                                streamResponse.Status = false;
                                                streamResponse.Message = "Video not found";

                                                streamResponse.PrimaryUserLiveStreamingState = true;
                                                streamResponse.PrimaryUserPlayBackStreamingState = false;
                                                dbResult = await UpdateDeviceStreamTypeDB(deviceStreamingTypeManagementException);
                                            }
                                            else if (Convert.ToInt16(res.Status) == 200)
                                            {
                                                streamResponse.Status = true;
                                                streamResponse.Message = "Method invoked";
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.Message.Contains(":404103,"))
                                            {
                                                _logger.LogError($"Device is not online Exception :- {ex}");
                                                streamResponse.Status = false;
                                                streamResponse.Message = "Device is not online";
                                            }
                                            else if (ex.Message.Contains(":504101,"))
                                            {
                                                _logger.LogError($"Request time out Exception :- {ex}");
                                                streamResponse.Status = false;
                                                streamResponse.Message = "Request time out Exception";

                                            }
                                            else if (ex.Message.Contains(":404001,"))
                                            {
                                                _logger.LogError($"Device not found  Exception :- {ex}");
                                                streamResponse.Status = false;
                                                streamResponse.Message = "Device not found";
                                            }
                                            else
                                            {
                                                _logger.LogError($"Exception in DeviceValidationCheck Exception :- {ex}");
                                                streamResponse.Status = false;
                                                streamResponse.Message = "Exception in DeviceValidationCheck";
                                            }

                                            streamResponse.PrimaryUserLiveStreamingState = true;
                                            streamResponse.PrimaryUserPlayBackStreamingState = false;
                                            dbResult = await UpdateDeviceStreamTypeDB(deviceStreamingTypeManagementException);
                                        }
                                    }
                                    else
                                    {
                                        streamResponse.Status = false;
                                        streamResponse.Message = "Video not found";

                                        streamResponse.PrimaryUserLiveStreamingState = true;
                                        streamResponse.PrimaryUserPlayBackStreamingState = false;
                                        dbResult = await UpdateDeviceStreamTypeDB(deviceStreamingTypeManagementException);
                                    }
                                }
                            }
                            else
                            {
                                streamResponse.Status = false;
                                streamResponse.Message = "Invalid Device ID";

                                streamResponse.PrimaryUserLiveStreamingState = true;
                                streamResponse.PrimaryUserPlayBackStreamingState = false;
                                dbResult = await UpdateDeviceStreamTypeDB(deviceStreamingTypeManagementException);
                            }
                        }
                        catch (Exception e)
                        {
                            streamResponse.Status = false;
                            streamResponse.Message = e.ToString();

                            streamResponse.PrimaryUserLiveStreamingState = true;
                            streamResponse.PrimaryUserPlayBackStreamingState = false;
                            dbResult = await UpdateDeviceStreamTypeDB(deviceStreamingTypeManagementException);
                        }
                    }
                    else
                    {
                        int j = 0;
                        foreach (var item in deviceIds.setCameraIds)
                        {
                        Loop:
                            if (j == deviceIds.setCameraIds.Count)
                            {
                                break;
                            }

                            var data = await _deviceRepo.GetDevice(deviceIds.setCameraIds[j].deviceId, OrgId);
                            if (data != null)
                            {
                                string resol = "480p";
                                if (deviceIds.setCameraIds.Count == 1)
                                {
                                    resol = data.FeatureConfig.VideoResolution == null ? "480p" : data.FeatureConfig.VideoResolution.livestream == null ? "480p" : data.FeatureConfig.VideoResolution.livestream;
                                }
                                var chkDeviceRoute = await _srtRoutesRepo.Query(x => x.DeviceId == deviceIds.setCameraIds[j].deviceId).FirstOrDefaultAsync();
                                var jsonSorParam = new
                                {
                                    gateway_IP = srtServerIP,
                                    sourcePort = chkDeviceRoute.Sor_Port,
                                    passphrase = chkDeviceRoute.PassPhrase,
                                    resolution = resol,
                                };
                                //direct method for hub
                                try
                                {
                                    var res = await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "live_stream_start", 5, _logger, JsonConvert.SerializeObject(jsonSorParam));
                                    if (Convert.ToInt16(res.Status) == 200)
                                    {
                                        streamResponse.Status = true;
                                        streamResponse.Message = "Method invoked";
                                    }
                                    else
                                    {
                                        streamResponse.Status = false;
                                        streamResponse.Message = "Device direct method return status:" + res.Status;
                                    }
                                    j++;
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.Contains(":404103,"))
                                    {
                                        _logger.LogError($"Device is not online Exception :- {ex}");
                                        streamResponse.Status = false;
                                        streamResponse.Message = "Device is not online";
                                    }
                                    else if (ex.Message.Contains(":504101,"))
                                    {
                                        _logger.LogError($"Request time out Exception :- {ex}");
                                        streamResponse.Status = false;
                                        streamResponse.Message = "Request time out Exception";

                                    }
                                    else if (ex.Message.Contains(":404001,"))
                                    {
                                        _logger.LogError($"Device not found  Exception :- {ex}");
                                        streamResponse.Status = false;
                                        streamResponse.Message = "Device not found";
                                    }
                                    else
                                    {
                                        _logger.LogError($"Exception in DeviceValidationCheck Exception :- {ex}");
                                        streamResponse.Status = false;
                                        streamResponse.Message = "Exception in DeviceValidationCheck";
                                    }
                                    j++;
                                    goto Loop;
                                }
                            }
                        }
                    }
                }
            }
           
            return Ok(streamResponse);
        }

        /// <summary>
        /// Copy the routes from SRT Server to MSSQL DB
        /// </summary>
        /// <returns></returns>
        [HttpPost("CreateRoutes")]
        public async Task<IActionResult> CreateRoutes()
        {
            try
            {
                string srtServerIP = _configuration.GetSection("SRTServerCredentials").GetSection("serverIP").Value;

                string checkserver = _configuration.GetValue<string>("IoTHubConString");

                //List<SRTRoutes> sRTRoutesLst = new List<SRTRoutes>();
                CookieContainer cookieContainer = new CookieContainer();
                string gatewayUrl = "https://" + srtServerIP + "/api";
                string sessionId = "";
                sessionId = await getSessionId();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(gatewayUrl)
                })
                {
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                    cookieContainer.Add(new Uri(gatewayUrl), new Cookie("sessionID", sessionId));

                    var divResponse = await client.GetAsync(gatewayUrl + "/devices");
                    var divMessage = divResponse.Content.ReadAsStringAsync().Result;
                    var divResult = System.Text.Json.JsonSerializer.Deserialize<List<SRTDevices>>(divMessage);


                    var response = await client.GetAsync(gatewayUrl + "/gateway/" + divResult[0]._id + "/routes?page=1&pageSize=500");
                    var responseMessage = response.Content.ReadAsStringAsync().Result;
                    var result = System.Text.Json.JsonSerializer.Deserialize<SRTRouterList>(responseMessage);
                    if (result != null)
                    {
                        if (result.data.Count > 0)
                        {
                            foreach (var item in result.data)
                            {
                                var chkSrtRoute = await _srtRoutesRepo.Query(x => x.RouteId == item.id).FirstOrDefaultAsync();
                                if (chkSrtRoute == null)
                                {
                                    SRTRoutes sRTRoutes = new SRTRoutes()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        RouteId = item.id,
                                        Des_Port = item.destinations[0].port,
                                        Des_PortName = item.destinations[0].name,
                                        Sor_Port = item.source.port,
                                        Sor_PortName = item.source.name,
                                        GatewayIP = srtServerIP,
                                        State = item.state,
                                        DeviceId = null,
                                        PassPhrase = item.source.srtPassPhrase,
                                        CreatedDate = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                        CreatedBy = "",
                                        ModifiedBy = "",
                                        RouteName = item.name,
                                    };
                                    await _srtRoutesRepo.AddAsync(sRTRoutes);
                                    _srtRoutesRepo.SaveChanges();
                                }
                            }
                            
                        }

                    }
                }

                return Ok("Success");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        /// <summary>
        /// Check the SRT Connection
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        [HttpGet("check_srt_connection")]
        [AllowAnonymous]
        public async Task<IActionResult> checkSrtConnection(string deviceId, string secretKey)
        {
            try
            {
                string scrtKey = _configuration.GetSection("SRTServerCredentials").GetSection("secretKey").Value;
                if (secretKey == scrtKey)
                {
                    string srtServerIP = _configuration.GetSection("SRTServerCredentials").GetSection("serverIP").Value;
                    CookieContainer cookieContainer = new CookieContainer();
                    string gatewayUrl = "https://" + srtServerIP + "/api";
                    string sessionId = "";
                    sessionId = await getSessionId();
                    var chkDeviceRoute = await _srtRoutesRepo.Query(x => x.DeviceId == deviceId).FirstOrDefaultAsync();
                    if (chkDeviceRoute != null)
                    {
                        using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                        using (var client = new HttpClient(handler)
                        {
                            BaseAddress = new Uri(gatewayUrl)
                        })
                        {
                            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                            cookieContainer.Add(new Uri(gatewayUrl), new Cookie("sessionID", sessionId));

                            var divResponse = await client.GetAsync(gatewayUrl + "/devices");
                            var divMessage = divResponse.Content.ReadAsStringAsync().Result;
                            var divResult = System.Text.Json.JsonSerializer.Deserialize<List<SRTDevices>>(divMessage);
                            var url = gatewayUrl + "/gateway/" + divResult[0]._id + "/statistics?routeID=" + chkDeviceRoute.RouteId;
                            _logger.LogInformation($"check_srt_connection url :- {url}");
                            var response = await client.GetAsync(gatewayUrl + "/gateway/" + divResult[0]._id + "/statistics?routeID=" + chkDeviceRoute.RouteId);
                            _logger.LogInformation($"response :- {response}");
                            var responseMessage = response.Content.ReadAsStringAsync().Result;
                            _logger.LogInformation($"responseMessage :- {responseMessage}");
                            var result = System.Text.Json.JsonSerializer.Deserialize<srtSessionResponse>(responseMessage);
                            //_logger.LogInformation($"check_srt_connection result :- {result.route.destinations.FirstOrDefault().clientsStat.FirstOrDefault().connections.FirstOrDefault().state}");
                            if (result != null)
                            {
                                if (result.route.destinations.FirstOrDefault().clientsStat!=null && result.route.destinations.FirstOrDefault().clientsStat.Count > 0)
                                {
                                    if (result.route.destinations.FirstOrDefault().clientsStat.FirstOrDefault().connections.FirstOrDefault().state == "disconnected")  //connected
                                    {
                                        var resultJson = new
                                        {
                                            Status = true,
                                            Message = "Stop streaming",
                                        };
                                        await ResetDeviceStreamingTypeManagement(_logger, deviceId);
                                        return Ok(resultJson);
                                    }
                                    else
                                    {
                                        var resultJson = new
                                        {
                                            Status = false,
                                            Message = "continues streaming",
                                        };
                                        return Ok(resultJson);
                                    }
                                }
                                else
                                {
                                    var resultJson = new
                                    {
                                        Status = true,
                                        Message = "Stop streaming",
                                    };
                                    await ResetDeviceStreamingTypeManagement(_logger, deviceId);
                                    return Ok(resultJson);
                                }

                            }
                            else
                            {
                                var resultJson = new
                                {
                                    Status = false,
                                    Message = "continues streaming",
                                };
                                return Ok(resultJson);
                            }
                        }
                    }
                    else
                    {
                        var resJson = new
                        {
                            Status = true,
                            Message = "Stop streaming",
                        };
                        await ResetDeviceStreamingTypeManagement(_logger, deviceId);
                        return Ok(resJson);
                    }
                }
                else
                {
                    var resJson = new
                    {
                        Status = false,
                        Message = "wrong secret key",
                    };
                    return Ok(resJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured check_srt_connection Ex:- {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError);

            }

        }

        /// <summary>
        /// Stop the Streaming
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <returns></returns>
        [HttpPost("StopStreaming")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StopStreaming(DeviceList deviceIds)
        {
            var result = new
            {
                Status = true,
                Message = "Streaming stop",
            };
            int j = 0;
            foreach (var item in deviceIds.setCameraIds)
            {
            Loop:
                var data = await _deviceRepo.GetDevice(deviceIds.setCameraIds[j].deviceId, OrgId);
                if (data != null)
                {
                    try
                    {
                        if (j == deviceIds.setCameraIds.Count)
                        {
                            break;
                        }
                        else
                        {
                            await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "live_stream_stop", 10, _logger);
                            j++;
                        }
                    }
                    catch (Exception ex)
                    {
                        j++;
                        goto Loop;
                    }
                }
            }
            return Ok(result);
        }

        /// <summary>
        /// Encryption of data
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="encryption"></param>
        /// <returns></returns>
        [HttpGet("SetEncryption")]
        [ProducesResponseType(typeof(MMSHttpReponse<List<RecordSettingsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetEncryption(string device_id, bool encryption)
        {
            try
            {
                RecordSettingsResponse recordSettingsResponse = new RecordSettingsResponse();
                if (!string.IsNullOrEmpty(device_id))
                {
                    var data = await _deviceRepo.GetDevice(device_id, OrgId);
                    if (data == null)
                    {
                        return NotFound(new MMSHttpReponse { ErrorMessage = "Device not found" });
                    }

                    if (!IsDeviceAuthorizedAsAdmin(data, _userDevicePermissionRepo))
                    {
                        return Forbid();
                    }
                    if (!data.SdCardAvilable)
                    {
                        recordSettingsResponse.Status = false;
                        recordSettingsResponse.Message = "SD Card is not plugged-In";
                        return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
                    }
                    data.Encryption = encryption;
                    try
                    {
                        //await _iotHub.UpdateSDCardRecordSettings(device_id, "video_encryption", false, false, false, false, fifo, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception in Configuring Iot for deviceid {device_id} Exception :- {ex}");
                        recordSettingsResponse.Status = false;
                        recordSettingsResponse.Message = $"Request time out for deviceid{device_id}";
                        return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
                    }
                    await _deviceRepo.UpdateDevice(OrgId, data);

                    recordSettingsResponse.Status = true;
                    recordSettingsResponse.Message = "Encryption updated successfully";
                }
                else
                {
                    recordSettingsResponse.Status = false;
                    recordSettingsResponse.Message = "Invalid deviceId";
                }

                return Ok(new MMSHttpReponse<RecordSettingsResponse> { ResponseBody = recordSettingsResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError("Encryption updation failed", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        ///// <summary>
        ///// For checking the mobile app connection alive.
        ///// </summary>
        ///// <param name="deviceIds"></param>
        ///// <returns></returns>
        [HttpPost("check_stream_connection")]
        public async Task<IActionResult> checkStreamConnection(DeviceList deviceIds)
        {
            if (deviceIds.setCameraIds.Count > 0)
            {
                List<StreamData> streamResponseLst = new List<StreamData>();
                int j = 0;
                foreach (var item in deviceIds.setCameraIds)
                {
                Loop:
                    if (j == deviceIds.setCameraIds.Count)
                        break;

                    j++;
                    var data = await _deviceRepo.GetDevice(deviceIds.setCameraIds[j - 1].deviceId, OrgId);
                    if (data != null)
                    {
                        try
                        {
                            var res = await _iotHub.InvokeDeviceDirectMethodAsync(data.Id, "srt_connection_status", 5, _logger);
                            if (Convert.ToInt16(res.Status) != 200)
                            {
                                var jsonParam = new StreamData()
                                {
                                    deviceId = deviceIds.setCameraIds[j - 1].deviceId,
                                    code = 400,
                                    hubId = data.Id,
                                };
                                streamResponseLst.Add(jsonParam);
                            }
                        }
                        catch (Exception ex)
                        {
                            var jsonParam = new StreamData()
                            {
                                deviceId = deviceIds.setCameraIds[j - 1].deviceId,
                                code = 400,
                                hubId = data.Id,
                            };
                            streamResponseLst.Add(jsonParam);
                            goto Loop;
                        }
                    }
                }
                int i = 0;
                int k = 0;
                if (streamResponseLst.Count > 0)
                {
                    foreach (var item in streamResponseLst)
                    {

                    loop:
                        if (i == streamResponseLst.Count)
                        {
                            break;
                        }
                        try
                        {
                            var res = await _iotHub.InvokeDeviceDirectMethodAsync(streamResponseLst[i].hubId, "srt_connection_status", 30, _logger);

                            i++;
                        }
                        catch (Exception ex)
                        {
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();
                            if (k == 2)
                            {
                                i++;
                                k = 0;
                                goto loop;
                            }
                            else
                            {
                                while (true)
                                {
                                    if (TimeSpan.FromSeconds(15) < stopwatch.Elapsed)
                                    {
                                        k++;
                                        goto loop;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return Ok(new MMSHttpReponse { SuccessMessage = "success" });
        }

        /// <summary>
        /// Method add or update multiuser accessing same device streaming type 
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <param name="sourceMethod"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        private async Task<StreamResponse> AddUpdateDeviceStreamTypeStatus(DeviceList deviceIds, string sourceMethod, string requestType)
        {
            string requestedUserTokenId = string.Empty;
            int primaryUserId = 0;
            string primaryUserTokenId = String.Empty;
            string primaryUserDeviceId = String.Empty;
            string primaryUserStreamType = String.Empty;
            Boolean primaryUserLiveStreamingState = false;
            Boolean primaryUserPlayBackStreamingState = false;
            string streamRequestType = string.Empty;
            long epochDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
            int dbResult = 0;
            string notificationMessage = String.Empty;
            StreamResponse streamSwapResponse = new StreamResponse();
            bool isRequestedDeviceExistFlag = false;

            try
            {
                var activeDeviceStreamingTypeList = _deviceStreamingTypeManagementRepo.GetAll()
                    .Where(e=>e.IsUserStreaming == true)
                    .ToList<DeviceStreamingTypeManagement>();
                requestedUserTokenId = Request.Headers.FirstOrDefault(x => x.Key == "uid").Value.FirstOrDefault();
                streamRequestType = sourceMethod.Trim().ToUpper() == "STARTSTREAMING" && requestType == null ? "LIVE_STREAM" : requestType;

                foreach (var dId in deviceIds.setCameraIds)
                {
                    var activeDeviceList = activeDeviceStreamingTypeList.Where(e => e.LogicalDeviceId == dId.deviceId.ToString() && e.IsUserStreaming == true).ToList();
                    var isRequestedDeviceExist = activeDeviceStreamingTypeList.Where(e => e.LogicalDeviceId == dId.deviceId.ToString() && e.IsUserStreaming == true &&
                    e.UserTokenId == requestedUserTokenId).ToList();

                    if (activeDeviceList.Count() == 0)
                    {
                        streamSwapResponse.PrimaryUserLiveStreamingState = true;
                        streamSwapResponse.PrimaryUserPlayBackStreamingState = false;
                        streamSwapResponse.PrimaryUserDeviceId = dId.deviceId;
                        streamSwapResponse.PrimaryUserTokenId = requestedUserTokenId;
                    }
                    else if (activeDeviceList != null & activeDeviceList.Count() > 0)
                    {
                        primaryUserId = activeDeviceList.OrderBy(e => e.Id).FirstOrDefault().Id;
                        primaryUserTokenId = activeDeviceList.OrderBy(e => e.Id).FirstOrDefault().UserTokenId;
                        primaryUserDeviceId = activeDeviceList.OrderBy(e => e.Id).FirstOrDefault().LogicalDeviceId;
                        primaryUserLiveStreamingState = activeDeviceList.OrderBy(e => e.Id).FirstOrDefault().LiveStreamingActive;
                        primaryUserPlayBackStreamingState = activeDeviceList.OrderBy(e => e.Id).FirstOrDefault().PlaybackStreamingActive;

                        streamSwapResponse.PrimaryUserLiveStreamingState = primaryUserLiveStreamingState;
                        streamSwapResponse.PrimaryUserPlayBackStreamingState = primaryUserPlayBackStreamingState;
                        streamSwapResponse.PrimaryUserDeviceId = dId.deviceId;
                        streamSwapResponse.PrimaryUserTokenId = primaryUserTokenId;

                    }

                    if (isRequestedDeviceExist.Count() == 0)
                    {
                        DeviceStreamingTypeManagement deviceStreamingTypeManagement = new DeviceStreamingTypeManagement
                        {
                            IsUserStreaming = true,
                            LogicalDeviceId = dId.deviceId,
                            OrgId = OrgId,
                            LiveStreamingActive = streamRequestType.Trim().ToUpper() == "SD_CARD_STREAM" ? false : true,
                            PlaybackStreamingActive = streamRequestType.Trim().ToUpper() == "SD_CARD_STREAM" ? true : false,
                            UserName = UserEmail,
                            UserTokenId = requestedUserTokenId,
                            ModifiedDate = epochDate,
                            ModifiedBy = UserEmail,
                            CreatedBy = UserEmail
                        };
                        dbResult = await AddDeviceStreamTypeDB(deviceStreamingTypeManagement);
                        streamSwapResponse.StreamChangeStatus = false;
                        isRequestedDeviceExistFlag = true;
                    }

                    if (activeDeviceList != null & activeDeviceList.Count() > 0 && (isRequestedDeviceExistFlag || isRequestedDeviceExist.Count() > 0))
                    {
                        if (primaryUserTokenId.Trim().ToLower() == requestedUserTokenId.Trim().ToLower()
                            && dId.deviceId == primaryUserDeviceId)
                        {
                            streamSwapResponse.StreamChangeStatus = false;

                            if (primaryUserLiveStreamingState == true && streamRequestType.Trim().ToUpper() == "SD_CARD_STREAM")
                            {
                                DeviceStreamingTypeManagement deviceStreamingTypeManagement = new DeviceStreamingTypeManagement
                                {
                                    UserTokenId = requestedUserTokenId,
                                    LogicalDeviceId = dId.deviceId,
                                    LiveStreamingActive = false,
                                    PlaybackStreamingActive = true,
                                    ModifiedDate = epochDate
                                };
                                dbResult = await UpdateDeviceStreamTypeDB(deviceStreamingTypeManagement);
                                streamSwapResponse.PrimaryUserLiveStreamingState = false;
                                streamSwapResponse.PrimaryUserPlayBackStreamingState = true;
                            }
                            else if (primaryUserPlayBackStreamingState == true && streamRequestType.Trim().ToUpper() != "SD_CARD_STREAM")
                            {
                                DeviceStreamingTypeManagement deviceStreamingTypeManagement = new DeviceStreamingTypeManagement
                                {
                                    UserTokenId = requestedUserTokenId,
                                    LogicalDeviceId = dId.deviceId,
                                    LiveStreamingActive = true,
                                    PlaybackStreamingActive = false,
                                    ModifiedDate = epochDate
                                };
                                dbResult = await UpdateDeviceStreamTypeDB(deviceStreamingTypeManagement);
                                streamSwapResponse.PrimaryUserLiveStreamingState = true;
                                streamSwapResponse.PrimaryUserPlayBackStreamingState = false;
                            }
                        }
                        else if (primaryUserTokenId.Trim().ToLower() != requestedUserTokenId.Trim().ToLower()
                            && dId.deviceId == primaryUserDeviceId && streamRequestType.Trim().ToUpper() != "SD_CARD_STREAM" && primaryUserLiveStreamingState)
                        {
                            streamSwapResponse.StreamChangeStatus = false;
                        }
                        else if (primaryUserTokenId.Trim().ToLower() != requestedUserTokenId.Trim().ToLower()
                            && dId.deviceId == primaryUserDeviceId && streamRequestType.Trim().ToUpper() == "SD_CARD_STREAM" && primaryUserLiveStreamingState)
                        {
                            streamSwapResponse.StreamChangeStatus = true;
                            streamSwapResponse.StreamChangeAlertMessage = _configuration.GetValue<string>("LiveStreamingSwapAlertMessage"); 
                            //"Device is busy. Another user is viewing live streaming.";
                            streamSwapResponse.ErrorCode = Convert.ToInt32(_configuration.GetValue<int>("StreamingLiveToPlaybackSwapErrorCode"));
                        }
                        else if (primaryUserTokenId.Trim().ToLower() != requestedUserTokenId.Trim().ToLower()
                            && dId.deviceId == primaryUserDeviceId && streamRequestType.Trim().ToUpper() != "SD_CARD_STREAM" && primaryUserPlayBackStreamingState)
                        {
                            streamSwapResponse.StreamChangeStatus = true;
                            streamSwapResponse.StreamChangeAlertMessage = _configuration.GetValue<string>("PlaybackStreamingSwapAlertMessage");  
                            //"Device is busy. Another user is viewing playback streaming.";
                            streamSwapResponse.ErrorCode = Convert.ToInt32(_configuration.GetValue<int>("StreamingPlaybackToPlaybackSwapErrorCode"));
                        }
                        else if (primaryUserTokenId.Trim().ToLower() != requestedUserTokenId.Trim().ToLower()
                            && dId.deviceId == primaryUserDeviceId && streamRequestType.Trim().ToUpper() == "SD_CARD_STREAM" && primaryUserPlayBackStreamingState)
                        {
                            streamSwapResponse.StreamChangeStatus = true;
                            streamSwapResponse.StreamChangeAlertMessage = _configuration.GetValue<string>("PlaybackStreamingSwapAlertMessage"); 
                            //"Device is busy. Another user is viewing playback streaming.";
                            streamSwapResponse.ErrorCode = Convert.ToInt32(_configuration.GetValue<int>("StreamingPlaybackToPlaybackSwapErrorCode"));
                        }
                    }
                    /*else if (isRequestedDeviceExist.Count() == 0)
                    {
                        DeviceStreamingTypeManagement deviceStreamingTypeManagement = new DeviceStreamingTypeManagement
                        {
                            IsUserStreaming = true,
                            LogicalDeviceId = dId.deviceId,
                            OrgId = OrgId,
                            LiveStreamingActive = true,
                            PlaybackStreamingActive = false,
                            UserName = UserEmail,
                            UserTokenId = requestedUserTokenId,
                            ModifiedDate = epochDate,
                            ModifiedBy = UserEmail,
                            CreatedBy = UserEmail
                        };
                        dbResult = await AddDeviceStreamTypeDB(deviceStreamingTypeManagement);
                        streamSwapResponse.StreamChangeStatus = false;
                    }*/
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("AddDeviceStreamTypeStatus method failed", ex);
            }
            return streamSwapResponse;
        }

        /// <summary>
        /// Add requested user device stream type in the database
        /// </summary>
        /// <param name="deviceStreamingTypeManagement"></param>
        /// <returns></returns>
        private async Task<int> AddDeviceStreamTypeDB(DeviceStreamingTypeManagement deviceStreamingTypeManagement)
        {
            int result = 0;
            try
            {
                _deviceStreamingTypeManagementRepo.Add(deviceStreamingTypeManagement);
                result = _deviceStreamingTypeManagementRepo.SaveDeviceStreamingType();
            }
            catch (Exception ex)
            {
                _logger.LogError("AddDeviceStreamTypeDB method failed", ex);
            }
            return result;
        }

        /// <summary>
        /// Update requested user device stream type in the database
        /// </summary>
        /// <param name="deviceStreamingTypeManagement"></param>
        /// <returns></returns>
        private async Task<int> UpdateDeviceStreamTypeDB(DeviceStreamingTypeManagement deviceStreamingTypeManagement)
        {
            int result = 0;

            try
            {
                var updateDeviceStreamType = _deviceStreamingTypeManagementRepo.Query(e => e.LogicalDeviceId == deviceStreamingTypeManagement.LogicalDeviceId
                && e.UserTokenId == deviceStreamingTypeManagement.UserTokenId
                && e.IsUserStreaming == true).FirstOrDefault();

                updateDeviceStreamType.LiveStreamingActive = deviceStreamingTypeManagement.LiveStreamingActive;
                updateDeviceStreamType.PlaybackStreamingActive = deviceStreamingTypeManagement.PlaybackStreamingActive;

                _deviceStreamingTypeManagementRepo.Update(updateDeviceStreamType);
                result = _deviceStreamingTypeManagementRepo.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateDeviceStreamTypeDB method failed", ex);
            }
            return result;
        }

        /// <summary>
        /// If any user exits out of live streaming or plaback screen or closes the App UpdateUserStreamingState API called by Mobile APP
        /// API will reset the streaming status of the user for that device.
        /// </summary>
        /// <param name="UserStreamingState"></param>
        /// <returns></returns>
        [HttpPut("UpdateUserStreamingState")]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MMSHttpReponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserStreamingState([FromHeader(Name = "uid")] string uid, UpdateUserStreamingStateRequestModel UserStreamingState)
        {
            try
            {
                var requestedUserTokenId = Request.Headers.FirstOrDefault(x => x.Key == "uid").Value.FirstOrDefault();
                if (requestedUserTokenId == null)
                {
                    return NotFound(new MMSHttpReponse {ErrorMessage = _configuration.GetValue<string>("UidFieldErrorMessage")});
                }
                var result = await UpdateUserStreamingState(_logger, UserStreamingState.logicalDeviceId, OrgId, requestedUserTokenId);
                if (result)
                {
                    return Ok(new MMSHttpReponse { SuccessMessage = "User Streaming state updated successfully." });
                }
                else
                {
                    return NotFound(new MMSHttpReponse { ErrorMessage = "Requested resoruce not found for update." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured while UpdateUserStreamingState Ex:- {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        private async Task<bool> ResetDeviceStreamingTypeManagement(ILogger log, string logicalDeviceId)
        {
            string sqlConnectionStr = String.Empty;
            try
            {
                sqlConnectionStr = _configuration.GetSection("Sql").GetValue<String>("ConnectionString");

                using (SqlConnection con = new SqlConnection(sqlConnectionStr))
                {
                    using (var command = new SqlCommand(String.Format("update DeviceStreamingTypeManagement set isuserstreaming = 0, LiveStreamingActive= 0, PlaybackStreamingActive = 0 where LogicalDeviceId = '{0}'", logicalDeviceId), con))
                    {
                        con.Open();
                        var result = command.ExecuteNonQuery();
                        log.LogInformation($"ResetDeviceStreamingTypeManagement has been executed successfully. Result: {result}, logicalDeviceId:{logicalDeviceId}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {

                log.LogError($"Exception occured while ResetdeviceStreamingTypeManagement ex :- {ex} ");
                return false;
            }
            #region Commentted Code
            /*using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var userStream = _deviceStreamingTypeManagementRepo.Query(d => d.LogicalDeviceId == logicalDeviceId && d.IsUserStreaming == true);
                    if (userStream.Any())
                    {
                        foreach (var d in userStream)
                        {
                            d.IsUserStreaming = false;
                            d.LiveStreamingActive = false;
                            d.PlaybackStreamingActive = false;
                            d.ModifiedBy = UserEmail;
                            d.ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                            _deviceStreamingTypeManagementRepo.Update(d);
                        }
                        _deviceStreamingTypeManagementRepo.SaveChanges();
                        transaction.Commit();
                    }
                    else
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    log.LogError($"Exception occured while ResetdeviceStreamingTypeManagement ex :- {ex} ");
                    transaction.Rollback();
                    return false;
                }
            }*/
            #endregion
        }

        private async Task<bool> UpdateUserStreamingState(ILogger log, string logicalDeviceId, string OrgId, string requestedUserTokenId)
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var userStream = _deviceStreamingTypeManagementRepo.Query(d => d.LogicalDeviceId == logicalDeviceId && d.OrgId == OrgId && d.UserTokenId == requestedUserTokenId);
                    if (userStream.Any())
                    {
                        foreach (var d in userStream)
                        {
                            d.IsUserStreaming = false;
                            d.LiveStreamingActive = false;
                            d.PlaybackStreamingActive = false;
                            d.ModifiedBy = UserEmail;
                            d.ModifiedDate = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                            _deviceStreamingTypeManagementRepo.Update(d);
                        }
                        _deviceStreamingTypeManagementRepo.SaveChanges();
                        transaction.Commit();
                    }
                    else
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    log.LogError($"Exception occured while ResetdeviceStreamingTypeManagement ex :- {ex} ");
                    transaction.Rollback();
                    return false;
                }
            }
        }
    }
}
