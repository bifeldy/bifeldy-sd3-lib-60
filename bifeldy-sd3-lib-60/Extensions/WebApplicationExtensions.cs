/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Memasang Grpc Service Sesuai Contract Attribute
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Reflection;
using System.ServiceModel;

using Microsoft.AspNetCore.Builder;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class WebApplicationExtensions {

        public static void AutoMapGrpcService(this WebApplication app) {
            Type serviceContract = typeof(ServiceContractAttribute);

            var libAsm = Assembly.GetExecutingAssembly();
            var prgAsm = Assembly.GetEntryAssembly();

            IEnumerable<Type> libPrgAsmTypes = libAsm.GetTypes().Concat(prgAsm.GetTypes())
                .Where(p => p.IsDefined(serviceContract, true) && p.IsInterface);

            foreach (Type grpcService in libPrgAsmTypes) {
                MethodInfo method = typeof(GrpcEndpointRouteBuilderExtensions).GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService)).MakeGenericMethod(grpcService);
                _ = method.Invoke(null, new[] { app });
            }
        }

    }

}
