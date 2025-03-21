/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Bifeldy's Initial Application
 * 
 */

using System.Globalization;
using System.Reflection;
using System.Net;

using Helmet;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

using DinkToPdf;
using DinkToPdf.Contracts;

using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

using Quartz;

using Serilog;
using Serilog.Events;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Backgrounds;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Grpcs;
using bifeldy_sd3_lib_60.Libraries;
using bifeldy_sd3_lib_60.Middlewares;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60 {

    public static class Bifeldy {

        public static List<string> GRPC_ROUTH_PATH = new();
        public static bool IS_USING_API_KEY = false;

        public static readonly string DEFAULT_DATA_FOLDER = "_data";

        public static WebApplicationBuilder Builder = null;
        public static IServiceCollection Services = null;
        public static IConfiguration Config = null;
        public static WebApplication App = null;

        // private static readonly Dictionary<string, KeyValuePair<IJobDetail, ITrigger>> jobList = new();
        private static readonly Dictionary<string, Dictionary<string, Type>> jobList = new();

        private static string NginxPathName = "x-forwarded-prefix";

        public static void AppContextOverride() {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            // AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        /* ** */

        public static void InitBuilder(WebApplicationBuilder builder) {
            Builder = builder;
            Services = builder.Services;
            Config = builder.Configuration;
            //
            _ = Services.Configure<JsonOptions>(opt => {
                opt.SerializerOptions.Converters.Add(new DecimalSystemTextJsonConverter());
            });
        }

        public static void InitApp(WebApplication app) => App = app;

        public static void SetKestrelApiGrpcPort(IConfigurationSection configuration = null) {
            IConfigurationSection config = configuration ?? Config.GetSection("ENV");
            IDictionary<string, dynamic> cfg = config.Get<IDictionary<string, dynamic>>();

            // Web Api Seperti Biasa
            string apiEnvName = "API_PORT";
            string apiPortEnv = Environment.GetEnvironmentVariable(apiEnvName);
            int webApiPort = int.Parse(apiPortEnv ?? cfg[apiEnvName]);

            // GRPC Pakai HTTP/2 Doank :: Cek Environment / appsettings.json
            string grpcEnvName = "GRPC_PORT";
            string grpcPortEnv = Environment.GetEnvironmentVariable(grpcEnvName);
            int grpcPort = int.Parse(grpcPortEnv ?? cfg[grpcEnvName]);

            Console.WriteLine($"=> Running Port :: {webApiPort} (API) :: {grpcPort} (GRPC)");

            _ = Builder.WebHost.ConfigureKestrel(options => {
                options.Listen(IPAddress.Any, webApiPort, listenOptions => {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
                options.Listen(IPAddress.Any, grpcPort, listenOptions => {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });
        }

        /* ** */

        public static void SetupSerilog() {
            _ = Builder.Host.UseSerilog((hostContext, services, configuration) => {
                string appPathDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                _ = configuration.WriteTo.File(appPathDir + $"/{DEFAULT_DATA_FOLDER}/logs/error_.txt", restrictedToMinimumLevel: LogEventLevel.Error, rollingInterval: RollingInterval.Day);
            });
        }

        public static void UseSerilog() {
            _ = App.UseSerilogRequestLogging(o => {
                o.MessageTemplate = "{RemoteOrigin} :: {RemoteIpAddress} :: {RequestMethod} :: {RequestPath} :: {StatusCode} :: {Elapsed:0.0000} ms";
                // o.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Error;
                o.EnrichDiagnosticContext = (diagnosticContext, httpContext) => {
                    IGlobalService gs = httpContext.RequestServices.GetRequiredService<IGlobalService>();
                    string origin = gs.GetIpOriginData(httpContext.Connection, httpContext.Request);
                    diagnosticContext.Set("RemoteOrigin", origin);
                    string ipAddr = gs.GetIpOriginData(httpContext.Connection, httpContext.Request, true);
                    diagnosticContext.Set("RemoteIpAddress", ipAddr);
                };
            });
        }

        /* ** */

        public static void AddSwagger(
            string apiUrlPrefix = "api",
            string docsTitle = null,
            string docsDescription = null,
            bool enableApiKey = true,
            bool enableJwt = false
        ) {
            _ = Services.AddSwaggerGen(c => {
                c.EnableAnnotations();
                c.SwaggerDoc(apiUrlPrefix, new OpenApiInfo() {
                    Title = docsTitle ?? Assembly.GetEntryAssembly().GetName().Name,
                    Description = docsDescription ?? "API Documentation ~"
                });
                if (enableApiKey) {
                    var apiKey = new OpenApiSecurityScheme() {
                        Description = @"API-Key Origin. Example: 'http://.../...?key=000...'",
                        Name = "key",
                        In = ParameterLocation.Query,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "ApiKey",
                        Reference = new OpenApiReference() {
                            Id = "api_key",
                            Type = ReferenceType.SecurityScheme
                        }
                    };
                    c.AddSecurityDefinition(apiKey.Reference.Id, apiKey);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                        { apiKey, Array.Empty<string>() }
                    });
                }

                if (enableJwt) {
                    var jwt = new OpenApiSecurityScheme() {
                        Description = @"Authorization Header. Example: 'Bearer eyj...'",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        Reference = new OpenApiReference() {
                            Id = "jwt",
                            Type = ReferenceType.SecurityScheme
                        }
                    };
                    c.AddSecurityDefinition(jwt.Reference.Id, jwt);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                        { jwt, Array.Empty<string>() }
                    });
                }

                c.OperationFilter<SwaggerMediaTypesOperationFilter>();
                c.SchemaFilter<SwaggerHideJsonPropertyFilter>();
                c.TagActionsBy(api => {
                    if (api.GroupName != null) {
                        return new[] { api.GroupName };
                    }

                    if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor) {
                        return new[] { controllerActionDescriptor.ControllerName };
                    }

                    throw new InvalidOperationException("Tidak Ada Tag [ApiExplorerSettings(GroupName = \"...\")]");
                });
                c.DocInclusionPredicate((name, api) => true);
            });
        }

        public static void UseSwagger(
            string apiUrlPrefix = "api",
            string proxyHeaderName = "x-forwarded-prefix"
        ) {
            NginxPathName = proxyHeaderName;
            _ = App.UseSwagger(c => {
                c.RouteTemplate = "{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, request) => {
                    var openApiServers = new List<OpenApiServer>();

                    string proxyPath = request.Headers[proxyHeaderName];
                    if (!string.IsNullOrEmpty(proxyPath)) {
                        openApiServers.Add(new OpenApiServer() {
                            Description = "Reverse Proxy Path",
                            Url = proxyPath.StartsWith("/") || proxyPath.StartsWith("http") ? proxyPath : $"/{proxyPath}"
                        });
                    }

                    openApiServers.Add(new OpenApiServer() {
                        Description = "Direct IP Server",
                        Url = "/"
                    });
                    swaggerDoc.Servers = openApiServers;
                });
            });
            _ = App.UseSwaggerUI(c => {
                c.RoutePrefix = apiUrlPrefix;
                c.SwaggerEndpoint("swagger.json", apiUrlPrefix);
                c.DefaultModelsExpandDepth(-1);
            });
        }

        /* ** */

        public static void LoadConfig(IConfigurationSection configuration = null) {
            _ = Services.Configure<EnvVar>(configuration ?? Config.GetSection("ENV"));
        }

        public static void AddDependencyInjection(bool isBlazorWebApp = true) {
            _ = Services.AddHttpContextAccessor();

            // --
            _ = Services.AddDbContext<IOracle, COracle>(ServiceLifetime.Transient);
            _ = Services.AddDbContext<IPostgres, CPostgres>(ServiceLifetime.Transient);
            _ = Services.AddDbContext<IMsSQL, CMsSQL>(ServiceLifetime.Transient);
            // --
            _ = Services.AddTransient<IOraPg>(sp => {
                EnvVar _envVar = sp.GetRequiredService<IOptions<EnvVar>>().Value;
                return _envVar.IS_USING_POSTGRES ? sp.GetRequiredService<IPostgres>() : sp.GetRequiredService<IOracle>();
            });

            if (isBlazorWebApp) {
                // --
                // Setiap Request Cycle 1 Scope 1x New Object 1x Sesion Saja
                // --
                _ = Services.AddScoped<ProtectedSessionStorage>();
                _ = Services.AddScoped<AuthenticationStateProvider, BlazorAuthenticationStateProvider>();
            }

            // --
            // Hanya Singleton Yang Leluasa Dengan Mudahnya Bisa Di Inject Di Constructor() { } Dimana Saja
            // --
            _ = Services.AddSingleton<IConverter>(sp => {
                return new SynchronizedConverter(new PdfTools());
            });
        }

        /* ** */

        public static void AddGrpc(bool useJwt = false) {
            InheritedProtoMemberAttribute.AddInheritedMembersIn();
            _ = Services.AddCodeFirstGrpc(opt => {
                if (useJwt) {
                    opt.Interceptors.Add<CGRpcServerInterceptor>();
                }

                opt.MaxReceiveMessageSize = null;
                opt.EnableDetailedErrors = true;
            });
            _ = Services.AddSingleton(
                BinderConfiguration.Create(
                    binder: new GrpcBinder(Services)
                )
            );
            _ = Services.AddCodeFirstGrpcReflection();
        }

        public static void AutoMapGrpcService() {
            App.AutoMapGrpcService();
            _ = App.MapCodeFirstGrpcReflectionService();
        }

        /* ** */

        public static void UseInvariantCulture() {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        public static void UseCultureLocalization() {
            CultureInfo[] supportedCultures = new[] {
                CultureInfo.InvariantCulture,
                new CultureInfo("en-US"),
                new CultureInfo("id-ID")
            };
            _ = App.UseRequestLocalization(new RequestLocalizationOptions() {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
        }

        /* ** */

        private static List<EJenisDc> CheckKafkaExcludeJenisDc(string excludeJenisDc) {
            List<EJenisDc> ls = null;

            if (!string.IsNullOrEmpty(excludeJenisDc)) {
                ls = new List<EJenisDc>(excludeJenisDc.Split(",").Where(d => !string.IsNullOrEmpty(d)).Select(d => {
                    string jenisDc = d.Trim().ToUpper();
                    EJenisDc _eJenisDc = EJenisDc.UNKNOWN;

                    if (Enum.TryParse(jenisDc, true, out EJenisDc eJenisDc)) {
                        _eJenisDc = eJenisDc;
                    }

                    return _eJenisDc;
                }));
            }

            return ls;
        }

        public static void AddKafkaProducerBackground(string hostPort, string topicName, short replication = 1, int partition = 1, bool suffixKodeDc = false, string excludeJenisDc = null, string pubSubName = null) {
            _ = Services.AddHostedService(sp => {
                List<EJenisDc> ls = CheckKafkaExcludeJenisDc(excludeJenisDc);
                return new CKafkaProducer(sp, hostPort, topicName, replication, partition, suffixKodeDc, ls, pubSubName);
            });
        }

        public static void AddKafkaConsumerBackground(string hostPort, string topicName, string logTableName = null, string groupId = null, bool suffixKodeDc = false, string excludeJenisDc = null, string pubSubName = null) {
            _ = Services.AddHostedService(sp => {
                List<EJenisDc> ls = CheckKafkaExcludeJenisDc(excludeJenisDc);
                return new CKafkaConsumer(sp, hostPort, topicName, logTableName, groupId, suffixKodeDc, ls, pubSubName);
            });
        }

        public static void AddKafkaAutoProducerConsumerBackground() {
            IDictionary<string, KafkaInstance> kafkaSettings = Config.GetSection("KAFKA").Get<IDictionary<string, KafkaInstance>>();
            if (kafkaSettings != null) {
                foreach (KeyValuePair<string, KafkaInstance> ks in kafkaSettings) {
                    if (ks.Key.StartsWith("PRODUCER_")) {
                        AddKafkaProducerBackground(ks.Value.HOST_PORT, ks.Value.TOPIC, ks.Value.REPLICATION, ks.Value.PARTITION, ks.Value.SUFFIX_KODE_DC, ks.Value.EXCLUDE_JENIS_DC, ks.Key);
                    }
                }

                foreach (KeyValuePair<string, KafkaInstance> ks in kafkaSettings) {
                    if (ks.Key.StartsWith("CONSUMER_")) {
                        AddKafkaConsumerBackground(ks.Value.HOST_PORT, ks.Value.TOPIC, ks.Value.LOG_TABLE_NAME, ks.Value.GROUP_ID, ks.Value.SUFFIX_KODE_DC, ks.Value.EXCLUDE_JENIS_DC, ks.Key);
                    }
                }
            }
        }

        /* ** */

        public static void StartJobScheduler() {
            _ = Services.AddQuartz(opt => {
                // opt.UseMicrosoftDependencyInjectionJobFactory();
                foreach (KeyValuePair<string, Dictionary<string, Type>> jl in jobList) {
                    string cronString = jl.Key;
                    foreach (KeyValuePair<string, Type> kvp in jl.Value) {
                        var jobKey = new JobKey(kvp.Key);
                        _ = opt.AddJob(kvp.Value, jobKey);
                        _ = opt.AddTrigger(t => t.ForJob(jobKey).WithCronSchedule(jl.Key));
                    }
                }
            });
            _ = Services.AddQuartzHostedService(opt => {
                opt.WaitForJobsToComplete = true;
            });
        }

        public static void CreateJobSchedule(string quartzCronString, Type classInheritFromCQuartzJobScheduler) {
            if (jobList.ContainsKey(quartzCronString)) {
                if (!jobList[quartzCronString].ContainsKey(classInheritFromCQuartzJobScheduler.Name)) {
                    jobList[quartzCronString].Add(classInheritFromCQuartzJobScheduler.Name, classInheritFromCQuartzJobScheduler);
                }
            }
            else {
                var dict = new Dictionary<string, Type> {
                    { classInheritFromCQuartzJobScheduler.Name, classInheritFromCQuartzJobScheduler }
                };
                jobList.Add(quartzCronString, dict);
            }
        }

        // https://www.freeformatter.com/cron-expression-generator-quartz.html
        public static void CreateJobSchedule<T>(string quartzCronString) => CreateJobSchedule(quartzCronString, typeof(T));

        public static void CreateJobSchedules(string quartzCronString, params Type[] classInheritFromCQuartzJobScheduler) {
            for (int i = 0; i < classInheritFromCQuartzJobScheduler.Length; i++) {
                CreateJobSchedule(quartzCronString, classInheritFromCQuartzJobScheduler[i]);
            }
        }

        /* ** */

        public static void Handle404ApiNotFound(string apiUrlPrefix = "api") {
            _ = App.Map("/" + apiUrlPrefix + "/{*url:regex(^(?!swagger).*$)}", async context => {
                HttpResponse response = context.Response;
                response.Clear();
                response.StatusCode = StatusCodes.Status404NotFound;
                await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "404 - Whoops :: API Tidak Ditemukan",
                    result = new ResponseJsonMessage() {
                        message = $"Silahkan Periksa Kembali Dokumentasi API"
                    }
                });
            });
        }

        /* ** */

        public static void UseNginxProxyPathSegment() {
            _ = App.Use(async (context, next) => {
                if (context.Request.Headers.TryGetValue(NginxPathName, out StringValues pathBase)) {
                    context.Request.PathBase = pathBase.Last();
                    if (context.Request.Path.StartsWithSegments(context.Request.PathBase, out PathString path)) {
                        context.Request.Path = path;
                    }
                }

                await next();
            });
        }

        public static void UseForwardedHeaders() {
            _ = App.UseForwardedHeaders(
                new ForwardedHeadersOptions() {
                    ForwardedHeaders = ForwardedHeaders.All
                }
            );
        }

        public static void UseHelmet() {
            _ = App.UseHelmet(o => {
                o.UseContentSecurityPolicy = false; // Buat Web Socket (Blazor SignalR, Socket.io, Web RTC)
                o.UseXContentTypeOptions = false; // Boleh Content-Sniff :: .mkv Dibaca .mp4
                o.UseReferrerPolicy = false; // Kalau Pakai Service Worker (Gak Set Origin, Tapi Referrer)
            });
        }

        public static void UseErrorHandlerMiddleware(string apiUrlPrefix = "api") {
            _ = App.Use(async (context, next) => {

                // Khusus API Path :: Akan Di Handle Error Dengan Balikan Data JSON
                // Selain Itu Atau Jika Masih Ada Error Lain
                // Misal Di Catch Akan Terlempar Ke Halaman Error Bawaan UI

                if (!context.Request.Path.Value.StartsWith($"/{apiUrlPrefix}/")) {
                    await next();
                }
                else {
                    try {
                        await next();
                    }
                    catch (Exception ex) {
                        try {
                            HttpResponse response = context.Response;

                            response.Clear();
                            response.StatusCode = StatusCodes.Status500InternalServerError;
                            await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                                info = "500 - Whoops :: Terjadi Kesalahan",
                                result = new ResponseJsonMessage() {
                                    message = App.Environment.IsDevelopment() ? ex.Message : "Gagal Memproses Data"
                                }
                            });
                        }
                        catch {
                            // Response has been sent ~
                        }
                    }
                }
            });
        }

        public static void UseSecretMiddleware() => App.UseMiddleware<SecretMiddleware>();

        public static void UseApiKeyMiddleware() {
            _ = App.UseMiddleware<ApiKeyMiddleware>();
            IS_USING_API_KEY = true;
        }

        public static void UseJwtMiddleware() => App.UseMiddleware<JwtMiddleware>();

    }

}
