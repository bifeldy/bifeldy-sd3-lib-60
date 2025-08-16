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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class ServiceCollectionExtensions {

        public static void AutoRegisterServices(this IServiceCollection services, bool isCNamaKelas = true) {
            Type scopedRegistration = typeof(ScopedServiceRegistrationAttribute);
            Type singletonRegistration = typeof(SingletonServiceRegistrationAttribute);
            Type transientRegistration = typeof(TransientServiceRegistrationAttribute);

            var libAsm = Assembly.GetExecutingAssembly();
            var prgAsm = Assembly.GetEntryAssembly();

            IEnumerable<Type> libPrgAsmTypes = libAsm.GetTypes().Concat(prgAsm.GetTypes())
                .Where(p =>
                    (
                        p.IsDefined(scopedRegistration, true) ||
                        p.IsDefined(transientRegistration, true) ||
                        p.IsDefined(singletonRegistration, true)
                    ) &&
                    !p.IsInterface &&
                    p.IsClass
                );

            var types = libPrgAsmTypes.Select(cls => {
                string pName = $"P{cls.Name}"; // Grpc ProtoBuf
                string iName = $"I{cls.Name}"; // Interface

                // CNamaKelas => INamaKelas & PNamaKelas
                if (isCNamaKelas && cls.Name.ToUpper().StartsWith("C")) {
                    pName = $"P{cls.Name[1..]}";
                    iName = $"I{cls.Name[1..]}";
                }

                Type pCls = cls.GetInterface(pName); // Ga Punya Ya Skip Aja
                Type iCls = cls.GetInterface(iName);
                if (iCls == null) {
                    throw new Exception($"Interface {iName} Untuk Class {cls.Name} Tidak Ditemukan");
                }

                return new {
                    ProtoBuf = pCls,
                    Service = iCls,
                    Implementation = cls
                };
            }).Where(x => x.Service != null);

            foreach (var type in types) {
                if (type.Implementation.IsDefined(scopedRegistration, false)) {
                    _ = services.AddScoped(type.Service, type.Implementation);
                    if (type.ProtoBuf != null) {
                        _ = services.AddScoped(type.ProtoBuf, type.Implementation);
                    }
                }

                if (type.Implementation.IsDefined(transientRegistration, false)) {
                    _ = services.AddTransient(type.Service, type.Implementation);
                    if (type.ProtoBuf != null) {
                        _ = services.AddTransient(type.ProtoBuf, type.Implementation);
                    }
                }

                if (type.Implementation.IsDefined(singletonRegistration, false)) {
                    _ = services.AddSingleton(type.Service, type.Implementation);
                    if (type.ProtoBuf != null) {
                        _ = services.AddSingleton(type.ProtoBuf, type.Implementation);
                    }
                }
            }
        }

        /** */



        public static IServiceCollection SetupConfigOptions(
            this IServiceCollection isolatedServiceCollection,
            IServiceProvider sourceServiceProvider
        ) {
            Type genericOptions = typeof(IOptions<>);
            var genericOpenType = new HashSet<Type> {
                typeof(ILogger<>)
            };

            foreach (ServiceDescriptor serviceDescriptor in isolatedServiceCollection.ToArray()) {
                if (serviceDescriptor.ImplementationType != null) {
                    Type implType = serviceDescriptor.ImplementationType;

                    ConstructorInfo ctor = implType.GetConstructors().FirstOrDefault();
                    if (ctor != null) {
                        foreach (ParameterInfo param in ctor.GetParameters()) {
                            if (!param.ParameterType.IsGenericType) {
                                continue;
                            }

                            ServiceDescriptor descriptor = null;

                            Type genericDef = param.ParameterType.GetGenericTypeDefinition();
                            if (genericDef == genericOptions) {
                                Type optionsType = param.ParameterType;

                                if (!isolatedServiceCollection.Any(d => d.ServiceType == optionsType)) {
                                    descriptor = new ServiceDescriptor(
                                        optionsType,
                                        sp => sourceServiceProvider.GetRequiredService(optionsType),
                                        serviceDescriptor.Lifetime
                                    );
                                }
                            }
                            else {
                                foreach (Type type in genericOpenType) {
                                    if (serviceDescriptor.ImplementationType == null) {
                                        continue;
                                    }

                                    Type genericType = type.MakeGenericType(serviceDescriptor.ImplementationType);

                                    if (isolatedServiceCollection.Any(d => d.ServiceType == genericType)) {
                                        continue;
                                    }

                                    descriptor = new ServiceDescriptor(
                                        genericType,
                                        sp => sourceServiceProvider.GetRequiredService(genericType),
                                        serviceDescriptor.Lifetime
                                    );
                                }
                            }

                            if (descriptor != null) {
                                isolatedServiceCollection.Add(descriptor);
                            }
                        }
                    }
                }
            }

            

            return isolatedServiceCollection;
        }

        public static IServiceCollection AddForwardAllService(
            this IServiceCollection isolatedServiceCollection,
            IServiceCollection sourceServiceCollection,
            IServiceProvider sourceServiceProvider
        ) {
            IEnumerable<ServiceDescriptor> descriptors = sourceServiceCollection; // .Where(descriptor => IsSafeForForwarding(descriptor));

            foreach (ServiceDescriptor descriptor in descriptors) {
                Type serviceType = descriptor.ServiceType;

                if (
                    serviceType.IsGenericTypeDefinition ||
                    isolatedServiceCollection.Any(d => d.ServiceType == serviceType)
                ) {
                    continue;
                }

                var forwardingDescriptor = new ServiceDescriptor(
                    serviceType,
                    sp => sourceServiceProvider.GetRequiredService(serviceType),
                    descriptor.Lifetime
                );

                isolatedServiceCollection.Add(forwardingDescriptor);
            }

            return isolatedServiceCollection;
        }

        // private static bool IsSafeForForwarding(ServiceDescriptor descriptor) {
        //     string ns = descriptor.ServiceType.Namespace ?? string.Empty;
        // 
        //     // Skip framework-related services or hosted infrastructure
        //     string[] notAllowedNamespace = new string[] {
        //         "System",
        //         "Microsoft.AspNetCore",
        //         "Microsoft.Extensions.DependencyInjection",
        //         "Microsoft.Extensions.Diagnostics",
        //         "Microsoft.Extensions.Hosting",
        //         "Microsoft.Extensions.Options"
        //     };
        // 
        //     if (notAllowedNamespace.Any(prefix => ns.ToUpper().StartsWith(prefix.ToUpper()))) {
        //         return false;
        //     }
        // 
        //     // // Optionally skip IHostedService, IHttpClientFactory, etc.
        //     // // You can add more filters based on your app
        //     // var notAllowedType = new HashSet<Type> {
        //     //     typeof(IHostedService),
        //     //     typeof(IHttpClientFactory),
        //     //     typeof(IHttpContextAccessor),
        //     //     typeof(IHostApplicationLifetime),
        //     // };
        //     // 
        //     // if (notAllowedType.Contains(descriptor.ServiceType)) {
        //     //     return false;
        //     // }
        // 
        //     return true;
        // }

    }

}
