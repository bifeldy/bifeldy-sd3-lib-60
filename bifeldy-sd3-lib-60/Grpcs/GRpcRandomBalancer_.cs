/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: DB Resolver GRpc
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Grpc.Net.Client.Balancer;

using Microsoft.Extensions.Logging;

namespace bifeldy_sd3_lib_60.Grpcs {

    public sealed class CGRpcRandomPicker : SubchannelPicker {

        private readonly IReadOnlyList<Subchannel> _subchannels;

        public CGRpcRandomPicker(IReadOnlyList<Subchannel> subchannels) {
            _subchannels = subchannels;
        }

        public override PickResult Pick(PickContext context) {
            return PickResult.ForSubchannel(_subchannels[Random.Shared.Next(0, _subchannels.Count)]);
        }

    }

    public sealed class CGRpcRandomBalancer : SubchannelsLoadBalancer {

        public CGRpcRandomBalancer(
            IChannelControlHelper controller,
            ILoggerFactory loggerFactory
        ) : base(controller, loggerFactory) {
            //
        }

        protected override SubchannelPicker CreatePicker(IReadOnlyList<Subchannel> readySubchannels) {
            return new CGRpcRandomPicker(readySubchannels);
        }

    }

    // https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing?view=aspnetcore-9.0#create-a-custom-load-balancer

    public class CGRpcRandomBalancerFactory : LoadBalancerFactory {

        public override string Name => "random";

        public override LoadBalancer Create(LoadBalancerOptions options) {
            return new CGRpcRandomBalancer(options.Controller, options.LoggerFactory);
        }

    }

}
