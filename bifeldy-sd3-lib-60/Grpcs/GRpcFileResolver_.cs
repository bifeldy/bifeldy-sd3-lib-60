/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: File Resolver GRpc
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Text.Json;

using Microsoft.Extensions.Logging;

using Grpc.Net.Client.Balancer;

namespace bifeldy_sd3_lib_60.Grpcs {

    public sealed class CGRpcFileResolver : PollingResolver {

        private readonly Uri _address;
        private readonly int _port;

        public CGRpcFileResolver(Uri address, int defaultPort, ILoggerFactory loggerFactory) : base(loggerFactory) {
            this._address = address;
            this._port = defaultPort;
        }

        protected override async Task ResolveAsync(CancellationToken cancellationToken) {
            string jsonString = await File.ReadAllTextAsync(this._address.LocalPath);
            string[] results = JsonSerializer.Deserialize<string[]>(jsonString);
            BalancerAddress[] addresses = results.Select(r => new BalancerAddress(r, this._port)).ToArray();

            this.Listener(ResolverResult.ForResult(addresses));
        }

    }

    // https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing?view=aspnetcore-9.0#create-a-custom-resolver

    public class CGRpcFileResolverFactory : ResolverFactory {

        public override string Name => "file";

        public override Resolver Create(ResolverOptions options) {
            return new CGRpcFileResolver(options.Address, options.DefaultPort, options.LoggerFactory);
        }

    }

}
