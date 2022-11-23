using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class TechnicianModel
    {
        [JsonProperty("technicianId")]
        public string TechnicianId { get; set; }
        [Required(ErrorMessage = "Technician name is Required")]
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("phone")]
        public string Phone { get; set; }
        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [Required(ErrorMessage = "Technician type is Required")]
        [JsonProperty("technicianType")]
        public string TechnicianType { get; set; }
        [JsonProperty("longitude")]
        public string Longitude { get; set; }
        [JsonProperty("deviceTypes")]
        public List<string> DeviceTypes { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }

    public class BasePaginatedReq
    {
        /// <summary>
        /// Search term pagination response of technicians
        /// </summary>
        [JsonProperty("search")]
        public string Search { get; set; }
        /// <summary>
        /// Page number
        /// </summary>
        [JsonProperty("pageNo")]
        public int PageNo { get; set; }
        /// <summary>
        /// PageSize
        /// </summary>
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        [JsonProperty("sortBy")]
        public string SortBy { get; set; }
        [JsonProperty("sortOrder")]
        public string SortOrder { get; set; }
        public BasePaginatedReq()
        {
            SortBy = "ModifiedBy";
            SortOrder = "desc";
            PageNo = 1;
            PageSize = 10;
        }
    }

    public class GetAllTechnicianPaginateReq : BasePaginatedReq
    {
        public GetAllTechnicianPaginateReq() : base()
        {

        }
    }
    public class GetAllPaginateResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; }
        [JsonProperty("pageNo")]
        public int PageNo { get; set; }
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        [JsonProperty("sortBy")]
        public string SortBy { get; set; }
        [JsonProperty("sortOrder")]
        public string SortOrder { get; set; }
        [JsonProperty("allRecordCount")]
        public int AllRecordCount { get; set; }
        public GetAllPaginateResponse()
        {
            SortBy = "ModifiedBy";
            SortOrder = "desc";
            Data = new List<T>();
        }
    }
}
