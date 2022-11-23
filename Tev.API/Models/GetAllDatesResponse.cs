using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class GetAllDatesResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<EntityObj> entity { get; set; }
    }
    public class EntityObj
    {
        public string year { get; set; }
        public List<MonthObj> months { get; set; }
    }
    public class MonthObj
    {
        public string month { get; set; }
        public List<DateObj> dates { get; set; }
    }
    public class DateObj
    {
        public string date { get; set; }
        public int count { get; set; }
    }
}
