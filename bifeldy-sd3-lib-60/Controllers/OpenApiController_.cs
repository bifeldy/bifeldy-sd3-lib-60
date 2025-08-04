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
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Swagger;

namespace bifeldy_sd3_lib_60.Controllers {

    [ApiController]
    [Route("open-api.json")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class OpenApiController : ControllerBase {

        private readonly IWebHostEnvironment _environment;
        private readonly ISwaggerProvider _provider;

        public OpenApiController(IWebHostEnvironment environment, ISwaggerProvider provider) {
            this._environment = environment;
            this._provider = provider;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Untuk Ambil Docs Swagger saja")]
        public IActionResult SwaggerData() {
            string appVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            OpenApiDocument swaggerDoc = this._provider.GetSwagger(appVersion);

            var openApiServers = new List<OpenApiServer>();

            if (this.HttpContext.Request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
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

            string jsonPath = Path.Combine(this._environment.WebRootPath, $"swagger.{DateTime.UtcNow.Ticks}");
            using (var streamWriter = new StreamWriter(jsonPath)) {
                var writer = new OpenApiJsonWriter(streamWriter);
                swaggerDoc.SerializeAsV3(writer);
            }

            string jsonData = System.IO.File.ReadAllText(jsonPath);
            if (System.IO.File.Exists(jsonPath)) {
                System.IO.File.Delete(jsonPath);
            }

            return new ContentResult() {
                StatusCode = StatusCodes.Status200OK,
                ContentType = "application/json",
                Content = jsonData
            };
        }

    }

}
