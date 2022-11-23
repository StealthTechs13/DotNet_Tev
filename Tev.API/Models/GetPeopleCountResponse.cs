using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class GetPeopleCountResponse
    {
        public string LocationName { get; set; }
        public string DeviceName { get; set; }
        public List<PeopleCountData> PeopleCountList { get; set; }
    }
    public class PeopleCountData
    {
        public long OccurenceTimestamp { get; set; }
        public int PeopleCount { get; set; }
    }
}
