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
using Microsoft.Extensions.Primitives;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class RequestVariableInitializerMiddleware {

        private readonly RequestDelegate _next;
        private readonly IGlobalService _gs;

        public RequestVariableInitializerMiddleware(
            RequestDelegate next,
            IGlobalService gs
        ) {
            this._next = next;
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

            string addrIp = this._gs.GetIpOriginData(connection, request, true, true);
            context.Items["address_ip"] = addrIp;

            string addrIpProxy = this._gs.GetIpOriginData(connection, request, true);
            context.Items["address_ip_proxy"] = addrIpProxy;

            string addrOrigin = this._gs.GetIpOriginData(connection, request, removeReverseProxyRoute: true);
            context.Items["address_origin"] = addrOrigin;

            string ipOrigin = addrOrigin == addrIp ? addrOrigin : $"{addrOrigin}@{addrIp}";
            context.Items["ip_origin"] = ipOrigin;

            RequestJson reqBody = await this._gs.GetHttpRequestBody<RequestJson>(request);

            string secret = this._gs.GetSecretData(request, reqBody);
            context.Items["secret"] = secret;

            string apiKey = this._gs.GetApiKeyData(request, reqBody);
            context.Items["api_key"] = apiKey;

            string token = this._gs.GetTokenData(request, reqBody);
            if (token.StartsWith("Bearer ")) {
                token = token[7..];
            }

            context.Items["token"] = token;

            //
            // TODO :: Log Request Mulai
            //

            await this._next(context);

            //
            // TODO :: Log Request Selesai
            //

            string kunciGxxx = null;
            if (context.Items["kunci_gxxx"] != null) {
                kunciGxxx = context.Items["kunci_gxxx"].ToString();
            }

            addrIp = context.Items["address_ip"]?.ToString();
            addrIpProxy = context.Items["address_ip_proxy"]?.ToString();
            addrOrigin = context.Items["address_origin"]?.ToString();
            ipOrigin = context.Items["ip_origin"]?.ToString();
            secret = context.Items["secret"]?.ToString();
            apiKey = context.Items["api_key"]?.ToString();
            token = context.Items["token"]?.ToString();

            UserApiSession user = null;
            if (context.Items["user"] != null) {
                user = (UserApiSession)context.Items["user"];
            }

            string errDtl = null;
            if (context.Items["error_detail"] != null) {
                errDtl = context.Items["error_detail"].ToString();
            }

            DateTime requestEndAt = DateTime.Now;
        }

    }

}
