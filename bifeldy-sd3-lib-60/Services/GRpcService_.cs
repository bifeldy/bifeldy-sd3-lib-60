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
using Grpc.Net.Client.Configuration;

using ProtoBuf.Grpc.Client;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Grpcs;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IGRpcService {
        GrpcChannelOptions CreateConfig(IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false);
        GrpcChannel CreateChannel(string host, int port = 0, bool disableResolver = true);
        (GrpcChannel, T) ClientGetService<T>(string host, int port = 0, bool disableResolver = true, CGRpcClientInterceptor interceptor = null) where T : class;
        GrpcChannel CreateChannelWithLoadBalanced(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false);
        (GrpcChannel, T) ClientGetServiceWithLoadBalanced<T>(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, CGRpcClientInterceptor interceptor = null) where T : class;
    }

    [SingletonServiceRegistration]
    public sealed class CGRpcService : IGRpcService {

        private readonly EnvVar _envVar;

        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;

        public CGRpcService(
            IOptions<EnvVar> envVar,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider
        ) {
            this._envVar = envVar.Value;
            this._loggerFactory = loggerFactory;
            this._serviceProvider = serviceProvider;
        }

        public GrpcChannelOptions CreateConfig(IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false) {
            var cfg = new ServiceConfig();

            if (loadBalancingConfigs != null) {
                foreach (LoadBalancingConfig lbc in loadBalancingConfigs) {
                    cfg.LoadBalancingConfigs.Add(lbc);
                }
            }

            var grpcChannel = new GrpcChannelOptions() {
                Credentials = ChannelCredentials.Insecure,
                ServiceProvider = this._serviceProvider,
                ServiceConfig = cfg,
                DisableResolverServiceConfig = disableResolver
            };

            return grpcChannel;
        }

        public GrpcChannel CreateChannel(string host, int port = 0, bool disableResolver = true) {
            if (string.IsNullOrEmpty(host)) {
                throw new ArgumentNullException("host", "Harus Berisi Alamat IP / Domain");
            }

            if (port <= 0) {
                port = this._envVar.GRPC_PORT;
            }

            if (!host.StartsWith("http")) {
                host = $"http://{host}";
            }

            return GrpcChannel.ForAddress($"{host}:{port}", this.CreateConfig(null, disableResolver));
        }

        public (GrpcChannel, T) ClientGetService<T>(string host, int port = 0, bool disableResolver = true, CGRpcClientInterceptor interceptor = null) where T : class {
            interceptor ??= new CGRpcClientInterceptor(this._loggerFactory);

            GrpcChannel channel = this.CreateChannel(host, port, disableResolver);
            CallInvoker invoker = channel.Intercept(interceptor);
            T service = invoker.CreateGrpcService<T>();

            return (channel, service);
        }

        public GrpcChannel CreateChannelWithLoadBalanced(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false) {
            return GrpcChannel.ForAddress(hostPort, this.CreateConfig(loadBalancingConfigs, disableResolver));
        }

        public (GrpcChannel, T) ClientGetServiceWithLoadBalanced<T>(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, CGRpcClientInterceptor interceptor = null) where T : class {
            interceptor ??= new CGRpcClientInterceptor(this._loggerFactory);
            loadBalancingConfigs ??= new[] {
                new RoundRobinConfig()
            };

            GrpcChannel channel = this.CreateChannelWithLoadBalanced(hostPort, loadBalancingConfigs, disableResolver);
            CallInvoker invoker = channel.Intercept(interceptor);
            T service = invoker.CreateGrpcService<T>();

            return (channel, service);
        }

    }

}
