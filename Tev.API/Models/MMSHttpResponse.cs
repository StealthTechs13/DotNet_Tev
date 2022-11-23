using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class MMSHttpReponse<T>
    {
        [JsonProperty("body")]
        public T ResponseBody { get; set; }
        [JsonProperty("successMessage")]
        public string SuccessMessage { get; set; }
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
    public class MMSHttpReponse
    {
        [JsonProperty("successMessage")]
        public string SuccessMessage { get; set; }
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
