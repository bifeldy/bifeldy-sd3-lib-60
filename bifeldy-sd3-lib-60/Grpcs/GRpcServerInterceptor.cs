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
using Microsoft.Extensions.Logging;

using Grpc.Core;
using Grpc.Core.Interceptors;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Repositories;

namespace bifeldy_sd3_lib_60.Grpcs {

    public class GRpcServerInterceptor : Interceptor {

        private readonly ILogger<GRpcServerInterceptor> _logger;

        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;
        private readonly IChiperService _chiper;
        private readonly IApiKeyRepository _akRepo;
        private readonly IGeneralRepository _generalRepo;

        public string SessionKey { get; } = "user-session";

        public GRpcServerInterceptor(
            ILogger<GRpcServerInterceptor> logger,
            IApplicationService app,
            IGlobalService gs,
            IChiperService chiper,
            IApiKeyRepository akRepo,
            IGeneralRepository generalRepo
        ) {
            this._logger = logger;
            this._app = app;
            this._gs = gs;
            this._chiper = chiper;
            this._akRepo = akRepo;
            this._generalRepo = generalRepo;
        }

        private async Task CheckUserLogin(ServerCallContext context) {
            if (!context.Method.Contains("ServerReflection")) {
                HttpContext http = context.GetHttpContext();
                HttpRequest request = http.Request;

                RequestJson reqBody = await this._gs.GetRequestBody(request);

                string token = string.Empty;
                string secret = this._gs.GetSecretData(request, reqBody);

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

                    if (!allowed) {
                        throw new Exception("Silahkan Login Terlebih Dahulu Menggunakan API Lalu Gunakan Pada Header 'x-secret-key'!");
                    }

                    token = this._chiper.EncodeJWT(new UserApiSession() {
                        name = this._gs.GetIpOriginData(http.Connection, request),
                        role = UserSessionRole.PROGRAM_SERVICE
                    });

                    request.Headers.Authorization = $"Bearer {token}";
                    request.Headers["x-access-token"] = token;
                    request.Headers["x-secret-key"] = string.Empty;
                }

                if (string.IsNullOrEmpty(token)) {
                    token = this._gs.GetTokenData(request, reqBody);
                }

                if (token.StartsWith("Bearer ")) {
                    token = token[7..];
                }

                if (string.IsNullOrEmpty(token)) {
                    throw new Exception("Silahkan Login Terlebih Dahulu Menggunakan API Lalu Gunakan Pada Header 'Authorization' Bearer Atau 'x-access-token'!");
                }

                http.Items["token"] = token;

                try {
                    IEnumerable<Claim> userClaim = this._chiper.DecodeJWT(token);
                    var userClaimIdentity = new ClaimsIdentity(userClaim, this.SessionKey);
                    http.User = new ClaimsPrincipal(userClaimIdentity);

                    Claim _claimName = userClaim.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault();
                    Claim _claimRole = userClaim.Where(c => c.Type == ClaimTypes.Role).FirstOrDefault();
                    if (_claimName == null || _claimRole == null) {
                        throw new Exception("Format Token Salah / Expired!");
                    }

                    http.Items["user"] = new UserApiSession() {
                        name = _claimName.Value,
                        role = (UserSessionRole) Enum.Parse(typeof(UserSessionRole), _claimRole.Value)
                    };
                }
                catch {
                    throw new Exception("Format Token Salah / Expired!");
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
                await this.CheckUserLogin(context);

                TResponse result = await continuation(request, context);

                this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Finished {nameOp} ... {targetServer}", nameOp, targetServer);

                return result;
            }
            catch (Exception e) {
                this._logger.LogInformation("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
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
                this._logger.LogInformation("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
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
                await this.CheckUserLogin(context);

                await continuation(request, responseStream, context);

                this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Finished {nameOp} ... {targetServer}", nameOp, targetServer);
            }
            catch (Exception e) {
                this._logger.LogInformation("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
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
                this._logger.LogInformation("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(TResponse).Name.ToUpper(), nameOp, e.Message);
                throw;
            }
        }

    }

}
