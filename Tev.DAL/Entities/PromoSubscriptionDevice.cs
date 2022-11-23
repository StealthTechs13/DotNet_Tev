using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class PromoSubscriptionDevice : Entity


    {
        [Key]
        public int Id { get; set; }
        public string CertificateId { get; set; }
        public string SubscriptionId { get; set; }
    }
}
