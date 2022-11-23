using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Tev.API.Models
{
    public class FirebaseNotifModel 
    {
        public  string Title { get; set; } 
        public  string Condition { get; set; }
        public  string Body { get; set; }
        public  string ImageUrl { get; set; }
        public string DeviceId { get; set; }
        public string UserTokenId { get; set; }
    }
}
