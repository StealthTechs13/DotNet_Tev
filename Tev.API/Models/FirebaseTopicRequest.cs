using MMSConstants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class FirebaseTopicRequest
    {
        [Required]
        public string FCMToken { get; set; }
        [Required]
        public Applications Application { get; set; }
    }
}
