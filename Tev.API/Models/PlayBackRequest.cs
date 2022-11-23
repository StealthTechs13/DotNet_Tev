using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class PlayBackRequest
    {
        public string device_id { get; set; }
        public string date { get; set; }
        public Filter filter { get; set; }
    }

    public class Filter
    {
        public bool trespassing { get; set; }
        public bool crowd { get; set; }
        public bool loitering { get; set; }
        public bool manual { get; set; }
        public bool offline { get; set; }
        public bool schedule { get; set; }
        public bool all { get; set; }
    }

    //public class EventFilter
    //{
    //    public bool trespassing { get; set; }
    //    public bool crowd { get; set; }
    //    public bool loitering { get; set; }
    //}
    //public class Request24grid
    //{
    //    public string device_id { get; set; }
    //    public string date { get; set; }
    //    public FilterData filter { get; set; }
    //}
    //public class FilterData
    //{
       
    //}

}
