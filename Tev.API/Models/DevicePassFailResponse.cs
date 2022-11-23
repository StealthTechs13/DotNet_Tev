using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class DevicePassFailResponse
    {
        public int TodayPass { get; set; }
        public int TodayFail { get; set; }
        public int TotalPass { get; set; }
        public int TotalFail { get; set; }

        public string TodayFirstSrNo { get; set; }

        public string TodayLastSrNo { get; set; }

    }
}
