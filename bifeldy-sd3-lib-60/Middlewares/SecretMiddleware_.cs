﻿/**
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

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;
using bifeldy_sd3_lib_60.Repositories;
using Newtonsoft.Json.Linq;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class SecretMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<SecretMiddleware> _logger;
        private readonly IConverterService _converter;
        private readonly IChiperService _chiper;

        public string SessionKey { get; } = "user-session";

        public SecretMiddleware(
            RequestDelegate next,
            ILogger<SecretMiddleware> logger,
            IConverterService converter,
            IChiperService chiper
        ) {
            this._next = next;
            this._logger = logger;
            this._converter = converter;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context, IApiKeyRepository _apiKeyRepo) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            if (!request.Path.Value.StartsWith("/api/") || request.Path.Value.StartsWith("/api/swagger")) {
                await this._next(context);
                return;
            }

            RequestJson reqBody = null;
            string contentType = request.Headers["content-type"].ToString();
            if (SwaggerMediaTypesOperationFilter.AcceptedContentType.Contains(contentType)) {
                string rbString = await request.GetRequestBodyStringAsync();
                if (!string.IsNullOrEmpty(rbString)) {
                    try {
                        reqBody = this._converter.XmlJsonToObject<RequestJson>(contentType, rbString);
                    }
                    catch (Exception ex) {
                        this._logger.LogError("[JSON_BODY] 🔐 {ex}", ex.Message);
                    }
                }
            }

            string secret = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers["x-secret-key"])) {
                secret = request.Headers["x-secret-key"];
            }
            else if (!string.IsNullOrEmpty(request.Query["secret"])) {
                secret = request.Query["secret"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.secret)) {
                secret = reqBody.secret;
            }

            context.Items["secret"] = secret;
            this._logger.LogInformation("[SECRET_MIDDLEWARE] 🗝 {secret}", secret);

            if (!string.IsNullOrEmpty(secret)) {
                API_KEY_T apiKeyT = await _apiKeyRepo.SecretLogin(secret);
                if (apiKeyT == null) {
                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    await response.WriteAsJsonAsync(new {
                        info = "401 - Secret :: Tidak Dapat Digunakan",
                        result = new {
                            message = "Secret salah / tidak dikenali!"
                        }
                    });
                    return;
                }
                else {
                    var userSession = new UserApiSession {
                        name = context.Connection.RemoteIpAddress.ToString(),
                        role = UserSessionRole.PROGRAM_SERVICE
                    };

                    var userClaim = new List<Claim> {
                        new(ClaimTypes.Name, userSession.name),
                        new(ClaimTypes.Role, userSession.role.ToString())
                    };

                    var userClaimIdentity = new ClaimsIdentity(userClaim, this.SessionKey);
                    context.User = new ClaimsPrincipal(userClaimIdentity);

                    context.Items["user"] = new UserApiSession {
                        name = userClaim.Where(c => c.Type == ClaimTypes.Name).First().Value,
                        role = (UserSessionRole)Enum.Parse(typeof(UserSessionRole), userClaim.Where(c => c.Type == ClaimTypes.Role).First().Value)
                    };
                }
            }

            await this._next(context);
        }

    }

}