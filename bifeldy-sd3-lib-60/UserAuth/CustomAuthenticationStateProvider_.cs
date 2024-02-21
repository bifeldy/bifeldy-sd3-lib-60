/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Authentication Browser Session Storage
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.UserAuth {

    public sealed class CustomAuthenticationStateProvider : AuthenticationStateProvider {

        private readonly ILogger<CustomAuthenticationStateProvider> _logger;
        private readonly ProtectedSessionStorage _protectedSessionStorage;

        private static readonly ClaimsIdentity _anonymousClaimsIdentity = new ClaimsIdentity();
        private static readonly ClaimsPrincipal _anonymousPrincipal = new ClaimsPrincipal(_anonymousClaimsIdentity);

        public string SessionKey { get; } = "user-session";

        public CustomAuthenticationStateProvider(
            ILogger<CustomAuthenticationStateProvider> logger,
            ProtectedSessionStorage protectedSessionStorage
        ) {
            _logger = logger;
            _protectedSessionStorage = protectedSessionStorage;
        }

        public ClaimsPrincipal GetUserClaimPrincipal(UserWebSession userSession) {
            List<Claim> userClaim = new List<Claim> {
                new Claim(ClaimTypes.Sid, userSession.nik),
                new Claim(ClaimTypes.Name, userSession.name),
                new Claim(ClaimTypes.Role, userSession.role.ToString())
            };
            ClaimsIdentity userClaimIdentity = new ClaimsIdentity(userClaim, SessionKey);
            return new ClaimsPrincipal(userClaimIdentity);
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
            try {
                var userSessionStorage = await _protectedSessionStorage.GetAsync<UserWebSession>(SessionKey);
                var userSession = userSessionStorage.Success ? userSessionStorage.Value : null;
                if (userSession == null) {
                    throw new Exception("User Not Login");
                }

                return new AuthenticationState(GetUserClaimPrincipal(userSession));
            }
            catch (Exception ex) {
                _logger.LogError($"[CUSTOM_AUTHENTICATION_STATE_PROVIDER_ERROR] 🔓 {ex.Message}");
                return new AuthenticationState(_anonymousPrincipal);
            }
        }

        public async Task UpdateAuthenticationState(UserWebSession userSession) {
            ClaimsPrincipal userClaimPrincipal = null;
            if (userSession == null) {
                userClaimPrincipal = _anonymousPrincipal;
                await _protectedSessionStorage.DeleteAsync(SessionKey);
            }
            else {
                userClaimPrincipal = GetUserClaimPrincipal(userSession);
                await _protectedSessionStorage.SetAsync(SessionKey, userSession);
            }

            AuthenticationState authState = new AuthenticationState(userClaimPrincipal);
            Task<AuthenticationState> authStateTask = Task.FromResult(authState);
            NotifyAuthenticationStateChanged(authStateTask);
        }

    }

}
