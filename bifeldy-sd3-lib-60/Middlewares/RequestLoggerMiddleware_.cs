/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Middleware Jwt Pasang User Ke Context
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class RequestLoggerMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggerMiddleware> _logger;
        private readonly IGlobalService _gs;

        public RequestLoggerMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggerMiddleware> logger,
            IGlobalService gs
        ) {
            this._next = next;
            this._logger = logger;
            this._gs = gs;
        }

        public async Task Invoke(HttpContext context) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string apiPathRequested = request.Path.Value;
            string apiPathRequestedForGrpc = apiPathRequested.Split('/').Where(u => !string.IsNullOrEmpty(u)).FirstOrDefault();

            bool isGrpc = Bifeldy.GRPC_ROUTH_PATH.Contains(apiPathRequestedForGrpc);
            bool isSignalr = apiPathRequested.StartsWith(Bifeldy.SIGNALR_PREFIX_HUB);
            bool isApi = apiPathRequested.StartsWith("/api/");
            bool isSwagger = apiPathRequested.StartsWith("/api/swagger");

            if ((!isGrpc && !isSignalr && !isApi) || isSwagger) {
                await this._next(context);
                return;
            }

            string secret = context.Items["secret"]?.ToString();
            string apiKey = context.Items["api_key"]?.ToString();
            string ipOrigin = context.Items["ip_origin"]?.ToString();
            string token = context.Items["token"]?.ToString();

            UserApiSession user = null;
            if (context.Items["user"] != null) {
                user = (UserApiSession)context.Items["user"];
            }

            string activityId = Activity.Current?.Id;
            string traceId = context?.TraceIdentifier;

            (string contentType, string rbString) = await this._gs.ParseRequestBodyString(request);

            //
            // TODO :: Nge Log Request Sebelum
            //

            await this._next(context);

            //
            // TODO :: Nge Log Request Sesudah
            //

        }

    }

}
