/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: GRPCs
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.Extensions.Logging;

using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Grpcs;

namespace bifeldy_sd3_lib_60.Services {

    public interface IGRpcClientService {
        GrpcChannel CreateChannel(string host, int port);
        GrpcClient ClientGetService<T>(string host, int port);
    }

    [SingletonServiceRegistration]
    public sealed class CGRpcClientService : IGRpcClientService {

        private readonly ILoggerFactory _loggerFactory;

        public CGRpcClientService(ILoggerFactory loggerFactory) {
            this._loggerFactory = loggerFactory;
        }

        public GrpcChannel CreateChannel(string host, int port) {
            var opt = new GrpcChannelOptions() {
                Credentials = ChannelCredentials.Insecure
            };
            if (!host.StartsWith("http")) {
                host = $"http://{host}";
            }

            return GrpcChannel.ForAddress($"{host}:{port}", opt);
        }

        public GrpcClient ClientGetService<T>(string host, int port) {
            GrpcChannel channel = this.CreateChannel(host, port);
            CallInvoker invoker = channel.Intercept(new GRpcClientInterceptor(this._loggerFactory));
            return invoker.CreateGrpcService(typeof(T));
        }

    }

}
