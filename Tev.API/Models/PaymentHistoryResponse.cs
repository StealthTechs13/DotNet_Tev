using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class PaymentHistoryResponse
    {
        public string Id { get; set; }

        /// <summary>
        /// Payment Date in epoch time
        /// </summary>
        public long PaymentDate { get; set; }

        /// <summary>
        /// Paid amount
        /// </summary>
        public decimal Amount { get; set; }
        public string InvoiceUrl { get; set; }
    }
}
