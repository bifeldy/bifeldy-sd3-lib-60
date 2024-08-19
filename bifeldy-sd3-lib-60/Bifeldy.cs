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

using Helmet;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

using DinkToPdf;
using DinkToPdf.Contracts;

using Quartz;

using Serilog;
using Serilog.Events;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Backgrounds;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Middlewares;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60 {

    public static class Bifeldy {

        public static readonly string DEFAULT_DATA_FOLDER = "_data";

        public static WebApplicationBuilder Builder = null;
        public static IServiceCollection Services = null;
        public static IConfiguration Config = null;
        public static WebApplication App = null;

        private static readonly Dictionary<string, KeyValuePair<IJobDetail, ITrigger>> jobList = new();

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
        }

        public static void InitApp(WebApplication app) => App = app;

        /* ** */

        public static void SetupSerilog() {
            _ = Builder.Host.UseSerilog((hostContext, services, configuration) => {
                string appPathDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                _ = configuration.WriteTo.File(appPathDir + $"/{DEFAULT_DATA_FOLDER}/logs/error_.txt", restrictedToMinimumLevel: LogEventLevel.Error, rollingInterval: RollingInterval.Day);
            });
        }

        public static void UseSerilog() {
            _ = App.UseSerilogRequestLogging(o => {
                o.MessageTemplate = "{RemoteIpAddress} :: {RequestScheme} :: {RequestHost} :: {RequestMethod} :: {RequestPath} :: {StatusCode} :: {Elapsed:0.0000} ms";
                // o.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Error;
                o.EnrichDiagnosticContext = (diagnosticContext, httpContext) => {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
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
                c.SwaggerDoc(apiUrlPrefix, new OpenApiInfo {
                    Title = docsTitle ?? Assembly.GetEntryAssembly().GetName().Name,
                    Description = docsDescription ?? "API Documentation ~"
                });
                if (enableApiKey) {
                    var apiKey = new OpenApiSecurityScheme {
                        Description = @"API-Key Origin. Example: 'http://.../...?key=000...'",
                        Name = "key",
                        In = ParameterLocation.Query,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "ApiKey",
                        Reference = new OpenApiReference {
                            Id = "api_key",
                            Type = ReferenceType.SecurityScheme
                        }
                    };
                    c.AddSecurityDefinition(apiKey.Reference.Id, apiKey);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                        { apiKey, Array.Empty<string>() }
                    });
                }

                if (enableJwt) {
                    var jwt = new OpenApiSecurityScheme {
                        Description = @"Authorization Header. Example: 'Bearer eyj...'",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        Reference = new OpenApiReference {
                            Id = "jwt",
                            Type = ReferenceType.SecurityScheme
                        }
                    };
                    c.AddSecurityDefinition(jwt.Reference.Id, jwt);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                        { jwt, Array.Empty<string>() }
                    });
                }

                c.OperationFilter<SwaggerMediaTypesOperationFilter>();
                c.SchemaFilter<SwaggerSkipPropertyFilter>();
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
                        openApiServers.Add(new OpenApiServer {
                            Description = "Reverse Proxy Path",
                            Url = proxyPath.StartsWith("/") || proxyPath.StartsWith("http") ? proxyPath : $"/{proxyPath}"
                        });
                    }

                    openApiServers.Add(new OpenApiServer {
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

        public static void LoadConfig() => Services.Configure<EnvVar>(Config.GetSection("ENV"));

        public static void AddDependencyInjection(bool isWebMvcNotBlazor = false) {
            _ = Services.AddHttpContextAccessor();
            // --
            _ = Services.AddDbContext<IOracle, COracle>();
            _ = Services.AddDbContext<IPostgres, CPostgres>();
            _ = Services.AddDbContext<IMsSQL, CMsSQL>();
            // --
            // Setiap Request Cycle 1 Scope 1x New Object 1x Sesion Saja
            // --
            _ = Services.AddScoped<IOraPg>(sp => {
                EnvVar _envVar = sp.GetRequiredService<IOptions<EnvVar>>().Value;
                return _envVar.IS_USING_POSTGRES ? sp.GetRequiredService<IPostgres>() : sp.GetRequiredService<IOracle>();
            });

            if (isWebMvcNotBlazor) {
                throw new NotImplementedException("Untuk Versi WebMVC Masih Belum Tersedia ...");
            }
            else {
                _ = Services.AddScoped<ProtectedSessionStorage>();
                _ = Services.AddScoped<AuthenticationStateProvider, BlazorAuthenticationStateProvider>();
            }

            _ = Services.AddScoped<IGeneralRepository, CGeneralRepository>();
            _ = Services.AddScoped<IApiKeyRepository, CApiKeyRepository>();
            _ = Services.AddScoped<IApiTokenRepository, CApiTokenRepository>();
            _ = Services.AddScoped<IListMailServerRepository, CListMailServerRepository>();
            _ = Services.AddScoped<IUserRepository, CUserRepository>();
            // --
            // Hanya Singleton Yang Bisa Di Inject Di Constructor() { }
            // --
            _ = Services.AddSingleton<IConverter>(sp => {
                return new SynchronizedConverter(new PdfTools());
            });
            _ = Services.AddSingleton<IApplicationService, CApplicationService>();
            _ = Services.AddSingleton<IGlobalService, CGlobalService>();
            _ = Services.AddSingleton<IConverterService, CConverterService>();
            _ = Services.AddSingleton<ICsvService, CCsvService>();
            _ = Services.AddSingleton<IZipService, CZipService>();
            _ = Services.AddSingleton<IHttpService, CHttpService>();
            _ = Services.AddSingleton<IFtpService, CFtpService>();
            _ = Services.AddSingleton<IBerkasService, CBerkasService>();
            _ = Services.AddSingleton<ISftpService, CSftpService>();
            _ = Services.AddSingleton<IStreamService, CStreamService>();
            _ = Services.AddSingleton<IChiperService, CChiperService>();
            _ = Services.AddSingleton<ILockerService, CLockerService>();
            _ = Services.AddSingleton<IPubSubService, CPubSubService>();
            _ = Services.AddSingleton<IKafkaService, CKafkaService>();
            _ = Services.AddSingleton<IRdlcService, CRdlcService>();
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
            _ = App.UseRequestLocalization(new RequestLocalizationOptions {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
        }

        /* ** */

        public static void AddKafkaProducerBackground(string hostPort, string topicName, short replication = 1, int partition = 1, bool suffixKodeDc = false, string excludeJenisDc = null, string pubSubName = null) {
            _ = Services.AddHostedService(sp => {
                List<string> ls = null;
                if (!string.IsNullOrEmpty(excludeJenisDc)) {
                    ls = new List<string>(excludeJenisDc.Split(",").Select(d => d.Trim().ToUpper()));
                }

                return new CKafkaProducer(sp, hostPort, topicName, replication, partition, suffixKodeDc, ls, pubSubName);
            });
        }

        public static void AddKafkaConsumerBackground(string hostPort, string topicName, string logTableName = null, string groupId = null, bool suffixKodeDc = false, string excludeJenisDc = null, string pubSubName = null) {
            _ = Services.AddHostedService(sp => {
                List<string> ls = null;
                if (!string.IsNullOrEmpty(excludeJenisDc)) {
                    ls = new List<string>(excludeJenisDc.Split(",").Select(d => d.Trim().ToUpper()));
                }

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

        [Obsolete("Defaultnya Sudah Pakai Microsoft Dependency Injection")]
        public static void AddJobScheduler() {
            _ = Services.AddQuartz(opt => {
                opt.UseMicrosoftDependencyInjectionJobFactory();
            });
            _ = Services.AddQuartzHostedService(opt => {
                opt.WaitForJobsToComplete = true;
            });
        }

        public static void CreateJobSchedule<T>(string cronString, string jobName = null) {
            if (string.IsNullOrEmpty(jobName)) {
                jobName = typeof(T).Name;
            }

            JobBuilder jobBuilder = JobBuilder.Create(typeof(T)).WithIdentity(jobName);
            IJobDetail jobDetail = jobBuilder.Build();
            TriggerBuilder triggerBuilder = TriggerBuilder.Create().WithIdentity(jobName);
            ITrigger trigger = triggerBuilder.WithCronSchedule(cronString).Build();
            jobList.Add(jobName, new KeyValuePair<IJobDetail, ITrigger>(jobDetail, trigger));
        }

        public static void CreateJobSchedules(string cronString, params Type[] classInheritFromCQuartzJobScheduler) {
            for (int i = 0; i < classInheritFromCQuartzJobScheduler.Length; i++) {
                Type classInherited = classInheritFromCQuartzJobScheduler[i];
                string jobName = $"{classInherited.Name}_{i}";
                JobBuilder jobBuilder = JobBuilder.Create(classInherited).WithIdentity(jobName);
                IJobDetail jobDetail = jobBuilder.Build();
                TriggerBuilder triggerBuilder = TriggerBuilder.Create().WithIdentity(jobName);
                ITrigger trigger = triggerBuilder.WithCronSchedule(cronString).Build();
                jobList.Add(jobName, new KeyValuePair<IJobDetail, ITrigger>(jobDetail, trigger));
            }
        }

        public async static Task<DateTimeOffset[]> StartJobScheduler() {
            ISchedulerFactory schedulerFactory = App.Services.GetRequiredService<ISchedulerFactory>();
            IScheduler scheduler = await schedulerFactory.GetScheduler();
            var allJobs = new List<Task<DateTimeOffset>>();
            foreach (KeyValuePair<string, KeyValuePair<IJobDetail, ITrigger>> jl in jobList) {
                allJobs.Add(scheduler.ScheduleJob(jl.Value.Key, jl.Value.Value));
            }

            return await Task.WhenAll(allJobs);
        }

        /* ** */

        public static async void Handle404ApiNotFound(HttpContext context) {
            HttpResponse response = context.Response;

            response.Clear();
            response.StatusCode = StatusCodes.Status404NotFound;
            await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonError> {
                info = "404 - Whoops :: API Tidak Ditemukan",
                result = new ResponseJsonError {
                    message = $"Silahkan Periksa Kembali Dokumentasi API"
                }
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

        public static void UseHelmet() {
            _ = App.UseHelmet(o => {
                o.UseContentSecurityPolicy = false; // Buat Web Socket (Blazor SignalR, Socket.io, Web RTC)
                o.UseXContentTypeOptions = false; // Boleh Content-Sniff :: .mkv Dibaca .mp4
                o.UseReferrerPolicy = false; // Kalau Pakai Service Worker (Gak Set Origin, Tapi Referrer)
            });
        }

        public static void UseErrorHandlerMiddleware() {
            _ = App.Use(async (context, next) => {

                // Khusus API Path :: Akan Di Handle Error Dengan Balikan Data JSON
                // Selain Itu Atau Jika Masih Ada Error Lain
                // Misal Di Catch Akan Terlempar Ke Halaman Error Bawaan UI

                if (!context.Request.Path.Value.StartsWith("/api/")) {
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
                            await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonError> {
                                info = "500 - Whoops :: Terjadi Kesalahan",
                                result = new ResponseJsonError {
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

        public static void UseApiKeyMiddleware() => App.UseMiddleware<ApiKeyMiddleware>();

        public static void UseJwtMiddleware() => App.UseMiddleware<JwtMiddleware>();

    }

}
