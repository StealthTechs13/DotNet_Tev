using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class PlayBackResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<Grid24Hour> data { get; set; }
        public List<EntityLst> entity { get; set; }
    }
    public class Grid24Hour
    {
        public string start_time { get; set; }
        public string end_time { get; set; }
        public int type { get; set; }
    }
    public class EntityLst
    {
        public string date { get; set; }
        public int count { get; set; }
    }

  

    
}
