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

using Microsoft.Extensions.DependencyInjection;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class ServiceExtensions {

        public static void AutoRegisterServices(this IServiceCollection services, bool isCNamaKelas = true) {
            Type scopedRegistration = typeof(ScopedServiceRegistrationAttribute);
            Type singletonRegistration = typeof(SingletonServiceRegistrationAttribute);
            Type transientRegistration = typeof(TransientServiceRegistrationAttribute);

            var libAsm = Assembly.GetExecutingAssembly();
            var prgAsm = Assembly.GetEntryAssembly();

            IEnumerable<Type> libPrgAsmTypes = libAsm.GetTypes().Concat(prgAsm.GetTypes())
                .Where(p => (p.IsDefined(scopedRegistration, true) || p.IsDefined(transientRegistration, true) || p.IsDefined(singletonRegistration, true)) && !p.IsInterface);

            var types = libPrgAsmTypes.Select(cls => {
                string iName = $"I{cls.Name}";

                // CNamaKelas => INamaKelas
                if (isCNamaKelas && cls.Name.ToUpper().StartsWith("C")) {
                    iName = $"I{cls.Name[1..]}";
                }

                Type interfaceClass = cls.GetInterface(iName);
                if (interfaceClass == null) {
                    throw new Exception($"Interface {iName} Untuk Class {cls.Name} Tidak Ditemukan");
                }

                return new {
                    Service = interfaceClass,
                    Implementation = cls
                };
            }).Where(x => x.Service != null);

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
