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
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Repositories;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class SecretMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<SecretMiddleware> _logger;
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;
        private readonly IChiperService _chiper;

        public string SessionKey { get; } = "user-session";

        public SecretMiddleware(
            RequestDelegate next,
            ILogger<SecretMiddleware> logger,
            IApplicationService app,
            IGlobalService gs,
            IChiperService chiper
        ) {
            this._next = next;
            this._logger = logger;
            this._app = app;
            this._gs = gs;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context, IApiKeyRepository _akRepo, IGeneralRepository _generalRepo) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string apiPathRequested = request.Path.Value;
            string apiPathRequestedForGrpc = apiPathRequested.Split('/').Where(u => !string.IsNullOrEmpty(u)).FirstOrDefault();

            bool isGrpc = Bifeldy.GRPC_ROUTH_PATH.Contains(apiPathRequestedForGrpc);
            bool isApi = apiPathRequested.StartsWith("/api/");
            bool isSwagger = apiPathRequested.StartsWith("/api/swagger");

            if ((!isGrpc && !isApi) || isSwagger) {
                await this._next(context);
                return;
            }

            RequestJson reqBody = await this._gs.GetRequestBody(request);
            string secret = this._gs.GetSecretData(request, reqBody);

            context.Items["secret"] = secret;
            this._logger.LogInformation("[SECRET_MIDDLEWARE] 🗝 {secret}", secret);

            if (!string.IsNullOrEmpty(secret)) {
                bool allowed = false;
                string hashText = this._chiper.HashText(this._app.AppName);

                string currentKodeDc = await _generalRepo.GetKodeDc();
                if (currentKodeDc == "DCHO") {
                    if (await _akRepo.SecretLogin(secret) != null) {
                        allowed = true;
                    }
                }
                else {
                    string apiKey = this._gs.GetApiKeyData(request, reqBody);
                    if (apiKey == hashText || await _akRepo.SecretLogin(secret) != null) {
                        allowed = true;
                    }
                }

                try {
                    if (!allowed) {
                        throw new Exception("Secret salah / tidak dikenali!");
                    }

                    string maskIp = string.IsNullOrEmpty(request.Query["mask_ip"])
                        ? this._gs.GetIpOriginData(connection, request)
                        : this._chiper.DecryptText(request.Query["mask_ip"], hashText);
                    string token = this._chiper.EncodeJWT(new UserApiSession() {
                        name = maskIp,
                        role = UserSessionRole.PROGRAM_SERVICE
                    });

                    request.Headers.Authorization = $"Bearer {token}";
                    request.Headers["x-access-token"] = token;
                    request.Headers["x-secret-key"] = string.Empty;

                    var queryitems = request.Query.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value)).ToList();
                    var queryparameters = new List<KeyValuePair<string, string>>();
                    foreach (KeyValuePair<string, string> item in queryitems) {
                        if (item.Key.ToLower() == "token") {
                            queryparameters.Add(new KeyValuePair<string, string>(item.Key, token));
                        }
                        else if (item.Key.ToLower() != "secret") {
                            queryparameters.Add(new KeyValuePair<string, string>(item.Key, item.Value));
                        }
                    }

                    if (queryparameters.FindIndex(qp => qp.Key.ToLower() == "token") == -1) {
                        queryparameters.Add(new KeyValuePair<string, string>("token", token));
                    }

                    request.QueryString = new QueryBuilder(queryparameters).ToQueryString();
                }
                catch {
                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = "401 - Secret :: Tidak Dapat Digunakan",
                        result = new ResponseJsonMessage() {
                            message = "Secret salah / tidak dikenali!"
                        }
                    });

                    return;
                }
            }

            await this._next(context);
        }

    }

}
