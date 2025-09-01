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

        public async Task CheckUserLogin<TRequest>(ServerCallContext context, TRequest body = default) {
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

                    RequestJson reqBody = null;
                    if (typeof(RequestJson).IsAssignableFrom(body?.GetType())) {
                        reqBody = (dynamic)body;
                    }
                    else {
                        reqBody = await gs.GetHttpRequestBody<RequestJson>(http.Request);
                    }

                    if (reqBody != null) {
                        string secret = reqBody.secret;
                        string apiKey = reqBody.key;
                        string token = reqBody.token;

                        // -- Secret

                        if (Bifeldy.IS_USING_SECRET && !string.IsNullOrEmpty(secret)) {
                            http.Items["secret"] = secret;

                            bool allowed = false;

                            // Khusus Bypass ~ Case Sensitive
                            string hashText = chiper.HashText(app.AppName);
                            if (secret == hashText || await akRepo.SecretLogin(env.IS_USING_POSTGRES, orapg, secret) != null) {
                                allowed = true;
                            }

                            if (!allowed) {
                                throw new RpcException(
                                    new Status(
                                        StatusCode.PermissionDenied,
                                        "Secret salah / tidak dikenali!"
                                    )
                                );
                            }

                            if (string.IsNullOrEmpty(token)) {
                                string addrIp = http.Items["address_ip"]?.ToString();

                                if (request.Query.ContainsKey("mask_ip")) {
                                    addrIp = chiper.DecryptText(request.Query["mask_ip"], hashText);
                                }

                                string addrOrigin = http.Items["address_origin"]?.ToString();
                                string ipOrigin = addrOrigin == addrIp ? addrOrigin : $"{addrOrigin}@{addrIp}";
                                http.Items["ip_origin"] = ipOrigin;

                                token = chiper.EncodeJWT(new UserApiSession() {
                                    name = addrIp,
                                    role = UserSessionRole.PROGRAM_SERVICE
                                });
                            }
                        }

                        // -- ApiKey

                        if (Bifeldy.IS_USING_API_KEY && !string.IsNullOrEmpty(apiKey)) {
                            http.Items["api_key"] = apiKey;

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

                            string ipOrigin = http.Items["ip_origin"]?.ToString();

                            // Khusus Bypass ~ Case Sensitive
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

                        if (Bifeldy.IS_USING_JWT && !string.IsNullOrEmpty(token)) {
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
                                if (_claimName == null || _claimRole == null) {
                                    throw new Exception("Format Token Salah / Expired!");
                                }

                                var userInfo = new UserApiSession() {
                                    name = _claimName.Value,
                                    role = (UserSessionRole)Enum.Parse(typeof(UserSessionRole), _claimRole.Value)
                                };

                                http.Items["user"] = userInfo;
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
                await this.CheckUserLogin<TRequest>(context);

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
                await this.CheckUserLogin<TRequest>(context);

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
