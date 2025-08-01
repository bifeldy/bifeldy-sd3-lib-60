﻿/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Middleware API Key
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class ApiKeyMiddleware {

        private readonly EnvVar _env;

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;
        private readonly IChiperService _chiper;

        public ApiKeyMiddleware(
            RequestDelegate next,
            IOptions<EnvVar> env,
            ILogger<ApiKeyMiddleware> logger,
            IApplicationService app,
            IGlobalService gs,
            IChiperService chiper
        ) {
            this._next = next;
            this._env = env.Value;
            this._logger = logger;
            this._app = app;
            this._gs = gs;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context, IOraPg _orapg, IApiKeyRepository _akRepo) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string apiPathRequested = request.Path.Value;
            string apiPathRequestedForGrpc = apiPathRequested.Split('/').Where(u => !string.IsNullOrEmpty(u)).FirstOrDefault();

            bool isGrpc = Bifeldy.GRPC_ROUTH_PATH.Contains(apiPathRequestedForGrpc);
            bool isSignalr = apiPathRequested.StartsWith(Bifeldy.SIGNALR_PREFIX_HUB);
            bool isApi = apiPathRequested.StartsWith("/api/");
            bool isSwagger = apiPathRequested.StartsWith("/api/swagger");

            string secret = context.Items["secret"]?.ToString();
            bool haveSecret = !string.IsNullOrEmpty(secret);
            if ((!isGrpc && !isSignalr && !isApi) || (isSwagger && string.IsNullOrEmpty(Bifeldy.PLUGINS_PROJECT_NAMESPACE)) || haveSecret) {
                await this._next(context);
                return;
            }

            string[] serverIps = this._app.GetAllIpAddress();
            foreach (string ip in serverIps) {
                if (!this._gs.AllowedIpOrigin.Contains(ip)) {
                    this._gs.AllowedIpOrigin.Add(ip);
                }
            }

            string ipDomainHost = request.Host.Host;
            if (!this._gs.AllowedIpOrigin.Contains(ipDomainHost)) {
                this._gs.AllowedIpOrigin.Add(ipDomainHost);
            }

            string ipDomainProxy = request.Headers["x-forwarded-host"];
            if (!string.IsNullOrEmpty(ipDomainProxy) && !this._gs.AllowedIpOrigin.Contains(ipDomainProxy)) {
                this._gs.AllowedIpOrigin.Add(ipDomainProxy);
            }

            RequestJson reqBody = await this._gs.GetRequestBody(request);
            string apiKey = this._gs.GetApiKeyData(request, reqBody);
            context.Items["api_key"] = apiKey;
            string ipOrigin = this._gs.GetIpOriginData(connection, request, removeReverseProxyRoute: true);
            context.Items["ip_origin"] = ipOrigin;

            this._logger.LogInformation("[KEY_IP_ORIGIN] 🌸 {apiKey} @ {ipOrigin}", apiKey, ipOrigin);

            // API Khusus Bypass ~ Case Sensitive
            string hashText = this._chiper.HashText(this._app.AppName);
            if (apiKey == hashText || await _akRepo.CheckKeyOrigin(this._env.IS_USING_POSTGRES, _orapg, ipOrigin, apiKey)) {
                await this._next(context);
            }
            else {
                response.Clear();
                response.StatusCode = StatusCodes.Status401Unauthorized;
                await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonErrorApiKeyIpOrigin>() {
                    info = "401 - API Key :: Tidak Dapat Digunakan",
                    result = new ResponseJsonErrorApiKeyIpOrigin() {
                        message = "Api Key Salah / Tidak Terdaftar!",
                        api_key = apiKey,
                        ip_origin = ipOrigin
                    }
                });
            }
        }

    }

}
