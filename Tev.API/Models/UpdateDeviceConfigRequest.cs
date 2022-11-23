using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.API.Models
{
    public class UpdateDeviceConfigRequest
    {
        /// <summary>
        /// Tresspassing Start Time in IST in AM PM format e.g "8:30 PM"
        /// </summary>
        public string TrespassingStartTime { get; set; }
        /// <summary>
        /// Tresspassing End Time in IST in AM PM format e.g "6:30 AM"
        /// </summary>
        public string TrespassingEndTime { get; set; }
        /// <summary>
        /// Timespan in minutes after which loiter alert will be triggered
        /// If a person stays in frame for n minutes then after n minutes only loiter alert will be triggered. Before n minutes there wont be loiter alert
        /// </summary>
        public int LoiterTime { get; set; }
        /// <summary>
        /// Crowd Person Limit
        /// </summary>
        public int CrowdPersonLimit { get; set; }

        /// <summary>
        /// Buzzer On Off Config
        /// </summary>
        public bool BuzzerControl { get; set; } = false;

        /// <summary>
        /// Sensitivity for Person Detection.
        /// </summary>
        public float PersonDetectionSensitivity { get; set; } = 0.5f;

        public List<RecordSchedule> TrespassingSchedule { get; set; }

        public List<RecordSchedule> LoiterSchedule { get; set; }

        public List<RecordSchedule> CrowdSchedule { get; set; }

    }
}
