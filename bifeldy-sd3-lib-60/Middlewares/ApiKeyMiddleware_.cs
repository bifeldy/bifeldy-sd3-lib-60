/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Middleware API Key
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class ApiKeyMiddleware {

        private readonly RequestDelegate _next;
        private readonly EnvVar _envVar;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IGlobalService _gs;
        private readonly IConverterService _cs;

        public ApiKeyMiddleware(
            RequestDelegate next,
            IOptions<EnvVar> envVar,
            ILogger<ApiKeyMiddleware> logger,
            IGlobalService gs,
            IConverterService cs
        ) {
            _next = next;
            _envVar = envVar.Value;
            _logger = logger;
            _gs = gs;
            _cs = cs;
        }

        public async Task Invoke(HttpContext context, IApiKeyRepository _akRepo) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            if (!request.Path.Value.StartsWith("/api/") || request.Path.Value.StartsWith("/api/swagger")) {
                await _next(context);
                return;
            }

            string ipDomainHost = request.Host.Host;
            if (!_gs.AllowedIpOrigin.Contains(ipDomainHost)) {
                _gs.AllowedIpOrigin.Add(ipDomainHost);
            }
            string ipDomainProxy = request.Headers["X-Forwarded-Host"];
            if (!string.IsNullOrEmpty(ipDomainProxy) && !_gs.AllowedIpOrigin.Contains(ipDomainProxy)) {
                _gs.AllowedIpOrigin.Add(ipDomainProxy);
            }

            StreamReader reader = new StreamReader(request.Body);
            RequestJson reqBody = null;

            string accept = request.Headers["accept"].ToString();
            if (accept.Contains("application/xml") || accept.Contains("application/json")) {
                string rbString = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(rbString)) {
                    try {
                        reqBody = _cs.JsonToObject<RequestJson>(rbString);
                    }
                    catch (Exception ex) {
                        _logger.LogError($"[API_KEY_BODY] 🌸 {ex.Message}");
                    }
                }
            }

            string apiKey = string.Empty;
            if (!string.IsNullOrEmpty(request.Cookies[_envVar.API_KEY_NAME])) {
                apiKey = request.Cookies[_envVar.API_KEY_NAME];
            }
            else if (!string.IsNullOrEmpty(request.Headers["x-api-key"])) {
                apiKey = request.Headers["x-api-key"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.key)) {
                apiKey = reqBody.key;
            }
            else if (!string.IsNullOrEmpty(request.Query["key"])) {
                apiKey = request.Query["key"];
            }

            string ipOrigin = connection.RemoteIpAddress.ToString();
            if (!string.IsNullOrEmpty(request.Headers["origin"])) {
                ipOrigin = request.Headers["origin"];
            }
            else if (!string.IsNullOrEmpty(request.Headers["referer"])) {
                ipOrigin = request.Headers["referer"];
            }
            else if (!string.IsNullOrEmpty(request.Headers["cf-connecting-ip"])) {
                ipOrigin = request.Headers["cf-connecting-ip"];
            }
            else if (!string.IsNullOrEmpty(request.Headers["X-Forwarded-For"])) {
                ipOrigin = request.Headers["X-Forwarded-For"];
            }
            ipOrigin = _gs.CleanIpOrigin(ipOrigin);

            context.Items["api_key"] = apiKey;
            _logger.LogInformation($"[API_KEY_IP_ORIGIN] 🌸 {apiKey} @ {ipOrigin}");

            if (await _akRepo.CheckKeyOrigin(ipOrigin, apiKey)) {
                await _next(context);
            }
            else {
                response.Clear();
                response.StatusCode = 401;
                response.ContentType = "application/json";

                object resBody = new {
                    info = "🙄 401 - API Key :: Tidak Dapat Digunakan 😪",
                    result = new {
                        message = "💩 Api Key Salah / Tidak Terdaftar! 🤬",
                        api_key = apiKey,
                        ip_origin = ipOrigin
                    }
                };

                await response.WriteAsync(_cs.ObjectToJson(resBody));
            }
        }

    }

}
