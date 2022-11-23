using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class GetAlertsRequest
    {
        /// <summary>
        /// Number of alerts to fetch
        /// </summary>
        [Required]
        public int Take { get; set; }

        /// <summary>
        /// Number of alerts to skip
        /// </summary>
        [Required]
        public int Skip { get; set; }

        /// <summary>
        /// Filters by akcnowledge status of the alerts,  do not include in request body if acknowledge status filtering is not required
        /// </summary>
        public bool? Acknowledged { get; set; }

        /// <summary>
        /// Filters by bookmarked status of the alerts,  do not include in request body if bookmark status filtering is not required
        /// </summary>
        public bool? IsBookMarked { get; set; }

        /// <summary>
        /// Alert types e.g, crowd, crouch etc, do not include in request body if type filtering is not required
        /// </summary>
        public List<int> AlertType { get; set; }

        /// <summary>
        /// Location id for which alert is required, do not include in request body if location filtering is not required
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// Device id for which alert is required, do not include in request body if device filtering is not required
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Filters by IsCorrect status of the alerts,  do not include in request body if IsCorrect status filtering is not required
        /// </summary>
        public bool? IsCorrect { get; set; }

        /// <summary>
        /// Start date in epoch time seconds, it should be epoch time corresponding to 00:00:00 hrs of the selected date
        /// </summary>
        public long? StartDate { get; set; }

        /// <summary>
        /// End date in epoch time seconds, it should be epoch time corresposing to 23:59:59 hrs of the selected date
        /// </summary>
        public long? EndDate { get; set; }

        /// <summary>
        /// The application for which alerts is required, defaults to TEV
        /// </summary>
        public Applications? Device { get; set; }
    }
}