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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

using Serilog;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Middlewares;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using System.Reflection;
using Serilog.Events;

namespace bifeldy_sd3_lib_60 {

    public static class Bifeldy {

        public static WebApplicationBuilder Builder = null;
        public static IServiceCollection Services = null;
        public static IConfiguration Config = null;
        public static WebApplication App = null;

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

        public static void SetupDI() {
            Services.AddDbContext<COracle>();
            Services.AddDbContext<CPostgres>();
            Services.AddDbContext<CMsSQL>();
            // --
            // Setiap Request Cycle 1 Scope 1x New Object 1x Sesion Saja
            // --
            Services.AddScoped<IOracle, COracle>();
            Services.AddScoped<IPostgres, CPostgres>();
            Services.AddScoped<IMsSQL, CMsSQL>();
            Services.AddScoped<IOraPg>(p => {
                EnvVar _envVar = p.GetService<IOptions<EnvVar>>().Value;
                return _envVar.IS_USING_POSTGRES ? p.GetService<CPostgres>() : p.GetService<COracle>();
            });
            // --
            Services.AddScoped<IApiKeyRepository, CApiKeyRepository>();
            Services.AddScoped<IListMailServerRepository, CListMailServerRepository>();
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
            Services.AddSingleton<IConfigService, CConfigService>();
        }

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

        public static void UseApiKeyMiddleware() {
            App.UseMiddleware<ApiKeyMiddleware>();
        }

        public static void UseJwtMiddleware() {
            // App.UseMiddleware<JwtMiddleware>();
        }

        public static void UseErrorHandlerMiddleware() {
            // App.UseMiddleware<ErrorHandlerMiddleware>();
        }

    }

}
