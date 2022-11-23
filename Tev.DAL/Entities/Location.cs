using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.DAL.Entities
{
    public class Location:Entity
    {
        public string Id { get; set; }
        public string Name { get; set;}
        public string OrgId { get; set; }
    }
}
