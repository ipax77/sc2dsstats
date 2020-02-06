using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace sc2dsstats.web.Controllers
{
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
