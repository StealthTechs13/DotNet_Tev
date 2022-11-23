using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.DAL.Entities
{
    public class Entity
    {
        public long CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public long? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
