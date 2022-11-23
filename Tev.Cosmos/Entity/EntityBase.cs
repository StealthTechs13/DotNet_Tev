using System;
using System.Collections.Generic;
using System.Text;

namespace Tev.Cosmos.Entity
{
    public class EntityBase
    {
        public string _rid { get; set; }
        public string _self { get; set; }
        public string _etag { get; set; }
        public string _attachments { get; set; }
        public long _ts { get; set; }
    }
}
