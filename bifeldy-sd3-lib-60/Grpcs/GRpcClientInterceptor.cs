/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Untuk Intercept Client
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.Extensions.Logging;

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace bifeldy_sd3_lib_60.Grpcs {

    public class GRpcClientInterceptor : Interceptor {

        private readonly ILogger<GRpcClientInterceptor> _logger;

        public GRpcClientInterceptor(ILoggerFactory loggerFactory) {
            this._logger = loggerFactory.CreateLogger<GRpcClientInterceptor>();
        }

        private async Task<T> HandleClient<T>(Task<T> data, string nameOp, string targetServer) {
            try {
                T result = await data;

                this._logger.LogInformation("[GRPC_INTERCEPTOR_{type}] ⚙ Finished {nameOp} ... {targetServer}", typeof(T).Name.ToUpper(), nameOp, targetServer);

                return result;
            }
            catch (Exception e) {
                this._logger.LogInformation("[GRPC_INTERCEPTOR_{type}] ⚙ Error {nameOp} ... {message}", typeof(T).Name.ToUpper(), nameOp, e.Message);
                throw;
            }
        }

        /* ** Client Interceptor */

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation
        ) {
            string targetServer = $"{context.Method.Type} / {context.Method.Name}";
            string nameOp = "Blocking Unary";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Calling {nameOp} ... {targetServer}", nameOp, targetServer);

            TResponse result = continuation(request, context);

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Finished {nameOp} ... {targetServer}", nameOp, targetServer);

            return result;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation
        ) {
            string targetServer = $"{context.Method.Type} / {context.Method.Name}";
            string nameOp = "Async Unary";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Calling {nameOp} ... {targetServer}", nameOp, targetServer);

            AsyncUnaryCall<TResponse> call = continuation(request, context);

            return new AsyncUnaryCall<TResponse>(
                this.HandleClient(call.ResponseAsync, nameOp, targetServer),
                this.HandleClient(call.ResponseHeadersAsync, nameOp, targetServer),
                call.GetStatus,
                call.GetTrailers,
                call.Dispose
            );
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
        ) {
            string targetServer = $"{context.Method.Type} / {context.Method.Name}";
            string nameOp = "Async Client Stream";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Calling {nameOp} ... {targetServer}", nameOp, targetServer);

            AsyncClientStreamingCall<TRequest, TResponse> call = continuation(context);

            return new AsyncClientStreamingCall<TRequest, TResponse>(
                call.RequestStream,
                this.HandleClient(call.ResponseAsync, nameOp, targetServer),
                this.HandleClient(call.ResponseHeadersAsync, nameOp, targetServer),
                call.GetStatus,
                call.GetTrailers,
                call.Dispose
            );
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
        ) {
            string targetServer = $"{context.Method.Type} / {context.Method.Name}";
            string nameOp = "Async Server Streaming";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Calling {nameOp} ... {targetServer}", nameOp, targetServer);

            AsyncServerStreamingCall<TResponse> call = continuation(request, context);

            return new AsyncServerStreamingCall<TResponse>(
                call.ResponseStream,
                this.HandleClient(call.ResponseHeadersAsync, nameOp, targetServer),
                call.GetStatus,
                call.GetTrailers,
                call.Dispose
            );
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
        ) {
            string targetServer = $"{context.Method.Type} / {context.Method.Name}";
            string nameOp = "Async Server Streaming";

            this._logger.LogInformation("[GRPC_INTERCEPTOR] ⚙ Calling {nameOp} ... {targetServer}", nameOp, targetServer);

            AsyncDuplexStreamingCall<TRequest, TResponse> call = continuation(context);

            return new AsyncDuplexStreamingCall<TRequest, TResponse>(
                call.RequestStream,
                call.ResponseStream,
                this.HandleClient(call.ResponseHeadersAsync, nameOp, targetServer),
                call.GetStatus,
                call.GetTrailers,
                call.Dispose
            );
        }

    }

}
