using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class CopyCameraSettingResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<CameraSettingData> data { get; set; }
    }

    public class CameraSettingData
    {

    }
}
