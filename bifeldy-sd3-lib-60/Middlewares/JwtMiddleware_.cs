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
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class JwtMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;
        private readonly IGlobalService _gs;
        private readonly IChiperService _chiper;

        public string SessionKey { get; } = "user-session";

        public JwtMiddleware(
            RequestDelegate next,
            ILogger<JwtMiddleware> logger,
            IGlobalService gs,
            IChiperService chiper
        ) {
            this._next = next;
            this._logger = logger;
            this._gs = gs;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            if (!request.Path.Value.StartsWith("/api/") || request.Path.Value.StartsWith("/api/swagger")) {
                await this._next(context);
                return;
            }

            string token = this._gs.GetTokenData(request, await this._gs.GetRequestBody(request));
            if (token.StartsWith("Bearer ")) {
                token = token[7..];
            }

            context.Items["token"] = token;
            this._logger.LogInformation("[JWT_MIDDLEWARE] 🔐 {token}", token);

            try {
                IEnumerable<Claim> userClaim = this._chiper.DecodeJWT(token);
                var userClaimIdentity = new ClaimsIdentity(userClaim, this.SessionKey);
                context.User = new ClaimsPrincipal(userClaimIdentity);

                context.Items["user"] = new UserApiSession {
                    name = userClaim.Where(c => c.Type == ClaimTypes.Name).First().Value,
                    role = (UserSessionRole) Enum.Parse(typeof(UserSessionRole), userClaim.Where(c => c.Type == ClaimTypes.Role).First().Value)
                };
            }
            catch {
                context.Items["user"] = null;

                if (!string.IsNullOrEmpty(token)) {
                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonError> {
                        info = "401 - JWT :: Tidak Dapat Digunakan",
                        result = new ResponseJsonError {
                            message = "Format Token Salah / Expired!"
                        }
                    });
                    return;
                }
            }

            await this._next(context);
        }

    }

}
