using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using MMSConstants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tev.IotHub.Models;

namespace Tev.IotHub
{
    public class TevIoTRegistry : ITevIoTRegistry
    {
        private readonly RegistryManager registry;
        private readonly string connectionString;
        private ServiceClient s_serviceClient;

        public TevIoTRegistry(string conString)
        {
            this.registry = RegistryManager.CreateFromConnectionString(conString);
            this.connectionString = conString;
            this.s_serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        public async Task<TevDeviceExtension> GetDeviceById(string deviceId)
        {
            var devices = new TevDeviceExtension();
            if (!string.IsNullOrEmpty(deviceId))
            {
                var sqlQuery = $"select deviceId as actualDeviceId, properties.reported.deviceType as deviceType, " +
                    $"properties.reported.logicalDeviceId as deviceId, properties.desired.deleted as disabled," +
                    $"properties.reported.device_deleted as deleted, properties.reported.deviceName as deviceName, " +
                    $"properties.reported.locationName as locationName, " +
                    $"properties.desired.live_streaming.stream as isLiveStreaming, " +
                    $"tags.subscriptionId as subscriptionId,tags.status as SubscriptionStatus, " +
                    $"properties.desired.subscription.expiry as subscriptionExpiryDate,properties.reported.orgId as orgId," +
                    $"properties.reported.firmware.currentFirmwareVersion as firmwareVersion," +
                    $"properties.desired.firmware.newFirmwareVersion as NewFirmwareVersion, " +
                    $"properties.desired.firmware.userApproved as UserApproved " +
                    $"from devices where properties.reported.logicalDeviceId ='{deviceId}'";

                var query = registry.CreateQuery(sqlQuery);
                while (query.HasMoreResults)
                {
                    var result = (await query.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                    if (result == null)
                    {
                        return devices;
                    }
                    devices = JsonConvert.DeserializeObject<TevDeviceExtension>(result);
                }
            }
            return devices;
        }

        public async Task<Twin> GetDeviceTwin(string deviceId)
        {
            var result = await registry.GetTwinAsync(deviceId).ConfigureAwait(false);
            return result;
        }
        //Done
        public async Task<Twin> UpdateTwin(string expirydate, bool isActive, string deviceId, string subscriptionId, int[] addons, string status, SubscriptionEventType eventType)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            if(addons.Contains(2))
            {
                addons = addons.Where(val => val != 2).ToArray(); // removing addon number 2 which is used for Last 7 days cloud storage for Alert.
            }
            Guard.NotNull(twin, nameof(twin));

            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                switch (eventType)
                {
                    case SubscriptionEventType.subscription_renewed:
                        sb.properties.desired.subscription = new Subscription
                        {
                            isActive = isActive,
                            expiry = Convert.ToDateTime(expirydate).ToString("yyyy-MM-dd"),
                            features = addons
                        };
                        sb.tags = new Tag
                        {
                            subscriptionId = subscriptionId,
                            status = status,
                            twinChangeStatus = TwinChangeStatus.SubscriptionRenewed
                        };
                        break;
                    case SubscriptionEventType.subscription_activation:
                        sb.properties.desired.subscription = new Subscription
                        {
                            isActive = isActive,
                            expiry = Convert.ToDateTime(expirydate).ToString("yyyy-MM-dd"),
                            features = addons
                        };
                        sb.tags = new Tag
                        {
                            subscriptionId = subscriptionId,
                            status = status,
                            twinChangeStatus = TwinChangeStatus.NewSubscription
                        };
                        break;
                    case SubscriptionEventType.subscription_upgraded:
                        sb.properties.desired.subscription = new Subscription
                        {
                            isActive = isActive,
                            expiry = Convert.ToDateTime(expirydate).ToString("yyyy-MM-dd"),
                            features = addons
                        };
                        sb.tags = new Tag
                        {
                            subscriptionId = subscriptionId,
                            status = status,
                            twinChangeStatus = TwinChangeStatus.UpgradeSubscription
                        };
                        break;
                    case SubscriptionEventType.subscription_downgraded:
                        sb.properties.desired.subscription = new Subscription
                        {
                            isActive = isActive,
                            expiry = Convert.ToDateTime(expirydate).ToString("yyyy-MM-dd"),
                            features = addons
                        };
                        sb.tags = new Tag
                        {
                            subscriptionId = subscriptionId,
                            status = status,
                            twinChangeStatus = TwinChangeStatus.DowngradeSubscription
                        };
                        break;
                    case SubscriptionEventType.subscription_scheduled_cancellation_removed:
                        sb.properties.desired.subscription = new Subscription
                        {
                            isActive = isActive,
                            expiry = Convert.ToDateTime(expirydate).ToString("yyyy-MM-dd"),
                            features = addons
                        };
                        sb.tags = new Tag
                        {
                            subscriptionId = subscriptionId,
                            status = status,
                            twinChangeStatus = TwinChangeStatus.SubscriptionReactivated
                        };
                        break;
                    case SubscriptionEventType.subscription_modified:
                        sb.properties.desired.subscription = new Subscription
                        {
                            isActive = isActive,
                            expiry = Convert.ToDateTime(expirydate).ToString("yyyy-MM-dd"),
                            features = addons
                        };
                        sb.tags = new Tag
                        {
                            subscriptionId = subscriptionId,
                            status = status,
                            twinChangeStatus = TwinChangeStatus.SubscriptionModified
                        };
                        break;
                    default:
                        break;
                }

                var patch = JsonConvert.SerializeObject(sb);
                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                return result;
            }
            else
            {
                return null;
            }

        }
        //Done
        public async Task<Twin> UpdateNameOrLocation(string deviceId, string name = "", string locationId = "", string locationName = "")
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            Guard.NotNull(twin, nameof(twin));

            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                if (sb.tags == null)
                {
                    sb.tags = new Tag
                    {
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }
                else
                {
                    sb.tags = new Tag
                    {
                        subscriptionId = sb.tags.subscriptionId,
                        status = sb.tags.status,
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }

                if (!string.IsNullOrEmpty(name))
                {
                    sb.properties.desired.deviceName = name;
                }
                if (!string.IsNullOrEmpty(locationId))
                {
                    sb.properties.desired.locationId = locationId;
                    sb.properties.desired.locationName = locationName;
                }

                var patch = JsonConvert.SerializeObject(sb);

                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                return result;
            }
            else
            {
                return null;
            }
        }
        //Done
        public async Task<Twin> UpdateDeviceFeatureConfiguration(string deviceId, string trespassStartTime, string trespassEndTime, int loiterInterval, int crowdPersonLimit, bool buzzerControl = false, float personDetectionSensitivity = 0.5f)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            Guard.NotNull(twin, nameof(twin));

            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                if (sb.tags == null)
                {
                    sb.tags = new Tag
                    {
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }
                else
                {
                    sb.tags = new Tag
                    {
                        subscriptionId = sb.tags.subscriptionId,
                        status = sb.tags.status,
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }

                //sb.properties.desired.featureConfig = new FeatureConfig
                //{
                //    crowd = new Crowd { crowdLimit = crowdPersonLimit },
                //    trespassing = new Trespassing {
                //        trespassingStartTime = trespassStartTime,
                //        trespassingEndTime = trespassEndTime,
                //    },
                //    loiter=new Loiter { time=loiterInterval},
                //    buzzerControl = buzzerControl,
                //};

                sb.properties.desired.featureConfig.crowd = new Crowd { crowdLimit = crowdPersonLimit };
                sb.properties.desired.featureConfig.trespassing = new Trespassing
                {
                    trespassingStartTime = trespassStartTime,
                    trespassingEndTime = trespassEndTime,
                };
                sb.properties.desired.featureConfig.loiter = new Loiter { time = loiterInterval };
                sb.properties.desired.featureConfig.buzzerControl = buzzerControl;
                sb.properties.desired.featureConfig.score_val = personDetectionSensitivity;

                var patch = JsonConvert.SerializeObject(sb);

                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                return result;
            }
            else { return null; }

        }
        //Done
        public async Task<Twin> UpdateDeviceSubscriptionStatus(string deviceId, string status)
        {
            try
            {
                var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
                var resultQry = registry.CreateQuery(query);
                Twin twin = null;
                while (resultQry.HasMoreResults)
                {
                    var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                    twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
                }
                Guard.NotNull(twin, nameof(twin));
                if (twin != null)
                {
                    var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                    switch (status)
                    {
                        case nameof(SubscriptionStatus.cancelled):
                            sb.tags = new Tag
                            {
                                subscriptionId = sb.tags.subscriptionId,
                                status = status,
                                twinChangeStatus = TwinChangeStatus.SubscriptionCancelled
                            };
                            sb.properties.desired.subscription = new Subscription()
                            {
                                isActive = false,
                                features = sb.properties.desired.subscription.features,
                                expiry = sb.properties.desired.subscription.expiry
                            };
                            break;
                        case nameof(SubscriptionStatus.non_renewing):
                            sb.tags = new Tag
                            {
                                subscriptionId = sb.tags.subscriptionId,
                                status = status,
                                twinChangeStatus = TwinChangeStatus.SubscriptionCancellationScheduled
                            };
                            break;
                        case nameof(SubscriptionStatus.live):
                            sb.tags = new Tag
                            {
                                subscriptionId = sb.tags.subscriptionId,
                                status = status,
                                twinChangeStatus = TwinChangeStatus.SubscriptionReactivated
                            };
                            break;
                        case nameof(SubscriptionStatus.expired):
                            sb.tags = new Tag
                            {

                                subscriptionId = sb.tags.subscriptionId,
                                status = status,
                                twinChangeStatus = TwinChangeStatus.SubscriptionReactivated
                            };
                            sb.properties.desired.subscription.isActive = false;
                            break;
                        default:
                            break;
                    }


                    var patch = JsonConvert.SerializeObject(sb);
                    var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                    return result;
                }
                else { return null; }
            }
            //try catch is implemented only for a temporary null check till device delete functionality is complete , will be removed later.
            catch (NullReferenceException ex)
            {
                return null;
            }
            catch (ArgumentNullException ex)
            {
                return null;
            }
        }
        //Done
        public async void UpdateDeviceTwinPropertyLocation(string locationId, string locationName)
        {

            if (!string.IsNullOrEmpty(locationId) && !string.IsNullOrEmpty(locationName))
            {
                var sqlQuery = $"select deviceId from devices where properties.reported.locationId  ='{locationId}'";
                var query = registry.CreateQuery(sqlQuery);
                Twin twin = null;

                while (query.HasMoreResults)
                {
                    var result = await query.GetNextAsJsonAsync().ConfigureAwait(false);
                    foreach (var item in result)
                    {
                        var device = JsonConvert.DeserializeObject<TevDevice>(item);
                        twin = await GetDeviceTwin(device.DeviceId).ConfigureAwait(false);
                        Guard.NotNull(twin, nameof(twin));

                        var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                        if (sb.tags == null)
                        {
                            sb.tags = new Tag
                            {
                                twinChangeStatus = TwinChangeStatus.Default
                            };
                        }
                        else
                        {
                            sb.tags = new Tag
                            {
                                subscriptionId = sb.tags.subscriptionId,
                                status = sb.tags.status,
                                twinChangeStatus = TwinChangeStatus.Default
                            };
                        }

                        sb.properties.desired.locationName = locationName;

                        var patch = JsonConvert.SerializeObject(sb);

                        await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task DeleteDeviceFromDeviceTwin(string deviceId)
        {

            await registry.RemoveDeviceAsync(deviceId).ConfigureAwait(false);

        }
        //Done
        public async Task<bool> UpdateLiveStreamingProperty(string deviceId, bool inputStream, string inputStreamName, string inputAccessKey,
            string inputAwsRegion, string inputSecretKey, long seqNumber, long autoStopSeqNumber)
        {
            if (!string.IsNullOrEmpty(deviceId))
            {
                var sqlQuery = $"select deviceId from devices where properties.reported.logicalDeviceId  ='{deviceId}'";
                var query = registry.CreateQuery(sqlQuery);
                Twin twin = null;

                if (query.HasMoreResults)
                {
                    var result = await query.GetNextAsJsonAsync().ConfigureAwait(false);
                    var item = result.FirstOrDefault();
                    if (item != null)
                    {
                        var device = JsonConvert.DeserializeObject<TevDevice>(item);
                        twin = await GetDeviceTwin(device.DeviceId).ConfigureAwait(false);
                        Guard.NotNull(twin, nameof(twin));

                        var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                        if (sb.properties.desired.live_streaming == null || sb.properties.desired.live_streaming.stream == false || inputStream == false)
                        {
                            if (sb.tags == null)
                            {
                                sb.tags = new Tag
                                {
                                    twinChangeStatus = TwinChangeStatus.LiveStreaming
                                };
                            }
                            else
                            {
                                sb.tags = new Tag
                                {
                                    subscriptionId = sb.tags.subscriptionId,
                                    status = sb.tags.status,
                                    twinChangeStatus = TwinChangeStatus.LiveStreaming
                                };
                            }

                            sb.properties.desired.live_streaming = new LiveStreaming
                            {
                                stream = inputStream,
                                stream_name = inputStreamName,
                                secret_key = inputSecretKey,
                                access_key = inputAccessKey,
                                aws_region = inputAwsRegion,
                                servicebus_sequence_number = seqNumber,
                                auto_stop_servicebus_seq_num = autoStopSeqNumber

                            };

                            var patch = JsonConvert.SerializeObject(sb);

                            await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task SendDataToDevice(string deviceId, WSDTestData data)
        {
            var device = await GetDeviceById(deviceId).ConfigureAwait(false);
            using (ServiceClient client = ServiceClient.CreateFromConnectionString(connectionString))
            {
                if (data != null)
                {
                    var msg = new Message();
                    msg.Properties.Add(nameof(data.GTemperatureSensorOffset), data.GTemperatureSensorOffset.ToString());
                    msg.Properties.Add(nameof(data.GTemperatureSensorOffset2), data.GTemperatureSensorOffset2.ToString());
                    msg.Properties.Add(nameof(data.ClearAir), data.ClearAir.ToString());
                    msg.Properties.Add(nameof(data.IREDCalibration), data.IREDCalibration.ToString());
                    msg.Properties.Add(nameof(data.PhotoOffset), data.PhotoOffset.ToString());
                    msg.Properties.Add(nameof(data.DriftLimit), data.DriftLimit.ToString());
                    msg.Properties.Add(nameof(data.DriftBypass), data.DriftBypass.ToString());
                    msg.Properties.Add(nameof(data.TransmitResolution), data.TransmitResolution.ToString());
                    msg.Properties.Add(nameof(data.TransmitThreshold), data.TransmitThreshold.ToString());
                    msg.Properties.Add(nameof(data.SmokeThreshold), data.SmokeThreshold.ToString());
                    msg.Properties.Add(nameof(data.TestId), data.TestId.ToString());
                    await client.SendAsync(device.ActualDeviceId, msg).ConfigureAwait(false);
                }

            }
        }
        //Done
        public async Task<bool> UpdateFirmware(string deviceId)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }

            Guard.NotNull(twin, nameof(twin));

            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                sb.properties.desired.firmware.userApproved = true;

                if (sb.tags == null)
                {
                    sb.tags = new Tag
                    {
                        twinChangeStatus = TwinChangeStatus.DesiredPropFirmwareUpdate
                    };
                }
                else
                {
                    sb.tags = new Tag
                    {

                        subscriptionId = sb.tags.subscriptionId,
                        status = sb.tags.status,
                        twinChangeStatus = TwinChangeStatus.DesiredPropFirmwareUpdate
                    };
                }

                var patch = JsonConvert.SerializeObject(sb);

                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);

                return true;
            }
            return false;
        }
        //Done
        public async Task<bool> UpdateZoneFencing(string deviceId, bool enabled, Zone z)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }

            Guard.NotNull(twin, nameof(twin));

            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                if (sb.tags == null)
                {
                    sb.tags = new Tag
                    {
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }
                else
                {
                    sb.tags = new Tag
                    {
                        subscriptionId = sb.tags.subscriptionId,
                        status = sb.tags.status,
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }

                sb.properties.desired.featureConfig.zoneFencingEnabled = enabled;
                sb.properties.desired.featureConfig.zone = z;

                var patch = JsonConvert.SerializeObject(sb);


                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);

                return true;
            }
            return false;
        }
        //Done
        public async Task<bool> AttachSubscriptionToDevice(string expirydate, bool isActive, string deviceId, string subscriptionId, int[] addons, string status, SubscriptionEventType eventType)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }

            Guard.NotNull(twin, nameof(twin));

            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                sb.properties.desired.subscription = new Subscription
                {
                    isActive = isActive,
                    expiry = Convert.ToDateTime(expirydate).ToString("yyyy-MM-dd"),
                    features = addons
                };
                sb.tags = new Tag
                {
                    subscriptionId = subscriptionId,
                    status = status,
                    twinChangeStatus = TwinChangeStatus.DeviceSwitched
                };

                var patch = JsonConvert.SerializeObject(sb);
                await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public async Task<CloudToDeviceMethodResult> InvokeDeviceDirectMethodAsync(string deviceId, string directMethodName, int timeoutInSec, ILogger log, string jsonMethodParam = null)
        {
            var methodInvocation = new CloudToDeviceMethod(directMethodName)
            {
                ResponseTimeout = TimeSpan.FromSeconds(timeoutInSec),
            };
            if (!string.IsNullOrEmpty(jsonMethodParam))
            {
                methodInvocation.SetPayloadJson(jsonMethodParam);
            }
            // Invoke the direct method asynchronously and get the response from the device..
            log.LogInformation($"Device_Id {deviceId} RequestParam :- {jsonMethodParam}");
            var T1 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            var response = await s_serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
            var T2 = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            var diff = T2 - T1;
            log.LogInformation($"Time taken in seconds for direct method {directMethodName} is {diff} and T1 is {T1} T2 is {T2}");
            return response;
        }

        public async Task<Twin> UpdateDeviceFeatureConfigurationAndSchedule(FormattedUpdateDeviceConfigRequest ValidReq)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{ValidReq.logicalDeviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            Guard.NotNull(twin, nameof(twin));

            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());

                if (sb.tags == null)
                {
                    sb.tags = new Tag
                    {
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }
                else
                {
                    sb.tags = new Tag
                    {
                        subscriptionId = sb.tags.subscriptionId,
                        status = sb.tags.status,
                        twinChangeStatus = TwinChangeStatus.Default
                    };
                }
                sb.properties.desired.featureConfig.crowd = new Crowd { crowdLimit = ValidReq.CrowdPersonLimit ,

                    CrowdSchedule = ValidReq.CrowdSchedule

                };
                sb.properties.desired.featureConfig.trespassing = new Trespassing
                {
                    trespassingStartTime = ValidReq.TrespassingStartTime,
                    trespassingEndTime = ValidReq.TrespassingEndTime,
                    TrespassingSchedule = ValidReq.TrespassingSchedule
                };
                sb.properties.desired.featureConfig.loiter = new Loiter { time = ValidReq.LoiterTime, LoiterSchedule = ValidReq.LoiterSchedule };
                sb.properties.desired.featureConfig.buzzerControl = ValidReq.BuzzerControl;
                sb.properties.desired.featureConfig.score_val = ValidReq.PersonDetectionSensitivity;

                var patch = JsonConvert.SerializeObject(sb);

                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                return result;
            }
            else { return null; }

        }

        public async Task<Twin> UpdateDeviceVideoResolution(string deviceId, string methodName, string resolution)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            Guard.NotNull(twin, nameof(twin));
            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());
                switch (methodName)
                {
                    case "hls_livestream_resolution":
                        {
                            sb.properties.desired.featureConfig.videoResolution.hls = resolution;
                        }
                        break;
                    case "webRtc_livestream_resolution":
                        {
                            sb.properties.desired.featureConfig.videoResolution.webRtc = resolution;
                        }
                        break;
                    case "sdCard_stream_resolution":
                        {
                            sb.properties.desired.sdCard.resolution = resolution;
                        }
                        break;
                    default:
                        return null;
                }
                var patch = JsonConvert.SerializeObject(sb);

                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                return result;
            }
            else { return null; }
        }

        public async Task<Twin> UpdateSDCardRecordSettings(string deviceId, string method_name, bool rec, bool trespassing, bool crowd, bool loiter, bool fifo, ILogger log, string manualRecording = null, List<Schedule> recordSchedules = null)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            Guard.NotNull(twin, nameof(twin));
            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());
                if (sb.properties.desired.sdCard != null)
                {
                    switch (method_name)
                    {
                        case "clearCameraSettings":
                            {
                                sb.properties.desired.sdCard.recordingType = 0;
                                //sb.properties.desired.sdCard.fifo = 0;
                                sb.properties.desired.sdCard.resolution = "480p";
                                break;
                            }
                        case "setFifo":
                            {
                                sb.properties.desired.sdCard.fifo = fifo ? 1 : 0;
                                break;
                            }
                        case "schedule":
                            {
                                int i = 0;
                                if (sb.properties.desired.sdCard.recordingSchedule != null)
                                {
                                    foreach (var item in recordSchedules)
                                    {
                                        sb.properties.desired.sdCard.recordingSchedule[i].enabled = item.enabled;
                                        sb.properties.desired.sdCard.recordingSchedule[i].fullday = item.fullday;
                                        int j = 0;
                                        if (item.times != null && item.times.Count > 0)
                                        {
                                            foreach (var tm in item.times)
                                            {
                                                sb.properties.desired.sdCard.recordingSchedule[i].times[j] = tm;
                                                j++;
                                            }
                                        }
                                        i++;
                                    }
                                }
                                else
                                {
                                    List<IotHub.Models.Schedule> iot_Schedulelist = new List<IotHub.Models.Schedule>();
                                    for (int k = 0; k < 7; k++)
                                    {
                                        IotHub.Models.Schedule schd = new Schedule();
                                        List<IotHub.Models.time> tmLst = new List<IotHub.Models.time>();
                                        for (int t = 0; t < 3; t++)
                                        {
                                            IotHub.Models.time tm = new time()
                                            {
                                                st = "11:00",
                                                et = "13:00",
                                            };
                                            tmLst.Add(tm);
                                        }
                                       
                                        schd.enabled = false;
                                        schd.fullday = false;
                                        schd.times = tmLst;
                                        iot_Schedulelist.Add(schd);
                                    }
                                    sb.properties.desired.sdCard.recordingSchedule = iot_Schedulelist;
                                    foreach (var item in recordSchedules)
                                    {
                                        sb.properties.desired.sdCard.recordingSchedule[i].enabled = item.enabled;
                                        sb.properties.desired.sdCard.recordingSchedule[i].fullday = item.fullday;
                                        int j = 0;
                                        if (item.times != null && item.times.Count > 0)
                                        {
                                            foreach (var tm in item.times)
                                            {
                                                sb.properties.desired.sdCard.recordingSchedule[i].times[j] = tm;
                                                j++;
                                            }
                                        }
                                        i++;
                                    }
                                }
                                sb.properties.desired.sdCard.recordingType = 2;

                                break;
                            }
                        case "event":
                            {
                                if (rec)
                                {
                                    EventRecording eventRecording = new EventRecording();
                                    eventRecording.eventList = new List<int>();
                                    if (loiter)
                                        eventRecording.eventList.Add(3);
                                    if (crowd)
                                        eventRecording.eventList.Add(1);
                                    if (trespassing)
                                        eventRecording.eventList.Add(7);

                                    sb.properties.desired.sdCard.recordingType = 1;
                                    sb.properties.desired.sdCard.eventRecording = eventRecording;
                                }
                                else
                                {
                                    sb.properties.desired.sdCard.recordingType = 0;
                                }
                                
                            }
                            break;
                        case "manual":
                            {
                                if (sb.properties.desired.sdCard.recordingType == 3)
                                {
                                    if (manualRecording == "stop")
                                    {
                                        sb.properties.desired.sdCard.recordingType = 0;
                                    }
                                }
                                else
                                {
                                    if (manualRecording == "start")
                                    {
                                        sb.properties.desired.sdCard.recordingType = 3;
                                    }
                                }
                            }
                            break;
                        case "offline":
                            {
                                if (rec)
                                {
                                    sb.properties.desired.sdCard.recordingType = 4;
                                }
                                else
                                {
                                    sb.properties.desired.sdCard.recordingType = 0;
                                }
                               
                            }
                            break;
                        default:
                            return null;
                    }
                    var patch = JsonConvert.SerializeObject(sb);
                    try
                    {
                        var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        log.LogInformation($"Device_Id {deviceId} Etag :- {twin.ETag}");
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public async Task<Twin> UpdateCameraSettings(string deviceId, bool fifo, int recordingType, string resolution, ILogger log, List<Schedule> recordSchedules = null)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            Guard.NotNull(twin, nameof(twin));
            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());
                if (sb.properties.desired.sdCard != null)
                {
                    sb.properties.desired.sdCard.resolution = resolution;
                    sb.properties.desired.sdCard.recordingType = recordingType;
                    sb.properties.desired.sdCard.fifo = fifo ? 1 : 0;
                    int i = 0;
                    foreach (var item in recordSchedules)
                    {
                        sb.properties.desired.sdCard.recordingSchedule[i].enabled = item.enabled;
                        sb.properties.desired.sdCard.recordingSchedule[i].fullday = item.fullday;
                        int j = 0;
                        if (item.times != null && item.times.Count > 0)
                        {
                            foreach (var tm in item.times)
                            {
                                sb.properties.desired.sdCard.recordingSchedule[i].times[j] = tm;
                                j++;
                            }
                        }
                        i++;
                    }
                    var patch = JsonConvert.SerializeObject(sb);
                    try
                    {
                        var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        log.LogInformation($"Device_Id {deviceId} Etag :- {twin.ETag}");
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else { return null; }
        }

        public async Task<Twin> UpdateVideoFilename(string deviceId, string filename)
        {
            var query = $"select deviceId from devices where properties.reported.logicalDeviceId='{deviceId}'";
            var resultQry = registry.CreateQuery(query);
            Twin twin = null;
            while (resultQry.HasMoreResults)
            {
                var tb = (await resultQry.GetNextAsJsonAsync().ConfigureAwait(false)).FirstOrDefault();
                twin = await GetDeviceTwin(JsonConvert.DeserializeObject<TevDevice>(tb).DeviceId).ConfigureAwait(false);
            }
            Guard.NotNull(twin, nameof(twin));
            if (twin != null)
            {
                var sb = JsonConvert.DeserializeObject<DeviceTwinProperty>(twin.ToJson());
                if (sb.properties.desired.sdCard != null)
                    sb.properties.desired.sdCard.playbackVideo = filename;

                var patch = JsonConvert.SerializeObject(sb);

                var result = await registry.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag).ConfigureAwait(false);
                return result;
            }
            else { return null; }
        }
    }
}
