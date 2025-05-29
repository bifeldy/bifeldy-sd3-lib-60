/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Untuk Binding GRpc
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using ProtoBuf.Grpc.Configuration;

namespace bifeldy_sd3_lib_60.Grpcs {

    public sealed class CGRpcBinder : ServiceBinder {

        private readonly IServiceCollection services;

        public CGRpcBinder(IServiceCollection services) {
            this.services = services;
        }

        public override IList<object> GetMetadata(MethodInfo method, Type contractType, Type serviceType) {
            Type resolvedServiceType = serviceType;

            if (serviceType.IsInterface) {
                resolvedServiceType = services.SingleOrDefault(x => x.ServiceType == serviceType)?.ImplementationType ?? serviceType;
            }

            return base.GetMetadata(method, contractType, resolvedServiceType);
        }

    }

}
