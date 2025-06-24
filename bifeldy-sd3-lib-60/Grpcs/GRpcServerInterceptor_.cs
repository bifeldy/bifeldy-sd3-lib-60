/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Untuk Intercept Server
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Grpc.Core;
using Grpc.Core.Interceptors;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Grpcs {

    public sealed class CGRpcServerInterceptor : Interceptor {

        private readonly ILogger<CGRpcServerInterceptor> _logger;

        public string SessionKey { get; } = "user-session";

        public CGRpcServerInterceptor(ILoggerFactory loggerFactory) {
            this._logger = loggerFactory.CreateLogger<CGRpcServerInterceptor>();
        }

        public async Task CheckUserLogin(ServerCallContext context, dynamic body = null) {
            if (!context.Method.Contains("ServerReflection")) {
                HttpContext http = context.GetHttpContext();
                ConnectionInfo connection = http.Connection;
                HttpRequest request = http.Request;

                EnvVar env = http.RequestServices.GetRequiredService<IOptions<EnvVar>>().Value;
                IOraPg orapg = http.RequestServices.GetRequiredService<IOraPg>();
                IApplicationService app = http.RequestServices.GetRequiredService<IApplicationService>();
                IGlobalService gs = http.RequestServices.GetRequiredService<IGlobalService>();
                IChiperService chiper = http.RequestServices.GetRequiredService<IChiperService>();
                IApiKeyRepository akRepo = http.RequestServices.GetRequiredService<IApiKeyRepository>();
                IGeneralRepository generalRepo = http.RequestServices.GetRequiredService<IGeneralRepository>();

                if (http.Items["user"] == null) {
                    if (body != null) {
                        string secret = body.secret;
                        string apiKey = body.key;
                        string token = body.token;

                        // -- Secret

                        if (!string.IsNullOrEmpty(secret)) {
                            bool allowed = false;
                            string hashText = chiper.HashText(app.AppName);

                            if (secret == hashText || await akRepo.SecretLogin(env.IS_USING_POSTGRES, orapg, secret) != null) {
                                allowed = true;
                            }

                            if (!allowed) {
                                throw new RpcException(
                                    new Status(
                                        StatusCode.Unauthenticated,
                                        "Silahkan Login Terlebih Dahulu Menggunakan API Lalu Gunakan Token Seperti Biasa!"
                                    )
                                );
                            }

                            string maskIp = string.IsNullOrEmpty(request.Query["mask_ip"])
                                ? gs.GetIpOriginData(connection, request)
                                : chiper.DecryptText(request.Query["mask_ip"], hashText);

                            token = chiper.EncodeJWT(new UserApiSession() {
                                name = maskIp,
                                role = UserSessionRole.PROGRAM_SERVICE
                            });

                            request.Headers.Authorization = $"Bearer {token}";
                            request.Headers["x-access-token"] = token;
                            request.Headers["x-secret-key"] = secret;

                            var queryitems = request.Query.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value)).ToList();
                            var queryparameters = new List<KeyValuePair<string, string>>();
                            foreach (KeyValuePair<string, string> item in queryitems) {
                                if (item.Key.ToLower() == "token") {
                                    queryparameters.Add(new KeyValuePair<string, string>(item.Key, token));
                                }

                                if (item.Key.ToLower() != "secret") {
                                    queryparameters.Add(new KeyValuePair<string, string>(item.Key, secret));
                                }
                            }

                            if (queryparameters.FindIndex(qp => qp.Key.ToLower() == "token") == -1) {
                                queryparameters.Add(new KeyValuePair<string, string>("token", token));
                            }

                            if (queryparameters.FindIndex(qp => qp.Key.ToLower() == "secret") == -1) {
                                queryparameters.Add(new KeyValuePair<string, string>("secret", secret));
                            }

                            request.QueryString = new QueryBuilder(queryparameters).ToQueryString();
                        }

                        // -- ApiKey

                        if (Bifeldy.IS_USING_API_KEY && !string.IsNullOrEmpty(apiKey)) {
                            string[] serverIps = app.GetAllIpAddress();
                            foreach (string ip in serverIps) {
                                if (!gs.AllowedIpOrigin.Contains(ip)) {
                                    gs.AllowedIpOrigin.Add(ip);
                                }
                            }

                            string ipDomainHost = request.Host.Host;
                            if (!gs.AllowedIpOrigin.Contains(ipDomainHost)) {
                                gs.AllowedIpOrigin.Add(ipDomainHost);
                            }

                            string ipDomainProxy = request.Headers["x-forwarded-host"];
                            if (!string.IsNullOrEmpty(ipDomainProxy) && !gs.AllowedIpOrigin.Contains(ipDomainProxy)) {
                                gs.AllowedIpOrigin.Add(ipDomainProxy);
                            }

                            http.Items["api_key"] = apiKey;
                            string ipOrigin = gs.GetIpOriginData(connection, request);
                            http.Items["ip_origin"] = ipOrigin;
                        
                            string hashText = chiper.HashText(app.AppName);
                            if (apiKey != hashText && !await akRepo.CheckKeyOrigin(env.IS_USING_POSTGRES, orapg, ipOrigin, apiKey)) {
                                throw new RpcException(
                                    new Status(
                                        StatusCode.PermissionDenied,
                                        "Api Key Salah / Tidak Terdaftar!"
                                    )
                                );
                            }
                        }

                        // -- JWT

                        if (!string.IsNullOrEmpty(token)) {
                            if (token.StartsWith("Bearer ")) {
                                token = token[7..];
                            }

                            http.Items["token"] = token;

                            try {
                                IEnumerable<Claim> userClaim = chiper.DecodeJWT(token);
                                var userClaimIdentity = new ClaimsIdentity(userClaim, this.SessionKey);
                                http.User = new ClaimsPrincipal(userClaimIdentity);

                                Claim _claimName = userClaim.Where(c => c.Type == ClaimTypes.Name).First();
                                Claim _claimRole = userClaim.Where(c => c.Type == ClaimTypes.Role).First();
                                http.Items["user"] = new UserApiSession() {
                                    name = _claimName.Value,
                                    role = (UserSessionRole)Enum.Parse(typeof(UserSessionRole), _claimRole.Value)
                                };

                                return;
                            }
                            catch {
                                throw new RpcException(
                                    new Status(
                                        StatusCode.PermissionDenied,
                                        "Format Token Salah / Expired!"
                                    )
                                );
                            }
                        }
                    }

                    throw new RpcException(
                        new Status(
                            StatusCode.Unauthenticated,
                            "Silahkan Login Terlebih Dahulu Menggunakan API Lalu Gunakan Token Pada Header 'Authorization' Bearer Atau 'x-access-token'!"
                        )
                    );
                }
            }
        }

        /* ** Server Interceptor */

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request, ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation
        ) {
            string targetServer = context.Method;
            string nameOp = "Unary";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Receiving {nameOp} ... {targetServer}", nameOp, targetServer);

            try {
                await this.CheckUserLogin(context, request);

                TResponse result = await continuation(request, context);

                this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Finished {nameOp} ... {targetServer}", nameOp, targetServer);

                return result;
            }
            catch (Exception e) {
                this._logger.LogError("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
                throw;
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation
        ) {
            string targetServer = context.Method;
            string nameOp = "Client Streaming";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Receiving {nameOp} ... {targetServer}", nameOp, targetServer);

            try {
                await this.CheckUserLogin(context);

                TResponse result = await continuation(requestStream, context);

                this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Finished {nameOp} ... {targetServer}", nameOp, targetServer);

                return result;
            }
            catch (Exception e) {
                this._logger.LogError("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
                throw;
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation
        ) {
            string targetServer = context.Method;
            string nameOp = "Server Streaming";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Receiving {nameOp} ... {targetServer}", nameOp, targetServer);

            try {
                await this.CheckUserLogin(context, request);

                await continuation(request, responseStream, context);

                this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Finished {nameOp} ... {targetServer}", nameOp, targetServer);
            }
            catch (Exception e) {
                this._logger.LogError("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
                throw;
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation
        ) {
            string targetServer = context.Method;
            string nameOp = "Duplex Streaming";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Receiving {nameOp} ... {targetServer}", nameOp, targetServer);

            try {
                await this.CheckUserLogin(context);

                await continuation(requestStream, responseStream, context);

                this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Finished {nameOp} ... {targetServer}", nameOp, targetServer);
            }
            catch (Exception e) {
                this._logger.LogError("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
                throw;
            }
        }

    }

}
