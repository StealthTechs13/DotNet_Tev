using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Models;

namespace Tev.API.Mocks
{
    public interface IMockData
    {
       
        List<PaymentHistoryResponse> GetPaymentHistory();

    }
}
