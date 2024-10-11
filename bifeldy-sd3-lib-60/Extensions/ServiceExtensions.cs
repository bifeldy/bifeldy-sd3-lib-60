/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Memasang Dependency Injection Sesuai Service Attribute
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class ServiceExtensions {

        public static void AutoRegisterServices(this IServiceCollection services, IConfiguration configuration) {
            Type scopedRegistration = typeof(ScopedServiceRegistrationAttribute);
            Type singletonRegistration = typeof(SingletonServiceRegistrationAttribute);
            Type transientRegistration = typeof(TransientServiceRegistrationAttribute);

            var libTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(p => (p.IsDefined(scopedRegistration, true) || p.IsDefined(transientRegistration, true) || p.IsDefined(singletonRegistration, true)) && !p.IsInterface)
                .Select(cls => {
                    // CNamaKelas => INamaKelas
                    string iName = cls.Name;
                    if (iName.ToUpper().StartsWith("C")) {
                        iName = iName[1..];
                    }

                    return new {
                        Service = cls.GetInterface($"I{iName}"),
                        Implementation = cls
                    };
                }).Where(x => x.Service != null);

            var prgAsmTypes = Assembly.GetEntryAssembly().GetTypes()
                .Where(p => (p.IsDefined(scopedRegistration, true) || p.IsDefined(transientRegistration, true) || p.IsDefined(singletonRegistration, true)) && !p.IsInterface)
                .Select(cls => {
                    // CNamaKelas => INamaKelas
                    string iName = cls.Name;
                    if (iName.ToUpper().StartsWith("C")) {
                        iName = iName[1..];
                    }

                    return new {
                        Service = cls.GetInterface($"I{iName}"),
                        Implementation = cls
                    };
                }).Where(x => x.Service != null);

            var types = libTypes.Concat(prgAsmTypes).ToArray();
            foreach (var type in types) {
                if (type.Implementation.IsDefined(scopedRegistration, false)) {
                    _ = services.AddScoped(type.Service, type.Implementation);
                }

                if (type.Implementation.IsDefined(transientRegistration, false)) {
                    _ = services.AddTransient(type.Service, type.Implementation);
                }

                if (type.Implementation.IsDefined(singletonRegistration, false)) {
                    _ = services.AddSingleton(type.Service, type.Implementation);
                }
            }
        }

    }

}
