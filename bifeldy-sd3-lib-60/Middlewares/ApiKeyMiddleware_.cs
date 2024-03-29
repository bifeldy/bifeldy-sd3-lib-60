﻿/**
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

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class ApiKeyMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;
        private readonly IConverterService _cs;

        public ApiKeyMiddleware(
            RequestDelegate next,
            ILogger<ApiKeyMiddleware> logger,
            IApplicationService app,
            IGlobalService gs,
            IConverterService cs
        ) {
            _next = next;
            _logger = logger;
            _app = app;
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
            string ipDomainProxy = request.Headers["x-forwarded-host"];
            if (!string.IsNullOrEmpty(ipDomainProxy) && !_gs.AllowedIpOrigin.Contains(ipDomainProxy)) {
                _gs.AllowedIpOrigin.Add(ipDomainProxy);
            }

            RequestJson reqBody = null;
            string accept = request.Headers["accept"].ToString();
            if (accept.Contains("application/xml") || accept.Contains("application/json")) {
                using (StreamReader reader = new StreamReader(request.Body)) {
                    string rbString = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(rbString)) {
                        try {
                            reqBody = _cs.JsonToObject<RequestJson>(rbString);
                        }
                        catch (Exception ex) {
                            _logger.LogError($"[JSON_BODY] 🌸 {ex.Message}");
                        }
                    }
                }
            }

            string apiKey = string.Empty;
            /* if (!string.IsNullOrEmpty(request.Headers["x-api-key"])) {
                apiKey = request.Headers["x-api-key"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.key)) {
                apiKey = reqBody.key;
            }
            else */ if (!string.IsNullOrEmpty(request.Query["key"])) {
                apiKey = request.Query["key"];
            }
            context.Items["api_key"] = apiKey;

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
            else if (!string.IsNullOrEmpty(request.Headers["x-forwarded-for"])) {
                ipOrigin = request.Headers["x-forwarded-for"];
            }
            ipOrigin = _gs.CleanIpOrigin(ipOrigin);
            context.Items["ip_origin"] = ipOrigin;

            _logger.LogInformation($"[KEY_IP_ORIGIN] 🌸 {apiKey} @ {ipOrigin}");

            // API Key Khusus Bypass ~ Case Sensitive
            if (apiKey == _app.AppName || await _akRepo.CheckKeyOrigin(ipOrigin, apiKey)) {
                await _next(context);
            }
            else {
                response.Clear();
                response.StatusCode = StatusCodes.Status401Unauthorized;
                await response.WriteAsJsonAsync(new {
                    info = "🙄 401 - API Key :: Tidak Dapat Digunakan 😪",
                    result = new {
                        message = "💩 Api Key Salah / Tidak Terdaftar! 🤬",
                        api_key = apiKey,
                        ip_origin = ipOrigin
                    }
                });
            }
        }

    }

}
