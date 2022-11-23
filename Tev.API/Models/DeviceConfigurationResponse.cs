using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.API.Models
{
    /// <summary>
    /// Response class for device config data
    /// </summary>
    public class DeviceConfigurationResponse
    {
        /// <summary>
        /// Config related to Crowd feature
        /// </summary>
        public Crowd Crowd { get; set; }

        /// <summary>
        /// Config related to Loiter feature
        /// </summary>
        public LoiterDTO Loiter { get; set; }

        /// <summary>
        /// Config related to trespassing feature, all times are in 24 hours format in IST e.g. "20:30"
        /// </summary>
        public Trespassing Trespassing { get; set; }

        /*
        /// <summary>
        /// API will always return Zone cordinates relative to 640*480. Client to scale up or scale down the cordinates as per their size.
        /// </summary>
        
        */

        /// <summary>
        /// Buzzer On Off Config
        /// </summary>
        public bool? BuzzerControl { get; set; } = false;

        /// <summary>
        /// Sensitivity for Person Detection.
        /// </summary>
        public float PersonDetectionSensitivity { get; set; } = 0.5f;
    }

    public class LoiterDTO
    {
        /// <summary>
        /// Loiter time in minutes
        /// </summary>
        public int Time { get; set; }

        public bool SchedulingSupported { get; set; }

        public List<RecordSchedule> LoiterSchedule { get; set; }

    }
}
