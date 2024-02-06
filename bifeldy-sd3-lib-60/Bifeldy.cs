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

using System.Reflection;

using Helmet;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

using Quartz;

using Serilog;
using Serilog.Events;

using bifeldy_sd3_lib_60.Backgrounds;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Middlewares;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60
{

    public static class Bifeldy {

        public static WebApplicationBuilder Builder = null;
        public static IServiceCollection Services = null;
        public static IConfiguration Config = null;
        public static WebApplication App = null;

        private static readonly Dictionary<string, KeyValuePair<IJobDetail, ITrigger>> jobList = new();

        /* ** */

        public static void InitBuilder(WebApplicationBuilder builder) {
            Builder = builder;
            Services = builder.Services;
            Config = builder.Configuration;
        }

        public static void InitApp(WebApplication app) {
            App = app;
        }

        /* ** */

        public static void SetupSerilog() {
            Builder.Host.UseSerilog((hostContext, services, configuration) => {
                string appPathDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                configuration.WriteTo.File(appPathDir + $"/logs/error_.txt", restrictedToMinimumLevel: LogEventLevel.Error, rollingInterval: RollingInterval.Day);
            });
        }

        /* ** */

        public static void AddSwagger(
            string apiUrlPrefix = "api",
            string docsTitle = "API Documentation",
            string docsDescription = "// No Description",
            bool enableApiKey = true,
            bool enableJwt = true
        ) {
            Services.AddSwaggerGen(c => {
                c.SwaggerDoc(apiUrlPrefix, new OpenApiInfo {
                    Title = docsTitle,
                    Description = docsDescription
                });
                if (enableApiKey) {
                    OpenApiSecurityScheme apiKey = new OpenApiSecurityScheme {
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
                    OpenApiSecurityScheme jwt = new OpenApiSecurityScheme {
                        Description = @"JWT Information. Example: 'Bearer eyj...'",
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
            });
        }

        public static void UseSwagger(
            string apiUrlPrefix = "api",
            string proxyHeaderName = "X-Forwarded-Prefix"
        ) {
            App.UseSwagger(c => {
                c.RouteTemplate = "{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, request) => {
                    List<OpenApiServer> openApiServers = new List<OpenApiServer>() {
                        new OpenApiServer() {
                            Description = "Direct IP Server",
                            Url = "/"
                        }
                    };
                    string proxyPath = request.Headers[proxyHeaderName];
                    if (!string.IsNullOrEmpty(proxyPath)) {
                        openApiServers.Add(new OpenApiServer() {
                            Description = "Reverse Proxy Path",
                            Url = proxyPath.StartsWith("/") || proxyPath.StartsWith("http") ? proxyPath : $"/{proxyPath}"
                        });
                    }
                    swaggerDoc.Servers = openApiServers;
                });
            });
            App.UseSwaggerUI(c => {
                c.RoutePrefix = apiUrlPrefix;
                c.SwaggerEndpoint("swagger.json", apiUrlPrefix);
            });
        }

        /* ** */

        public static void LoadConfig() {
            Services.Configure<EnvVar>(Config.GetSection("ENV"));
        }

        public static void AddDependencyInjection() {
            Services.AddDbContext<COracle>();
            Services.AddDbContext<CPostgres>();
            Services.AddDbContext<CMsSQL>();
            // --
            // Setiap Request Cycle 1 Scope 1x New Object 1x Sesion Saja
            Services.AddScoped<ProtectedSessionStorage>();
            Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            // --
            Services.AddScoped<IOracle, COracle>();
            Services.AddScoped<IPostgres, CPostgres>();
            Services.AddScoped<IMsSQL, CMsSQL>();
            Services.AddScoped<IOraPg>(sp => {
                EnvVar _envVar = sp.GetRequiredService<IOptions<EnvVar>>().Value;
                return _envVar.IS_USING_POSTGRES ? sp.GetRequiredService<CPostgres>() : sp.GetRequiredService<COracle>();
            });
            // --
            Services.AddScoped<IGeneralRepository, CGeneralRepository>();
            Services.AddScoped<IApiKeyRepository, CApiKeyRepository>();
            Services.AddScoped<IListMailServerRepository, CListMailServerRepository>();
            Services.AddScoped<IUserRepository, CUserRepository>();
            // --
            // Hanya Singleton Yang Bisa Di Inject Di Constructor() { }
            // --
            Services.AddSingleton<IApplicationService, CApplicationService>();
            Services.AddSingleton<IGlobalService, CGlobalService>();
            Services.AddSingleton<IConverterService, CConverterService>();
            Services.AddSingleton<IHttpService, CHttpService>();
            Services.AddSingleton<IFtpService, CFtpService>();
            Services.AddSingleton<IBerkasService, CBerkasService>();
            Services.AddSingleton<ISftpService, CSftpService>();
            Services.AddSingleton<IStreamService, CStreamService>();
            Services.AddSingleton<IChiperService, CChiperService>();
            Services.AddSingleton<ILockerService, CLockerService>();
            Services.AddSingleton<IPubSubService, CPubSubService>();
            Services.AddSingleton<IKafkaService, CKafkaService>();
        }

        /* ** */

        public static void AddKafkaProducerBackground(string hostPort, string topicName, short replication = 1, int partition = 1, bool suffixKodeDc = false, string pubSubName = null) {
            Services.AddHostedService(sp => {
                return new CKafkaProducer(sp, hostPort, topicName, replication, partition, suffixKodeDc, pubSubName);
            });
        }

        public static void AddKafkaConsumerBackground(string hostPort, string topicName, string logTableName, string groupId = null, bool suffixKodeDc = false, string pubSubName = null) {
            Services.AddHostedService(sp => {
                return new CKafkaConsumer(sp, hostPort, topicName, logTableName, groupId, suffixKodeDc, pubSubName);
            });
        }

        public static void AddKafkaAutoProducerConsumerBackground() {
            IDictionary<string, KafkaInstance> kafkaSettings = Config.GetSection("KAFKA").Get<IDictionary<string, KafkaInstance>>();
            foreach (KeyValuePair<string, KafkaInstance> ks in kafkaSettings) {
                if (ks.Key.StartsWith("PRODUCER_")) {
                    AddKafkaProducerBackground(ks.Value.HOST_PORT, ks.Value.TOPIC, ks.Value.REPLICATION, ks.Value.PARTITION, ks.Value.SUFFIX_KODE_DC, ks.Key);
                }
            }
            foreach (KeyValuePair<string, KafkaInstance> ks in kafkaSettings) {
                if (ks.Key.StartsWith("CONSUMER_")) {
                    AddKafkaConsumerBackground(ks.Value.HOST_PORT, ks.Value.TOPIC, ks.Value.LOG_TABLE_NAME, ks.Value.GROUP_ID, ks.Value.SUFFIX_KODE_DC, ks.Key);
                }
            }
        }

        /* ** */

        public static void AddJobScheduler() {
            Services.AddQuartz(opt => {
                opt.UseMicrosoftDependencyInjectionJobFactory();
            });
            Services.AddQuartzHostedService(opt => {
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
            List<Task<DateTimeOffset>> allJobs = new List<Task<DateTimeOffset>>();
            foreach (KeyValuePair<string, KeyValuePair<IJobDetail, ITrigger>> jl in jobList) {
                allJobs.Add(scheduler.ScheduleJob(jl.Value.Key, jl.Value.Value));
            }
            return await Task.WhenAll(allJobs);
        }

        /* ** */

        public static async void Handle404ApiNotFound(HttpContext context) {
            IConverterService _cs = App.Services.GetRequiredService<IConverterService>();
            HttpResponse response = context.Response;

            response.Clear();
            response.StatusCode = 404;
            response.ContentType = "application/json";

            object resBody = new {
                info = "🙄 404 - Whoops :: API Tidak Ditemukan 😪",
                result = new {
                    message = $"💩 Silahkan Periksa Kembali Dokumentasi API 🤬"
                }
            };

            await response.WriteAsync(_cs.ObjectToJson(resBody));
        }

        /* ** */

        public static void UseNginxProxyPathSegment() {
            App.Use(async (context, next) => {
                if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out StringValues pathBase)) {
                    context.Request.PathBase = pathBase.Last();
                    if (context.Request.Path.StartsWithSegments(context.Request.PathBase, out PathString path)) {
                        context.Request.Path = path;
                    }
                }
                await next();
            });
        }

        public static void UseHelmet() {
            App.UseHelmet(o => {
                o.UseContentSecurityPolicy = false; // Buat Web Socket (Blazor SignalR, Socket.io, Web RTC)
                o.UseXContentTypeOptions = false; // Boleh Content-Sniff :: .mkv Dibaca .mp4
                o.UseReferrerPolicy = false; // Kalau Pakai Service Worker (Gak Set Origin, Tapi Referrer)
            });
        }

        public static void UseErrorHandlerMiddleware() {
            App.Use(async (context, next) => {

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
                        IConverterService _cs = App.Services.GetRequiredService<IConverterService>();
                        HttpResponse response = context.Response;

                        response.Clear();
                        response.StatusCode = 500;
                        response.ContentType = "application/json";

                        string errMsg = "Gagal Memproses Data";
                        if (App.Environment.IsDevelopment()) {
                            errMsg = ex.Message;
                        }

                        object resBody = new {
                            info = "🙄 500 - Whoops :: Terjadi Kesalahan 😪",
                            result = new {
                                message = $"💩 {errMsg} 🤬"
                            }
                        };

                        await response.WriteAsync(_cs.ObjectToJson(resBody));
                    }
                }
            });
        }

        public static void UseApiKeyMiddleware() {
            App.UseMiddleware<ApiKeyMiddleware>();
        }

    }

}
