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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Grpc.Core;
using Grpc.Core.Interceptors;

using bifeldy_sd3_lib_60.Exceptions;

namespace bifeldy_sd3_lib_60.Grpcs {

    public sealed class CGRpcServerInterceptor : Interceptor {

        private readonly ILogger<CGRpcServerInterceptor> _logger;

        public string SessionKey { get; } = "user-session";

        public CGRpcServerInterceptor(
            ILogger<CGRpcServerInterceptor> logger
        ) {
            this._logger = logger;
        }

        public async Task CheckUserLogin(ServerCallContext context) {
            if (!context.Method.Contains("ServerReflection")) {
                HttpContext http = context.GetHttpContext();
                
                if (http.Items["user"] == null) {
                    throw new TidakMemenuhiException("Silahkan Login Terlebih Dahulu Menggunakan API Lalu Gunakan Pada Header 'Authorization' Bearer Atau 'x-access-token'!");
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
                await this.CheckUserLogin(context);

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
