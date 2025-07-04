﻿/**
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
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using Microsoft.Extensions.Primitives;

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

            DateTime requestStartAt = DateTime.Now;
            context.Items["request_start_at"] = requestStartAt;

            string activityId = Activity.Current?.Id;
            string traceId = context?.TraceIdentifier;

            string requestProxy = string.Empty;
            if (request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
                string proxyPath = pathBase.Last();
                if (!string.IsNullOrEmpty(proxyPath)) {
                    requestProxy = proxyPath;
                }
            }

            string requestPath = request.Path;
            string requestQuery = request.QueryString.ToString();

            (string contentType, string rbString) = await this._gs.ParseRequestBodyJsonString(request);

            //
            // TODO :: Log Request Mulai
            //

            await this._next(context);

            string secret = context.Items["secret"]?.ToString();
            string apiKey = context.Items["api_key"]?.ToString();
            string ipOrigin = context.Items["ip_origin"]?.ToString();
            string token = context.Items["token"]?.ToString();

            UserApiSession user = null;
            if (context.Items["user"] != null) {
                user = (UserApiSession)context.Items["user"];
            }

            DateTime requestEndAt = DateTime.Now;

            //
            // TODO :: Log Request Selesai
            //
        }

    }

}
