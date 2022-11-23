using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Models;

namespace Tev.API.Mocks
{
    public class MockData : IMockData
    {
        

     

        

        public List<PaymentHistoryResponse> GetPaymentHistory()
        {
            var list = new List<PaymentHistoryResponse>() {
                new PaymentHistoryResponse {
                    Id="123456adasd",
                    Amount=3599.90M,
                    PaymentDate=1600239516,
                    InvoiceUrl="https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf"
                },
                new PaymentHistoryResponse {
                    Id="123456adasd",
                    Amount=3599.90M,
                    PaymentDate=1600039516,
                    InvoiceUrl="https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf"
                }
            };

            return list;
        }

       
    }
}
