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
using Microsoft.AspNetCore.Http.Features;
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

using DinkToPdf;
using DinkToPdf.Contracts;

using Grpc.Net.Client.Balancer;

using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

using Quartz;

using Serilog;
using Serilog.Events;

using StackExchange.Redis;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Backgrounds;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Grpcs;
using bifeldy_sd3_lib_60.Libraries;
using bifeldy_sd3_lib_60.Middlewares;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Plugins;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60 {

    public static class Bifeldy {

        public const string DEFAULT_DATA_FOLDER = "_data";

        public static List<string> GRPC_ROUTE_PATH = new();
        public static List<string> SIGNALR_ROUTE_PATH = new();

        public static string PLUGINS_PROJECT_NAMESPACE = null;

        public static DateTime? LAST_GC_RUN = null;

        public static bool IS_USING_SECRET = false;
        public static bool IS_USING_API_KEY = false;
        public static bool IS_USING_JWT = false;

        public static string API_PREFIX = "api";
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
            _ = Services.Configure<FormOptions>(o => {
                o.MultipartBodyLengthLimit = long.MaxValue;
            });
        }

        public static void InitApp(WebApplication app, bool forceGcToCleanUpRamEveryRequest = false, int gcDelaySkipRunMinutes = 30) {
            App = app;

            if (forceGcToCleanUpRamEveryRequest) {
                _ = App.Use(async (context, next) => {
                    context.Response.OnCompleted(() => {
                        if (LAST_GC_RUN != null && (DateTime.Now - LAST_GC_RUN.Value).TotalMinutes < gcDelaySkipRunMinutes) {
                            return Task.CompletedTask;
                        }

                        LAST_GC_RUN = DateTime.Now;

                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        GC.WaitForPendingFinalizers();

                        return Task.CompletedTask;
                    });

                    await next();
                });
            }
        }

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
                options.Limits.MaxRequestBodySize = long.MaxValue;

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
            _ = Services.AddSingleton<SerilogKunciGxxxPropertyEnricher>();
            _ = Builder.Host.UseSerilog((hostContext, services, configuration) => {
                string appPathDir = AppDomain.CurrentDomain.BaseDirectory;
                SerilogKunciGxxxPropertyEnricher spe = services.GetRequiredService<SerilogKunciGxxxPropertyEnricher>();
                _ = configuration.Enrich.With(spe).WriteTo.File(
                    appPathDir + $"/{DEFAULT_DATA_FOLDER}/logs/error_.txt",
                    LogEventLevel.Error,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {KunciGxxx} | {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                );
            });
        }

        public static void UseSerilog() {
            _ = App.UseSerilogRequestLogging(o => {
                o.MessageTemplate = "{TraceId} :: {RemoteOriginIpAddress} :: {RequestMethod} :: {RequestPath} :: {StatusCode} :: {Elapsed:0.0000} ms";
                // o.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Error;
                o.EnrichDiagnosticContext = (diagnosticContext, httpContext) => {
                    diagnosticContext.Set("TraceId", Activity.Current?.Id ?? httpContext?.TraceIdentifier);
                    diagnosticContext.Set("RemoteOriginIpAddress", httpContext?.Items["ip_origin"]);
                };
            });
        }

        /* ** */

        public static void AddDynamicApiPluginRouteEndpoint(string projectNamespace, string dataFolderName = "plugins") {
            PLUGINS_PROJECT_NAMESPACE = projectNamespace;

            string pluginFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_DATA_FOLDER, dataFolderName);
            _ = Directory.CreateDirectory(pluginFolderPath);

            _ = Services.AddSingleton(CDynamicActionDescriptorChangeProvider.Instance);
            _ = Services.AddSingleton<IActionDescriptorChangeProvider>(CDynamicActionDescriptorChangeProvider.Instance);

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
            API_PREFIX = apiUrlPrefix;

            IMvcBuilder mvcBuilder = Services.AddControllers(x => {
                x.UseRoutePrefix(apiUrlPrefix);
                x.UseHideControllerEndPointDc(App.Services);
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
            bool enableJwt = false,
            IDictionary<string, OpenApiInfo> oaDef = null
        ) {
            API_PREFIX = swaggerPrefix;

            _ = Services.AddEndpointsApiExplorer();

            _ = Services.AddSwaggerGen(c => {
                c.EnableAnnotations();

                string swaggerName = Assembly.GetEntryAssembly().GetName().Version.ToString();

                c.SwaggerDoc(swaggerName, new OpenApiInfo() {
                    Title = docsTitle ?? App.Environment.ApplicationName,
                    Description = docsDescription ?? "API Documentation ~"
                });

                if (oaDef != null) {
                    foreach (KeyValuePair<string, OpenApiInfo> oa in oaDef) {
                        c.SwaggerDoc(oa.Key, oa.Value);
                    }
                }

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

                    return new[] { "_UnCategorized!" };
                });

                c.DocInclusionPredicate((name, api) => true);
            });
        }

        public static void UseSwagger(
            string proxyHeaderName = "x-forwarded-prefix",
            IDictionary<string, OpenApiInfo> oaDef = null
        ) {
            NGINX_PATH_NAME = proxyHeaderName;

            _ = App.UseSwagger();

            _ = App.UseSwaggerUI(c => {
                c.RoutePrefix = API_PREFIX;

                string swaggerUrl = $"open-api.json";
                string swaggerName = Assembly.GetEntryAssembly().GetName().Version.ToString();

                c.SwaggerEndpoint(swaggerUrl, swaggerName);

                if (oaDef != null) {
                    foreach (KeyValuePair<string, OpenApiInfo> oa in oaDef) {
                        c.SwaggerEndpoint($"{swaggerUrl}?e={oa.Key}", oa.Value.Title);
                    }
                }

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
            _ = Services.AddDbContext<ISqlite, CSqlite>(ServiceLifetime.Transient);
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
            string appPathDir = AppDomain.CurrentDomain.BaseDirectory;

            string folderPath = Path.Combine(appPathDir, DEFAULT_DATA_FOLDER, "protobuf-net");
            _ = Directory.CreateDirectory(folderPath);

            if (!File.Exists(Path.Combine(folderPath, "bcl.proto"))) {
                File.Copy(
                    Path.Combine(appPathDir, "_assets", "protobuf-net", "bcl.proto"),
                    Path.Combine(folderPath, "bcl.proto")
                );
            }

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

        public static List<string> AutoMapHubService(string signalrPrefixHub = "signalr", Action<HttpConnectionDispatcherOptions> configureOptions = null) {
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

        public static void SetInvariantCulture() {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            _ = Services.Configure<RequestLocalizationOptions>(options => {
                var supportedCultures = new List<CultureInfo>() {
                    CultureInfo.InvariantCulture,
                    // new("en-US"),
                    // new("id-ID")
                };

                options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);

                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.ApplyCurrentCultureToResponseHeaders = true;
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
        public static void AutoCheckMultiDc() {
            string appLocation = AppDomain.CurrentDomain.BaseDirectory;

            AssemblyName prgAsm = Assembly.GetEntryAssembly().GetName();
            AssemblyName libAsm = Assembly.GetExecutingAssembly().GetName();

            string targetDatabaseLocationApp = Path.Combine(appLocation, DEFAULT_DATA_FOLDER, $"{prgAsm.Name}.db");

            if (!File.Exists(targetDatabaseLocationApp)) {
                string defaultDatabaseLocation = Path.Combine(appLocation, $"{prgAsm.Name}.db");

                if (!File.Exists(defaultDatabaseLocation)) {
                    string targetDatabaseLocationLib = Path.Combine(appLocation, DEFAULT_DATA_FOLDER, $"{libAsm.Name}.db");

                    if (!File.Exists(targetDatabaseLocationLib)) {
                        defaultDatabaseLocation = Path.Combine(appLocation, $"{libAsm.Name}.db");

                        if (!File.Exists(defaultDatabaseLocation)) {
                            throw new FileNotFoundException("Default Database Not Found!", defaultDatabaseLocation);
                        }
                    }
                }

                if (!File.Exists(targetDatabaseLocationApp)) {
                    File.Copy(defaultDatabaseLocation, targetDatabaseLocationApp);
                }
            }

            _ = App.UseMiddleware<AutoCheckMultiDcMiddleware>();
        }

        public static void UseNginxProxyPathSegment() {
            _ = App.Use(async (context, next) => {
                if (context.Request.Headers.TryGetValue(NGINX_PATH_NAME, out StringValues pathBase)) {
                    string proxyPath = pathBase.Last();
                    if (context.Request.Path.StartsWithSegments(proxyPath, StringComparison.InvariantCultureIgnoreCase, out PathString path)) {
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

        private static async Task HandleRequestException<T>(HttpContext context, Exception ex, int statusCode, string pluginName = null) {
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

            response.StatusCode = statusCode;

            string errMsg = ex.Message;

            Exception ie = ex.InnerException;
            while (ie != null) {
                errMsg += " ~ " + ie.Message;
                ie = ie.InnerException;
            }

            string errDtl = errMsg + Environment.NewLine + ex.StackTrace;

            bool isPlugin = !string.IsNullOrEmpty(pluginName);

            _logger.LogError(
                "[" + (isPlugin ? "PLUGIN" : "GLOBAL") + "_ERROR_HANDLER] {TraceId} {xRequestTraceProxy} 💣 {Message}",
                xRequestTraceActivity, xRequestTraceProxy, errDtl
            );

            context.Items["error_detail"] = errDtl;

            bool showErrorDetail = App.Environment.IsDevelopment() || user?.role <= UserSessionRole.USER_SD_SSD_3;
            await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                info = $"{statusCode} - Whoops :: {(isPlugin ? $"Plugin `{pluginName}` Bermasalah" : "Terjadi Kesalahan")}",
                result = new ResponseJsonMessage() {
                    message = showErrorDetail || isPlugin ? errDtl : "Gagal Melanjutkan Permintaan"
                }
            });
        }

        public static void Handle500ApiError<T>() {
            _ = App.Use(async (context, next) => {

                // Khusus API Path :: Akan Di Handle Error Dengan Balikan Data JSON
                // Selain Itu Atau Jika Masih Ada Error Lain
                // Misal Di Catch Akan Terlempar Ke Halaman Error Bawaan UI

                if (!context.Request.Path.Value.StartsWith($"/{API_PREFIX}/", StringComparison.InvariantCultureIgnoreCase)) {
                    await next();
                }
                else {
                    try {
                        await next();
                    }
                    catch (Exception ex) {
                        await HandleRequestException<T>(context, ex, StatusCodes.Status500InternalServerError);
                    }
                }
            });
        }

        public static void HandleDynamicApiPluginRouteEndpoint<T>(string pluginFolderName = "plugins") {
            _ = App.Use(async (context, next) => {
                string path = context.Request.Path.Value?.Trim('/');

                if (!string.IsNullOrEmpty(path) && path.StartsWith(API_PREFIX)) {
                    string[] segments = path.Split('/');

                    if (segments.Length >= 2) {
                        string pluginName = segments[1];
                        string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_DATA_FOLDER, pluginFolderName, pluginName, $"{pluginName}.dll");

                        if (File.Exists(pluginPath)) {
                            try {
                                CPluginWatcherCoordinator pwc = context.RequestServices.GetRequiredService<CPluginWatcherCoordinator>();

                                if (!pwc.Context.Manager.IsPluginLoaded(pluginName)) {
                                    pwc.Context.Manager.LoadPlugin(pluginName, true);

                                    await Task.Delay(1500);

                                    context.Response.Clear();
                                    context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                                    await context.Response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                                        info = $"408 - Whoops :: API Plugin `{pluginName}` Loaded",
                                        result = new ResponseJsonMessage() {
                                            message = "Silahkan Ulang Kembali Permintaan Anda"
                                        }
                                    });
                                }
                                else {
                                    IServiceProvider hostServiceProvider = App.Services;
                                    IServiceProvider pluginServiceProvider = pwc.Context.Manager.GetServiceProvider(pluginName);

                                    var pluginServiceScopeFactory = new PluginServiceScopeFactory(
                                        pluginServiceProvider,
                                        hostServiceProvider
                                    );

                                    IServiceScope scopedPluginServiceProvider = pluginServiceScopeFactory.CreateScope();
                                    context.RequestServices = scopedPluginServiceProvider.ServiceProvider;

                                    await next();
                                }
                            }
                            catch (PluginGagalProsesException ex) {
                                await HandleRequestException<T>(context, ex, StatusCodes.Status503ServiceUnavailable, pluginName);
                            }

                            return;
                        }

                    }
                }

                await next();
            });
        }

        public static void Handle404ApiNotFound() {
            string urlPattern = "/" + API_PREFIX + "/{*url:regex(^(?!swagger).*$)}";

            if (!string.IsNullOrEmpty(PLUGINS_PROJECT_NAMESPACE)) {
                urlPattern = "/" + API_PREFIX + "/{*url:regex(.*$)}";
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

        public static void UseRequestVariableInitializerMiddleware() {
            _ = App.UseMiddleware<RequestVariableInitializerMiddleware>();
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
