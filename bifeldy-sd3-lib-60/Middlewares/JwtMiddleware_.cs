/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Middleware Jwt Pasang User Ke Context
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;
using Newtonsoft.Json.Linq;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class JwtMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;
        private readonly IConverterService _converter;
        private readonly IChiperService _chiper;

        public JwtMiddleware(
            RequestDelegate next,
            ILogger<JwtMiddleware> logger,
            IConverterService converter,
            IChiperService chiper
        ) {
            _next = next;
            _logger = logger;
            _converter = converter;
            _chiper = chiper;
        }

        public async Task Invoke(HttpContext context, IApiTokenRepository _apiTokenRepo) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            if (!request.Path.Value.StartsWith("/api/") || request.Path.Value.StartsWith("/api/swagger")) {
                await _next(context);
                return;
            }

            RequestJson reqBody = null;
            string accept = request.Headers["accept"].ToString();
            if (accept.Contains("application/xml") || accept.Contains("application/json")) {
                using (StreamReader reader = new StreamReader(request.Body)) {
                    string rbString = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(rbString)) {
                        try {
                            reqBody = _converter.JsonToObject<RequestJson>(rbString);
                        }
                        catch (Exception ex) {
                            _logger.LogError($"[JSON_BODY] 🔐 {ex.Message}");
                        }
                    }
                }
            }

            string token = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers.Authorization)) {
                token = request.Headers.Authorization;
            }
            /* else if (!string.IsNullOrEmpty(request.Headers["x-access-token"])) {
                token = request.Headers["x-access-token"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.token)) {
                token = reqBody.token;
            }
            else if (!string.IsNullOrEmpty(request.Query["token"])) {
                token = request.Query["token"];
            } */

            if (token.StartsWith("Bearer ")) {
                token = token[7..];
            }
            context.Items["token"] = token;
            _logger.LogInformation($"[JWT_MIDDLEWARE] 🔐 {token}");

            try {
                IEnumerable<Claim> claims = _chiper.DecodeJWT(token);

                // API_TOKEN_T dcApiToken = await _apiTokenRepo.GetByUserName(claims.Where(c => c.Type == ClaimTypes.Name).First().Value);
                // if (dcApiToken == null) {
                //     throw new Exception("JWT Tidak Valid!");
                // }

                context.Items["user"] = new UserApiSession() {
                    name = claims.Where(c => c.Type == ClaimTypes.Name).First().Value,
                    role = (UserSessionRole) Enum.Parse(typeof(UserSessionRole), claims.Where(c => c.Type == ClaimTypes.Role).First().Value),
                    // dc_api_token_t = dcApiToken
                };
            }
            catch {
                context.Items["user"] = null;

                if (!string.IsNullOrEmpty(token)) {
                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    await response.WriteAsJsonAsync(new {
                        info = "🙄 401 - JWT :: Tidak Dapat Digunakan 😪",
                        result = new {
                            message = "💩 Format Token Salah / Expired! 🤬"
                        }
                    });
                    return;
                }
            }

            await _next(context);
        }

    }

}
