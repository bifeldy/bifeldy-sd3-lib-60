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

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;

using Helmet;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

using DinkToPdf;
using DinkToPdf.Contracts;

using Grpc.Net.Client.Balancer;

using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

using Quartz;

using Serilog;
using Serilog.Events;

using StackExchange.Redis;

using Swashbuckle.AspNetCore.Swagger;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Backgrounds;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Grpcs;
using bifeldy_sd3_lib_60.Libraries;
using bifeldy_sd3_lib_60.Middlewares;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Plugins;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60 {

    public static class Bifeldy {

        public const string DEFAULT_DATA_FOLDER = "_data";

        public static List<string> GRPC_ROUTH_PATH = new();
        public static List<string> SIGNALR_ROUTH_PATH = new();

        public static string PLUGINS_PROJECT_NAMESPACE = null;

        public static bool IS_USING_REQUEST_LOGGER = false;
        public static bool IS_USING_SECRET = false;
        public static bool IS_USING_API_KEY = false;
        public static bool IS_USING_JWT = false;

        public static string SIGNALR_PREFIX_HUB = "/signalr";
        public static string NGINX_PATH_NAME = "x-forwarded-prefix";

        public static WebApplicationBuilder Builder = null;
        public static IServiceCollection Services = null;
        public static IConfiguration Config = null;
        public static WebApplication App = null;
        public static ApplicationPartManager Apm = null;

        private static readonly Dictionary<string, Dictionary<string, Type>> jobList = new(StringComparer.InvariantCultureIgnoreCase);

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

        public static void SetKestrelApiGrpcPort(IConfigurationSection configuration = null, bool enableGrpc = true) {
            IConfigurationSection config = configuration ?? Config.GetSection("ENV");
            IDictionary<string, dynamic> cfg = config.Get<IDictionary<string, dynamic>>();

            // Web Api Seperti Biasa
            string apiEnvName = "API_PORT";
            string apiPortEnv = Environment.GetEnvironmentVariable(apiEnvName);
            int webApiPort = int.Parse(apiPortEnv ?? cfg[apiEnvName]);

            string logInfo = $"=> Running Port :: {webApiPort} (API)";

            int grpcPort = -1;
            if (enableGrpc) {
                // GRPC Pakai HTTP/2 Doank :: Cek Environment / appsettings.json
                string grpcEnvName = "GRPC_PORT";
                string grpcPortEnv = Environment.GetEnvironmentVariable(grpcEnvName);
                grpcPort = int.Parse(grpcPortEnv ?? cfg[grpcEnvName]);
                logInfo += $" :: {grpcPort} (GRPC)";
            }

            Console.WriteLine(logInfo);

            _ = Builder.WebHost.ConfigureKestrel(options => {
                options.Listen(IPAddress.Any, webApiPort, listenOptions => {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });

                if (enableGrpc && grpcPort >= 0) {
                    options.Listen(IPAddress.Any, grpcPort, listenOptions => {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                }
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
                o.MessageTemplate = "{TraceId} :: {RemoteOriginIpAddress} :: {RequestMethod} :: {RequestPath} :: {StatusCode} :: {Elapsed:0.0000} ms";
                // o.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Error;
                o.EnrichDiagnosticContext = (diagnosticContext, httpContext) => {
                    diagnosticContext.Set("TraceId", Activity.Current?.Id ?? httpContext?.TraceIdentifier);

                    IGlobalService gs = httpContext.RequestServices.GetRequiredService<IGlobalService>();
                    string ipAddr = gs.GetIpOriginData(httpContext.Connection, httpContext.Request, true, true);
                    string origin = gs.GetIpOriginData(httpContext.Connection, httpContext.Request, removeReverseProxyRoute: true);
                    diagnosticContext.Set("RemoteOriginIpAddress", origin == ipAddr ? origin : $"{origin}@{ipAddr}");
                };
            });
        }

        /* ** */

        public static void AddDynamicApiPluginRouteEndpoint(string projectNamespace, string dataFolderName = "plugins") {
            PLUGINS_PROJECT_NAMESPACE = projectNamespace;

            string pluginFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_DATA_FOLDER, dataFolderName);
            if (!Directory.Exists(pluginFolderPath)) {
                _ = Directory.CreateDirectory(pluginFolderPath);
            }

            _ = Services.AddSingleton<IActionDescriptorChangeProvider>(CDynamicActionDescriptorChangeProvider.Instance);
            _ = Services.AddSingleton(CDynamicActionDescriptorChangeProvider.Instance);

            _ = Services.AddSingleton<IPluginContext>(sp => {
                ILogger<CPluginContext> logger = sp.GetRequiredService<ILogger<CPluginContext>>();
                IOptions<EnvVar> envVar = sp.GetRequiredService<IOptions<EnvVar>>();
                return new CPluginContext(pluginFolderPath, logger, envVar, sp) {
                    PartManager = Apm
                };
            });

            _ = Services.AddSingleton(sp => {
                IPluginContext pluginContext = sp.GetRequiredService<IPluginContext>();
                return new CPluginWatcherCoordinator(dataFolderName, pluginContext);
            });
        }

        public static IMvcBuilder AddControllers(string apiUrlPrefix = "api") {
            IMvcBuilder mvcBuilder = Services.AddControllers(x => {
                x.UseRoutePrefix(apiUrlPrefix);
                x.UseHideControllerEndPointDc(Services);
            }).AddJsonOptions(x => {
                x.JsonSerializerOptions.PropertyNamingPolicy = null;
                x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            }).AddXmlSerializerFormatters(); // .PartManager.ApplicationParts.Add(new AssemblyPart(typeof(Bifeldy).Assembly));

            if (!string.IsNullOrEmpty(PLUGINS_PROJECT_NAMESPACE)) {
                mvcBuilder = mvcBuilder.ConfigureApplicationPartManager(apm => {
                    Apm = apm;
                });
            }

            return mvcBuilder;
        }

        public static void AddSwagger(
            string swaggerPrefix = "api",
            string docsTitle = null,
            string docsDescription = null,
            bool enableApiKey = true,
            bool enableJwt = false
        ) {
            _ = Services.AddEndpointsApiExplorer();

            _ = Services.AddSwaggerGen(c => {
                c.EnableAnnotations();

                if (!string.IsNullOrEmpty(PLUGINS_PROJECT_NAMESPACE)) {
                    swaggerPrefix = Assembly.GetEntryAssembly().GetName().Version.ToString();
                }

                c.SwaggerDoc(swaggerPrefix, new OpenApiInfo() {
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

        public static void UseDynamicApiPluginRouteEndpoint(string dataFolderName = "plugins") {
            CPluginWatcherCoordinator pwc = App.Services.GetRequiredService<CPluginWatcherCoordinator>();

            string pluginFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_DATA_FOLDER, dataFolderName);
            CPluginLoaderForSwagger.LoadAllPlugins(pwc.Context, pluginFolderPath);

            IWebHostEnvironment environment = App.Services.GetRequiredService<IWebHostEnvironment>();
            ISwaggerProvider provider = App.Services.GetRequiredService<ISwaggerProvider>();

            if (!Directory.Exists(environment.WebRootPath)) {
                _ = Directory.CreateDirectory(environment.WebRootPath);
            }

            string appVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            OpenApiDocument swaggerDoc = provider.GetSwagger(appVersion);

            string jsonPath = Path.Combine(environment.WebRootPath, "swagger.json");
            using (var streamWriter = new StreamWriter(jsonPath)) {
                var writer = new OpenApiJsonWriter(streamWriter);
                swaggerDoc.SerializeAsV3(writer);
            }

            CPluginLoaderForSwagger.RegisterSwaggerReload(pwc.Context);
        }

        public static void UseSwagger(
            string apiUrlPrefix = "api",
            string proxyHeaderName = "x-forwarded-prefix"
        ) {
            NGINX_PATH_NAME = proxyHeaderName;

            _ = App.UseSwagger(c => {
                c.RouteTemplate = "{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, request) => {
                    var openApiServers = new List<OpenApiServer>();

                    if (request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
                        string proxyPath = pathBase.Last();
                        if (!string.IsNullOrEmpty(proxyPath)) {
                            openApiServers.Add(new OpenApiServer() {
                                Description = "Reverse Proxy Path",
                                Url = proxyPath.StartsWith("/") || proxyPath.StartsWith("http") ? proxyPath : $"/{proxyPath}"
                            });
                        }
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

                string swaggerUrl = "swagger.json";
                string swaggerName = apiUrlPrefix;

                if (!string.IsNullOrEmpty(PLUGINS_PROJECT_NAMESPACE)) {
                    swaggerUrl = $"/swagger.json?v={DateTime.UtcNow.Ticks}";
                    swaggerName = Assembly.GetEntryAssembly().GetName().Version.ToString();
                }

                c.SwaggerEndpoint(swaggerUrl, swaggerName);

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
            // Transient Selalu Dapat Object Baru ~
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
            // --
            _ = Services.AddSingleton<IScheduler>(sp => {
                ISchedulerFactory scheduler = sp.GetRequiredService<ISchedulerFactory>();
                return scheduler.GetScheduler().Result;
            });
        }

        /* ** */

        public static void AddGrpcCodeFirst(bool useAuth = false) {
            InheritedProtoMemberAttribute.AddInheritedMembersIn();
            _ = Services.AddCodeFirstGrpc(opt => {
                if (useAuth) {
                    opt.Interceptors.Add<CGRpcServerInterceptor>();
                }

                opt.MaxReceiveMessageSize = null;
                opt.EnableDetailedErrors = true;
            });
            _ = Services.AddSingleton(
                BinderConfiguration.Create(
                    binder: new CGRpcBinder(Services)
                )
            );
            _ = Services.AddCodeFirstGrpcReflection();
        }

        public static void AddCustomGrpcResolver<T>(LoadBalancerFactory customLoadBalancerFactory = null) where T : ResolverFactory {
            _ = Services.AddSingleton<ResolverFactory, T>();
            _ = Services.AddSingleton(typeof(LoadBalancerFactory), customLoadBalancerFactory ?? new CGRpcRandomBalancerFactory());
        }

        public static List<string> AutoMapGrpcService() {
            List<string> route = App.AutoMapGrpcService();
            _ = App.MapCodeFirstGrpcReflectionService();
            return route;
        }

        /* ** */

        public static void AddSignalR(IConfigurationSection configuration = null, Action<HubOptions> hubOptions = null) {
            ISignalRServerBuilder signalR = null;

            if (hubOptions == null) {
                signalR = Services.AddSignalRCore();
            }
            else {
                signalR = Services.AddSignalR(hubOptions);
            }

            _ = signalR.AddJsonProtocol(options => {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });

            IConfigurationSection config = configuration ?? Config.GetSection("ENV");
            IDictionary<string, dynamic> cfg = config.Get<IDictionary<string, dynamic>>();

            string redisEnvName = "REDIS";
            string redisEnvVal = Environment.GetEnvironmentVariable(redisEnvName);

            string redisConstStr = redisEnvVal ?? cfg[redisEnvName];
            if (!string.IsNullOrEmpty(redisConstStr)) {
                _ = signalR.AddStackExchangeRedis(redisConstStr, options => {
                    options.Configuration.ChannelPrefix = new RedisChannel($"{App.Environment.ApplicationName}_SignalR", RedisChannel.PatternMode.Literal);
                });
            }
        }

        public static List<string> AutoMapHubService(string signalrPrefixHub = "/signalr", Action<HttpConnectionDispatcherOptions> configureOptions = null) {
            return App.AutoMapHubService(signalrPrefixHub, configureOptions);
        }

        /* ** */

        public static void AddRedisDistributedCache(IConfigurationSection configuration = null) {
            IConfigurationSection config = configuration ?? Config.GetSection("ENV");
            IDictionary<string, dynamic> cfg = config.Get<IDictionary<string, dynamic>>();

            string redisEnvName = "REDIS";
            string redisEnvVal = Environment.GetEnvironmentVariable(redisEnvName);

            string redisConstStr = redisEnvVal ?? cfg[redisEnvName];
            if (string.IsNullOrEmpty(redisConstStr)) {
                _ = Services.AddDistributedMemoryCache(options => {
                    // No Additional Config ~
                });
            }
            else {
                _ = Services.AddStackExchangeRedisCache(options => {
                    options.Configuration = redisConstStr;
                    options.InstanceName = $"{App.Environment.ApplicationName}_Cache";
                });
            }
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
                var dict = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase) {
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

        public static void UseNginxProxyPathSegment() {
            _ = App.Use(async (context, next) => {
                if (context.Request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
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

        public static void Handle500ApiError<T>(string apiUrlPrefix = "api") {
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
                        ILogger<T> _logger = context.RequestServices.GetRequiredService<ILogger<T>>();

                        var user = (UserApiSession)context.Items["user"];

                        HttpRequest request = context.Request;
                        HttpResponse response = context.Response;

                        response.Clear();

                        string xRequestTraceProxy = null;
                        if (response.Headers.ContainsKey("x-request-trace-proxy")) {
                            xRequestTraceProxy = response.Headers["x-request-trace-proxy"];
                        }
                        else {
                            if (request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
                                string proxyPath = pathBase.Last();
                                if (!string.IsNullOrEmpty(proxyPath)) {
                                    xRequestTraceProxy = proxyPath;
                                    response.Headers.Add("x-request-trace-proxy", xRequestTraceProxy);
                                }
                            }
                        }

                        string xRequestTraceActivity = null;
                        if (response.Headers.ContainsKey("x-request-trace-activity")) {
                            xRequestTraceActivity = response.Headers["x-request-trace-activity"];
                        }
                        else {
                            xRequestTraceActivity = Activity.Current?.Id;
                            response.Headers.Add("x-request-trace-activity", xRequestTraceActivity);
                        }

                        string xRequestTraceId = null;
                        if (response.Headers.ContainsKey("x-request-trace-id")) {
                            xRequestTraceId = response.Headers["x-request-trace-id"];
                        }
                        else {
                            xRequestTraceId = context?.TraceIdentifier;
                            response.Headers.Add("x-request-trace-id", xRequestTraceId);
                        }

                        _logger.LogError(
                            "[GLOBAL_ERROR_HANDLER] {TraceId} {xRequestTraceProxy} 💣 {Message}",
                            xRequestTraceActivity, xRequestTraceProxy,
                            ex.Message + Environment.NewLine + ex.StackTrace
                        );

                        response.StatusCode = StatusCodes.Status500InternalServerError;

                        string errMsg = ex.Message;
                        string errDtl = errMsg + Environment.NewLine + ex.StackTrace;

                        if (IS_USING_REQUEST_LOGGER) {
                            // TODO :: Update Log With Error
                        }

                        bool showErrorDetail = App.Environment.IsDevelopment() || user?.role <= UserSessionRole.USER_SD_SSD_3;
                        await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = "500 - Whoops :: Terjadi Kesalahan",
                            result = new ResponseJsonMessage() {
                                message = showErrorDetail ? errDtl : "Gagal Memproses Data"
                            }
                        });
                    }
                }
            });
        }

        public static void HandleDynamicApiPluginRouteEndpoint(string apiUrlPrefix = "api", string pluginFolderName = "plugins") {
            _ = App.Use(async (context, next) => {
                string path = context.Request.Path.Value?.Trim('/');

                if (!string.IsNullOrEmpty(path) && path.StartsWith(apiUrlPrefix)) {
                    string[] segments = path.Split('/');

                    if (segments.Length >= 2) {
                        string pluginName = segments[1];
                        string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_DATA_FOLDER, pluginFolderName, pluginName, $"{pluginName}.dll");

                        if (File.Exists(pluginPath)) {
                            try {
                                CPluginWatcherCoordinator pwc = context.RequestServices.GetRequiredService<CPluginWatcherCoordinator>();

                                if (!pwc.Context.Manager.IsPluginLoaded(pluginName)) {
                                    pwc.Context.Manager.LoadPlugin(pluginName);
                                    pwc.Context.Manager.ReloadSingleDynamicApiPluginRouteEndpoint(pluginName);
                                }

                                if (!pwc.Context.Manager.IsPluginLoaded(pluginName)) {
                                    throw new Exception($"Tidak Dapat Memuat Plugin '{pluginName}'");
                                }

                                context.RequestServices = pwc.Context.Manager.GetServiceProvider(pluginName, context.RequestServices);

                                await next();
                            }
                            catch (Exception ex) {
                                context.Response.Clear();
                                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                                await context.Response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                                    info = "503 - Whoops :: API Plugin Bermasalah",
                                    result = new ResponseJsonMessage() {
                                        message = ex.Message
                                    }
                                });
                            }

                            return;
                        }

                    }
                }

                await next();
            });
        }

        public static void Handle404ApiNotFound(string apiUrlPrefix = "api") {
            string urlPattern = "/" + apiUrlPrefix + "/{*url:regex(^(?!swagger).*$)}";

            if (!string.IsNullOrEmpty(PLUGINS_PROJECT_NAMESPACE)) {
                urlPattern = "/" + apiUrlPrefix + "/{*url:regex(.*$)}";
            }

            _ = App.MapFallback(urlPattern, async context => {
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

        public static void UseRequestLoggerMiddleware() {
            _ = App.UseMiddleware<RequestLoggerMiddleware>();
            IS_USING_REQUEST_LOGGER = true;
        }

        public static void UseSecretMiddleware() {
            _ = App.UseMiddleware<SecretMiddleware>();
            IS_USING_SECRET = true;
        }

        public static void UseApiKeyMiddleware() {
            _ = App.UseMiddleware<ApiKeyMiddleware>();
            IS_USING_API_KEY = true;
        }

        public static void UseJwtMiddleware() {
            _ = App.UseMiddleware<JwtMiddleware>();
            IS_USING_JWT = true;
        }

    }

}
