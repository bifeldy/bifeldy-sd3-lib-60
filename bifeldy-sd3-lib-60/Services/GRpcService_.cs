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
using Microsoft.Extensions.Options;

using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Grpcs;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IGRpcService {
        GrpcChannel CreateChannel(string host, int port = 0);
        T ClientGetService<T>(string host, int port = 0) where T : class;
    }

    [SingletonServiceRegistration]
    public sealed class CGRpcService : IGRpcService {

        private readonly EnvVar _envVar;

        private readonly ILoggerFactory _loggerFactory;

        public CGRpcService(
            IOptions<EnvVar> envVar,
            ILoggerFactory loggerFactory
        ) {
            this._envVar = envVar.Value;
            this._loggerFactory = loggerFactory;
        }

        public GrpcChannel CreateChannel(string host, int port = 0) {
            if (string.IsNullOrEmpty(host)) {
                throw new ArgumentNullException("host", "Harus Berisi Alamat IP / Domain");
            }

            if (port <= 0) {
                port = this._envVar.GRPC_PORT;
            }

            var opt = new GrpcChannelOptions() {
                Credentials = ChannelCredentials.Insecure
            };
            if (!host.StartsWith("http")) {
                host = $"http://{host}";
            }

            return GrpcChannel.ForAddress($"{host}:{port}", opt);
        }

        public T ClientGetService<T>(string host, int port = 0) where T : class {
            GrpcChannel channel = this.CreateChannel(host, port);
            CallInvoker invoker = channel.Intercept(new CGRpcClientInterceptor(this._loggerFactory));
            return invoker.CreateGrpcService<T>();
        }

    }

}
