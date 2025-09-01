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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Grpc.Core;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Repositories;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class SecretMiddleware {

        private readonly EnvVar _env;

        private readonly RequestDelegate _next;
        private readonly ILogger<SecretMiddleware> _logger;
        private readonly IApplicationService _app;
        private readonly IChiperService _chiper;

        public string SessionKey { get; } = "user-session";

        public SecretMiddleware(
            RequestDelegate next,
            IOptions<EnvVar> env,
            ILogger<SecretMiddleware> logger,
            IApplicationService app,
            IChiperService chiper
        ) {
            this._next = next;
            this._env = env.Value;
            this._logger = logger;
            this._app = app;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context, IOraPg _orapg, IApiKeyRepository _akRepo) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string apiPathRequested = request.Path.Value;
            if (string.IsNullOrEmpty(apiPathRequested)) {
                await this._next(context);
                return;
            }

            string apiPathRequestedForGrpc = apiPathRequested.Split('/').Where(u => !string.IsNullOrEmpty(u)).FirstOrDefault();

            bool isGrpc = Bifeldy.GRPC_ROUTE_PATH.Contains(apiPathRequestedForGrpc);
            bool isSignalr = apiPathRequested.StartsWith(Bifeldy.SIGNALR_PREFIX_HUB, StringComparison.InvariantCultureIgnoreCase);
            bool isApi = apiPathRequested.StartsWith($"/{Bifeldy.API_PREFIX}/", StringComparison.InvariantCultureIgnoreCase);
            bool isSwagger = apiPathRequested.StartsWith($"/{Bifeldy.API_PREFIX}/swagger", StringComparison.InvariantCultureIgnoreCase);

            if ((!isGrpc && !isSignalr && !isApi) || (isSwagger && string.IsNullOrEmpty(Bifeldy.PLUGINS_PROJECT_NAMESPACE))) {
                await this._next(context);
                return;
            }

            string secret = context.Items["secret"]?.ToString();

            this._logger.LogInformation("[SECRET_MIDDLEWARE] 🗝 {secret}", secret);

            if (!string.IsNullOrEmpty(secret)) {
                bool allowed = false;

                // Khusus Bypass ~ Case Sensitive
                string hashText = this._chiper.HashText(this._app.AppName);
                if (secret == hashText || await _akRepo.SecretLogin(this._env.IS_USING_POSTGRES, _orapg, secret) != null) {
                    allowed = true;
                }

                if (!allowed) {
                    string errMsg = "Secret salah / tidak dikenali!";

                    if (isGrpc) {
                        throw new RpcException(
                            new Status(
                                StatusCode.PermissionDenied,
                                errMsg
                            )
                        );
                    }

                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;

                    await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = "401 - Secret :: Tidak Dapat Digunakan",
                        result = new ResponseJsonMessage() {
                            message = errMsg
                        }
                    });

                    return;
                }

                string token = context.Items["token"]?.ToString();
                if (string.IsNullOrEmpty(token)) {
                    string addrIp = context.Items["address_ip"]?.ToString();

                    if (request.Query.ContainsKey("mask_ip")) {
                        addrIp = this._chiper.DecryptText(request.Query["mask_ip"], hashText);
                    }

                    string addrOrigin = context.Items["address_origin"]?.ToString();
                    string ipOrigin = addrOrigin == addrIp ? addrOrigin : $"{addrOrigin}@{addrIp}";
                    context.Items["ip_origin"] = ipOrigin;

                    token = this._chiper.EncodeJWT(new UserApiSession() {
                        name = addrIp,
                        role = UserSessionRole.PROGRAM_SERVICE
                    });

                    context.Items["token"] = token;
                }
            }

            await this._next(context);
        }

    }

}
