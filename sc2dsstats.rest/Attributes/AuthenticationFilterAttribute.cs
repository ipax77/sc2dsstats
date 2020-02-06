using sc2dsstats.rest.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace sc2dsstats.rest.Attributes
{
    public class AuthenticationFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string authKey = context.HttpContext.Request
                    .Headers["Authorization"].SingleOrDefault();

            if (authKey != "DSupload77")
                throw new HttpException(HttpStatusCode.Unauthorized);
        }
    }
}
