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

using System.CodeDom.Compiler;
using System.Reflection;
using System.ServiceModel;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

using ProtoBuf.Grpc.Reflection;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class WebApplicationExtensions {

        public static void AutoMapGrpcService(this WebApplication app) {
            Type serviceContract = typeof(ServiceContractAttribute);
            Type generatedCode = typeof(GeneratedCodeAttribute);

            string dirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Bifeldy.DEFAULT_DATA_FOLDER, "protobuf-net");
            DirectoryInfo di = Directory.CreateDirectory(dirPath);
            foreach (FileInfo fi in di.GetFiles()) {
                if (fi.Name != "bcl.proto") {
                    fi.Delete();
                }
            }

            var libAsm = Assembly.GetExecutingAssembly();
            var prgAsm = Assembly.GetEntryAssembly();

            IEnumerable<Type> grpcServices = libAsm.GetTypes().Concat(prgAsm.GetTypes())
                .Where(p => p.IsDefined(serviceContract, true) && !p.IsDefined(generatedCode, true) && p.IsInterface);

            foreach (Type grpcService in grpcServices) {
                string[] fullName = grpcService.FullName.Split(".");
                var arrFn = fullName.Where(fn => fn != grpcService.Name).ToList();
                string name = grpcService.Name.StartsWith("I") ? grpcService.Name[1..] : grpcService.Name;
                arrFn.Add(name);
                string grpcRoute = string.Join(".", arrFn);
                if (!Bifeldy.GRPC_ROUTH_PATH.Contains(grpcRoute)) {
                    Bifeldy.GRPC_ROUTH_PATH.Add(grpcRoute);
                }

                MethodInfo mapGrpcService = typeof(GrpcEndpointRouteBuilderExtensions).GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService)).MakeGenericMethod(grpcService);
                _ = mapGrpcService.Invoke(null, new[] { app });

                var schemaGenerator = new SchemaGenerator();
                string schema = schemaGenerator.GetSchema(grpcService);
                File.WriteAllText(Path.Combine(dirPath, $"{grpcService}.proto"), schema);
            }
        }

        public static List<string> AutoMapHubService(this WebApplication app, string signalrPrefixHub, Action<HttpConnectionDispatcherOptions> configureOptions = null) {
            var libAsm = Assembly.GetExecutingAssembly();
            var prgAsm = Assembly.GetEntryAssembly();

            IEnumerable<Type> hubServices = libAsm.GetTypes().Concat(prgAsm.GetTypes())
                .Where(c => c.IsClass && !c.IsAbstract && c.IsPublic && typeof(Hub).IsAssignableFrom(c));

            var signalrHubPathList = new List<string>();

            foreach (Type hubService in hubServices) {
                string urlPathPascaCaseToKebabCase = Regex.Replace(hubService.Name, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])", "-$1", RegexOptions.Compiled).Trim().ToLower();

                if (!signalrPrefixHub.StartsWith("/")) {
                    signalrPrefixHub = $"/{signalrPrefixHub}";
                }

                if (!urlPathPascaCaseToKebabCase.StartsWith("/")) {
                    urlPathPascaCaseToKebabCase = $"/{urlPathPascaCaseToKebabCase}";
                }

                if (urlPathPascaCaseToKebabCase.EndsWith("-hub")) {
                    urlPathPascaCaseToKebabCase = urlPathPascaCaseToKebabCase.Replace("-hub", string.Empty);
                }

                string signalrHubPath = signalrPrefixHub + urlPathPascaCaseToKebabCase;

                if (configureOptions == null) {
                    _ = typeof(HubEndpointRouteBuilderExtensions).GetMethod(
                        nameof(HubEndpointRouteBuilderExtensions.MapHub),
                        BindingFlags.Static | BindingFlags.Public,
                        new[] { typeof(IEndpointRouteBuilder), typeof(string) }
                    ).MakeGenericMethod(hubService).Invoke(null, new dynamic[] { app, signalrHubPath });
                }
                else {
                    _ = typeof(HubEndpointRouteBuilderExtensions).GetMethod(
                        nameof(HubEndpointRouteBuilderExtensions.MapHub),
                        BindingFlags.Static | BindingFlags.Public,
                        new[] { typeof(IEndpointRouteBuilder), typeof(string), typeof(Action<HttpConnectionDispatcherOptions>) }
                    ).MakeGenericMethod(hubService).Invoke(null, new dynamic[] { app, signalrHubPath, configureOptions });
                }

                string hubPath = signalrHubPath;
                if (hubPath.StartsWith("/")) {
                    hubPath = hubPath[1..];
                }

                signalrHubPathList.Add(hubPath);
            }

            return signalrHubPathList;
        }

    }

}
