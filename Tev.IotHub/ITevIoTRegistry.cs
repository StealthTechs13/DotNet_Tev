using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using MMSConstants;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tev.IotHub.Models;


namespace Tev.IotHub
{
    public interface ITevIoTRegistry
    {
        Task<Twin> GetDeviceTwin(string deviceId);
        Task<Twin> UpdateTwin(string expirydate, bool isActive, string deviceId, string subscriptionId, int[] addons, string status, SubscriptionEventType eventType);
        Task<Twin> UpdateDeviceFeatureConfiguration(string deviceId, string trespassStartTime, string trespassEndTime, int trespassInterval, int crowdPersonLimit,bool buzzerControl = false, float personDetectionSensitivity = 0.5f);
        Task<Twin> UpdateDeviceFeatureConfigurationAndSchedule(FormattedUpdateDeviceConfigRequest ValidReq);
        Task<Twin> UpdateDeviceSubscriptionStatus(string deviceId, string status);
        Task<Twin> UpdateNameOrLocation( string deviceId, string name="", string locationId="", string locationName="");
        Task<TevDeviceExtension> GetDeviceById(string deviceId);
        void UpdateDeviceTwinPropertyLocation(string locationId, string locationName);
        Task DeleteDeviceFromDeviceTwin(string deviceId);
        Task<bool> UpdateLiveStreamingProperty(string deviceId, bool inputStream, string inputStreamName, string inputAccessKey, 
        string inputAwsRegion, string inputSecretKey,long seqNumber,long autoStopSeqNumber);
        Task SendDataToDevice(string deviceId, WSDTestData data);
        Task<bool> UpdateFirmware(string deviceId);
        Task<bool> UpdateZoneFencing(string deviceId, bool enabled, Zone z);
        Task<bool> AttachSubscriptionToDevice(string expirydate, bool isActive, string deviceId, string subscriptionId, int[] addons, string status, SubscriptionEventType eventType);
        Task<CloudToDeviceMethodResult> InvokeDeviceDirectMethodAsync(string deviceId, string directMethodName, int timeoutInSec, ILogger log, string jsonMethodParam = null);
       
        Task<Twin> UpdateDeviceVideoResolution(string deviceId,string methodName, string resolution);
        Task<Twin> UpdateSDCardRecordSettings(string deviceId, string method_name, bool eventRecording, bool trespassing, bool crowd, bool loiter,bool fifo, ILogger log, string manualRecording = null, List<Schedule> recordSchedules=null);
        Task<Twin> UpdateCameraSettings(string deviceId, bool fifo, int recordingType, string resolution, ILogger log, List<Schedule> recordSchedules = null);
        Task<Twin> UpdateVideoFilename(string deviceId, string filename);

    }
}
