using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class AddLocationRequest
    {
        /// <summary>
        /// Location name , maxlength 300 charcters
        /// </summary>
        [Required]
        [MaxLength(300,ErrorMessage ="Location name has to be 300 or less characters")]
        [RegularExpression(@"^[a-zA-Z0-9'' ']+$", ErrorMessage = "The special characters are not allowed in Location name")]
        public string LocationName { get; set; }
    }

    public class UpdateLocationRequest
    {
        [Required(ErrorMessage = "Location Name is required")]
        [MaxLength(300, ErrorMessage = "Location name has to be 300 or less characters")]
        [RegularExpression(@"^[a-zA-Z0-9'' ']+$", ErrorMessage = "The special characters are not allowed in Location name")]
        public string LocationName { get; set; }
        [Required(ErrorMessage = "Location id requierd")]
        public string LocationId { get; set; }
    }
}
