using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Models;

namespace Tev.API
{
    public static class InvalidModelCustomResponse
    {
        public static BadRequestObjectResult CustomErrorResponse(ActionContext actionContext)
        {
            if (actionContext != null)
            {
                var errorList = new List<string>();
                var errors = actionContext.ModelState.Select(x => x.Value.Errors).ToList();
                foreach (var error in errors)
                {
                    foreach (var item in error)
                    {
                        errorList.Add(item.ErrorMessage);
                    }

                }
                return new BadRequestObjectResult(new MMSHttpReponse { ErrorMessage = errorList.FirstOrDefault().ToString() });
            }
            else
            {
                return new BadRequestObjectResult(new MMSHttpReponse { ErrorMessage = "actionContext argumnet is null" });
            }
           
        }
    }
}
