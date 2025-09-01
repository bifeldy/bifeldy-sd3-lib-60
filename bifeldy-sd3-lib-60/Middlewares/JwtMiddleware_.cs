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

using Grpc.Core;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class JwtMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;
        private readonly IChiperService _chiper;

        public string SessionKey { get; } = "user-session";

        public JwtMiddleware(
            RequestDelegate next,
            ILogger<JwtMiddleware> logger,
            IChiperService chiper
        ) {
            this._next = next;
            this._logger = logger;
            this._chiper = chiper;
        }

        public async Task Invoke(HttpContext context) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string apiPathRequested = request.Path.Value;
            if (string.IsNullOrEmpty(apiPathRequested)) {
                await this._next(context);
                return;
            }

            string apiPathRequestedForGrpc = apiPathRequested.Split('/').Where(u => !string.IsNullOrEmpty(u)).FirstOrDefault();

            bool isGrpc = Bifeldy.GRPC_ROUTE_PATH.Contains(apiPathRequestedForGrpc);
            bool isSignalr = apiPathRequested.StartsWith(Bifeldy.SIGNALR_PREFIX_HUB, StringComparison.InvariantCultureIgnoreCase);
            bool isApi = apiPathRequested.StartsWith($"/{Bifeldy.API_PREFIX}/", StringComparison.InvariantCultureIgnoreCase);
            bool isSwagger = apiPathRequested.StartsWith($"/{Bifeldy.API_PREFIX}/swagger", StringComparison.InvariantCultureIgnoreCase);

            if ((!isGrpc && !isSignalr && !isApi) || (isSwagger && string.IsNullOrEmpty(Bifeldy.PLUGINS_PROJECT_NAMESPACE))) {
                await this._next(context);
                return;
            }

            string token = context.Items["token"]?.ToString();

            this._logger.LogInformation("[JWT_MIDDLEWARE] 🔐 {token}", token);

            context.Items["user"] = null;

            if (!string.IsNullOrEmpty(token)) {
                try {
                    IEnumerable<Claim> userClaim = this._chiper.DecodeJWT(token);

                    var userClaimIdentity = new ClaimsIdentity(userClaim, this.SessionKey);
                    context.User = new ClaimsPrincipal(userClaimIdentity);

                    Claim _claimName = userClaim.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault();
                    Claim _claimRole = userClaim.Where(c => c.Type == ClaimTypes.Role).FirstOrDefault();
                    if (_claimName == null || _claimRole == null) {
                        throw new Exception("Format Token Salah / Expired!");
                    }

                    var userInfo = new UserApiSession() {
                        name = _claimName.Value,
                        role = (UserSessionRole)Enum.Parse(typeof(UserSessionRole), _claimRole.Value)
                    };

                    context.Items["user"] = userInfo;
                }
                catch {
                    string errMsg = "Format Token Salah / Expired!";

                    if (isGrpc) {
                        throw new RpcException(
                            new Status(
                                StatusCode.PermissionDenied,
                                errMsg
                            )
                        );
                    }

                    response.Clear();
                    response.StatusCode = StatusCodes.Status401Unauthorized;

                    await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = "401 - JWT :: Tidak Dapat Digunakan",
                        result = new ResponseJsonMessage() {
                            message = errMsg
                        }
                    });

                    return;
                }
            }

            await this._next(context);
        }

    }

}
