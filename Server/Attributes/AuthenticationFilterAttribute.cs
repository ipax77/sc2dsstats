using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace sc2dsstats._2022.Server.Attributes
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

    public class HttpException : Exception
    {
        public int StatusCode { get; }

        public HttpException(HttpStatusCode httpStatusCode)
            : base(httpStatusCode.ToString())
        {
            this.StatusCode = (int)httpStatusCode;
        }
    }
}
