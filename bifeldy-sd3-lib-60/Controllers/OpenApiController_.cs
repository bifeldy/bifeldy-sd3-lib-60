/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Default Ambil Data Swagger Terbaru
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Reflection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Swagger;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Plugins;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Controllers {

    [ApiController]
    [Route("open-api.json")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class OpenApiController : ControllerBase {

        private readonly EnvVar _env;
        private readonly IWebHostEnvironment _environment;
        private readonly ISwaggerProvider _provider;
        private readonly IApplicationService _app;
        private readonly IOraPg _orapg;
        private readonly ILockerService _locker;
        private readonly IGeneralRepository _generalRepo;

        public OpenApiController(
            IOptions<EnvVar> env,
            IWebHostEnvironment environment,
            ISwaggerProvider provider,
            IApplicationService app,
            IOraPg orapg,
            ILockerService locker,
            IGeneralRepository generalRepo
        ) {
            this._env = env.Value;
            this._environment = environment;
            this._provider = provider;
            this._app = app;
            this._orapg = orapg;
            this._locker = locker;
            this._generalRepo = generalRepo;
        }

        private bool IsVisible(Type hideType, string kodeDc, EJenisDc jenisDc) {
            bool IsVisible = true;

            if (
                (hideType == typeof(RouteExcludeDcHoAttribute) && kodeDc == "DCHO") ||
                (hideType == typeof(RouteExcludeKonsolidasiCbnAttribute) && kodeDc == "KCBN") ||
                (hideType == typeof(RouteExcludeWhHoAttribute) && kodeDc == "WHHO") ||
                (hideType == typeof(RouteExcludeAllDcAttribute) && kodeDc != "DCHO" && kodeDc != "KCBN" && kodeDc != "WHHO") ||
                (hideType == typeof(RouteExcludeIndukAttribute) && jenisDc == EJenisDc.INDUK) ||
                (hideType == typeof(RouteExcludeDepoAttribute) && jenisDc == EJenisDc.DEPO) ||
                (hideType == typeof(RouteExcludeKonvinienceAttribute) && jenisDc == EJenisDc.KONVINIENCE) ||
                (hideType == typeof(RouteExcludeIplazaAttribute) && jenisDc == EJenisDc.IPLAZA) ||
                (hideType == typeof(RouteExcludeFrozenAttribute) && jenisDc == EJenisDc.FROZEN) ||
                (hideType == typeof(RouteExcludePerishableAttribute) && jenisDc == EJenisDc.PERISHABLE) ||
                (hideType == typeof(RouteExcludeLpgAttribute) && jenisDc == EJenisDc.LPG) ||
                (hideType == typeof(RouteExcludeSewaAttribute) && jenisDc == EJenisDc.SEWA)
            ) {
                IsVisible = false;
            }

            return IsVisible;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Untuk Ambil Docs Swagger saja")]
        public async Task<IActionResult> SwaggerData() {
            try {
                _ = await this._locker.SemaphoreGlobalApp("SWAGGER").WaitAsync(-1);

                var prgAsm = Assembly.GetEntryAssembly();
                string swaggerDocName = prgAsm.GetName().Version.ToString();
                OpenApiDocument swaggerDoc = this._provider.GetSwagger(swaggerDocName);

                var openApiServers = new List<OpenApiServer>();

                if (!this._app.DebugMode && this.HttpContext.Request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
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

                if (!this._app.DebugMode) {
                    var existingAssemblies = new List<Assembly> {
                        Assembly.GetExecutingAssembly(),
                        Assembly.GetEntryAssembly()
                    };

                    lock (CPluginManager.LOADED_PLUGIN) {
                        existingAssemblies.AddRange(CPluginManager.LOADED_PLUGIN.Values.Select(v => v.Item2));
                    }

                    IEnumerable<Type> controllerTypes = existingAssemblies.Select(asm => asm.GetType())
                        .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

                    string kodeDc = await this._generalRepo.GetKodeDc(this._env.IS_USING_POSTGRES, this._orapg);
                    EJenisDc jenisDc = await this._generalRepo.GetJenisDc(this._env.IS_USING_POSTGRES, this._orapg);

                    List<KeyValuePair<string, string>> excludeApiPath = new();

                    foreach (Type controllerType in controllerTypes) {
                        string controllerRoutePath = "/";

                        RouteAttribute ra = controllerType.GetCustomAttribute<RouteAttribute>();
                        if (ra != null) {
                            controllerRoutePath = $"/{ra.Template}";
                        }

                        IEnumerable<Attribute> attribs = controllerType.GetCustomAttributes()
                            .Where(t => typeof(RouteExcludeCompleteAttribute).IsAssignableFrom(t.GetType()));

                        foreach (Attribute attrib in attribs) {
                            bool isVisible = this.IsVisible(attrib.GetType(), kodeDc, jenisDc);

                            if (!isVisible) {
                                string httpMethod = "ALL";
                                string ctrlPth = "/" + Bifeldy.API_PREFIX + controllerRoutePath.Replace("//", "/");

                                if (!excludeApiPath.Any(p => p.Key.ToLower() == ctrlPth.ToLower() && p.Value.ToLower() == httpMethod.ToLower())) {
                                    excludeApiPath.Add(new(ctrlPth, httpMethod));
                                }
                            }
                        }

                        MethodInfo[] methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        foreach (MethodInfo method in methods) {
                            string actionRoutePath = "/";

                            attribs = method.GetCustomAttributes()
                                .Where(t => typeof(RouteExcludeCompleteAttribute).IsAssignableFrom(t.GetType()));

                            foreach (Attribute attrib in attribs) {
                                bool isVisible = this.IsVisible(attrib.GetType(), kodeDc, jenisDc);

                                if (!isVisible) {
                                    IEnumerable<HttpMethodAttribute> hma = method.GetCustomAttributes()
                                        .Where(t => typeof(HttpMethodAttribute).IsAssignableFrom(t.GetType()))
                                        .Select(t => (HttpMethodAttribute)t);

                                    foreach (HttpMethodAttribute h in hma) {

                                        if (!string.IsNullOrEmpty(h.Template)) {
                                            actionRoutePath = $"/{h.Template}";
                                        }

                                        foreach (string hm in h.HttpMethods) {
                                            string controllerActionRoutePath = controllerRoutePath + actionRoutePath;
                                            string actnPth = "/" + Bifeldy.API_PREFIX + controllerActionRoutePath.Replace("//", "/");

                                            if (!excludeApiPath.Any(p => p.Key.ToLower() == actnPth.ToLower() && p.Value.ToLower() == hm.ToLower())) {
                                                excludeApiPath.Add(new(actnPth, hm));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach (KeyValuePair<string, string> path in excludeApiPath) {
                        if (path.Value.ToUpper() == "ALL") {
                            foreach (KeyValuePair<string, OpenApiPathItem> api in swaggerDoc.Paths) {
                                if (api.Key.StartsWith(path.Key)) {
                                    _ = swaggerDoc.Paths.Remove(api.Key);
                                }
                            }
                        }
                        else if (swaggerDoc.Paths.ContainsKey(path.Key)) {
                            OpenApiPathItem api = swaggerDoc.Paths[path.Key];

                            if (Enum.TryParse(typeof(OperationType), path.Value, true, out object httpMethod)) {
                                if (api.Operations.ContainsKey((OperationType)httpMethod)) {
                                    _ = api.Operations.Remove((OperationType)httpMethod);
                                }
                            }

                            if (api.Operations.Count <= 0) {
                                _ = swaggerDoc.Paths.Remove(path.Key);
                            }
                        }
                    }
                }

                string jsonPath = Path.Combine(this._environment.WebRootPath, $"swagger.{DateTime.UtcNow.Ticks}");
                using (var streamWriter = new StreamWriter(jsonPath)) {
                    var writer = new OpenApiJsonWriter(streamWriter);
                    swaggerDoc.SerializeAsV3(writer);
                }

                string jsonData = await System.IO.File.ReadAllTextAsync(jsonPath);
                if (System.IO.File.Exists(jsonPath)) {
                    System.IO.File.Delete(jsonPath);
                }

                return new ContentResult() {
                    StatusCode = StatusCodes.Status200OK,
                    ContentType = "application/json",
                    Content = jsonData
                };
            }
            finally {
                _ = this._locker.SemaphoreGlobalApp("SWAGGER").Release();
            }
        }

    }

}
