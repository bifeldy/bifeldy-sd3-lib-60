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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

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
        GrpcChannelOptions CreateConfig(IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, IHeaderDictionary httpHeader = null);
        GrpcChannel CreateChannel(string host, int port = 0, bool disableResolver = true, IHeaderDictionary httpHeader = null);
        (GrpcChannel, T) ClientGetService<T>(string host, int port = 0, bool disableResolver = true, IHeaderDictionary httpHeader = null) where T : class;
        GrpcChannel CreateChannelWithLoadBalanced(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, IHeaderDictionary httpHeader = null);
        (GrpcChannel, T) ClientGetServiceWithLoadBalanced<T>(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, IHeaderDictionary httpHeader = null) where T : class;
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

        public GrpcChannelOptions CreateConfig(IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, IHeaderDictionary httpHeader = null) {
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

            if (httpHeader != null) {
                IHttpService httpService = this._serviceProvider.GetRequiredService<IHttpService>();
                HttpClient httpClient = httpService.CreateHttpClient();

                foreach (KeyValuePair<string, StringValues> header in httpHeader) {
                    if (!httpClient.DefaultRequestHeaders.Contains(header.Key)) {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value.AsEnumerable());
                    }
                }

                grpcChannel.HttpClient = httpClient;
            }

            return grpcChannel;
        }

        public GrpcChannel CreateChannel(string host, int port = 0, bool disableResolver = true, IHeaderDictionary httpHeader = null) {
            if (string.IsNullOrEmpty(host)) {
                throw new ArgumentNullException("host", "Harus Berisi Alamat IP / Domain");
            }

            if (port <= 0) {
                port = this._envVar.GRPC_PORT;
            }

            if (!host.StartsWith("http")) {
                host = $"http://{host}";
            }

            return GrpcChannel.ForAddress($"{host}:{port}", this.CreateConfig(null, disableResolver, httpHeader));
        }

        public (GrpcChannel, T) ClientGetService<T>(string host, int port = 0, bool disableResolver = true, IHeaderDictionary httpHeader = null) where T : class {
            GrpcChannel channel = this.CreateChannel(host, port, disableResolver, httpHeader);
            CallInvoker invoker = channel.Intercept(new CGRpcClientInterceptor(this._loggerFactory));
            T service = invoker.CreateGrpcService<T>();

            return (channel, service);
        }

        public GrpcChannel CreateChannelWithLoadBalanced(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, IHeaderDictionary httpHeader = null) {
            return GrpcChannel.ForAddress(hostPort, this.CreateConfig(loadBalancingConfigs, disableResolver, httpHeader));
        }

        public (GrpcChannel, T) ClientGetServiceWithLoadBalanced<T>(string hostPort, IEnumerable<LoadBalancingConfig> loadBalancingConfigs = null, bool disableResolver = false, IHeaderDictionary httpHeader = null) where T : class {
            loadBalancingConfigs ??= new[] {
                new RoundRobinConfig()
            };

            GrpcChannel channel = this.CreateChannelWithLoadBalanced(hostPort, loadBalancingConfigs, disableResolver, httpHeader);
            CallInvoker invoker = channel.Intercept(new CGRpcClientInterceptor(this._loggerFactory));
            T service = invoker.CreateGrpcService<T>();

            return (channel, service);
        }

    }

}
