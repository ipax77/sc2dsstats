using Microsoft.AspNetCore.Mvc.Filters;
using sc2dsstats.lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace sc2dsstats.web.Controllers
{
    public class ReloadFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string authKey = context.HttpContext.Request
                    .Headers["Authorization"].SingleOrDefault();

            if (authKey != DSdata.ServerConfig.RESTToken)
                throw new HttpException(HttpStatusCode.Unauthorized);
        }
    }
}
