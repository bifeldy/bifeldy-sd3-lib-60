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

        private static readonly ClaimsIdentity _anonymousClaimsIdentity = new();
        private static readonly ClaimsPrincipal _anonymousPrincipal = new(_anonymousClaimsIdentity);

        public string SessionKey { get; } = "user-session";

        public CustomAuthenticationStateProvider(
            ILogger<CustomAuthenticationStateProvider> logger,
            ProtectedSessionStorage protectedSessionStorage
        ) {
            this._logger = logger;
            this._protectedSessionStorage = protectedSessionStorage;
        }

        public ClaimsPrincipal GetUserClaimPrincipal(UserWebSession userSession) {
            var userClaim = new List<Claim> {
                new(ClaimTypes.Sid, userSession.nik),
                new(ClaimTypes.Name, userSession.name),
                new(ClaimTypes.Role, userSession.role.ToString())
            };
            var userClaimIdentity = new ClaimsIdentity(userClaim, this.SessionKey);
            return new ClaimsPrincipal(userClaimIdentity);
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
            try {
                ProtectedBrowserStorageResult<UserWebSession> userSessionStorage = await this._protectedSessionStorage.GetAsync<UserWebSession>(this.SessionKey);
                UserWebSession userSession = (userSessionStorage.Success ? userSessionStorage.Value : null) ?? throw new Exception("User Not Login");
                return new AuthenticationState(this.GetUserClaimPrincipal(userSession));
            }
            catch (Exception ex) {
                this._logger.LogError("[CUSTOM_AUTHENTICATION_STATE_PROVIDER_ERROR] 🔓 {ex}", ex.Message);
                return new AuthenticationState(_anonymousPrincipal);
            }
        }

        public async Task UpdateAuthenticationState(UserWebSession userSession) {
            ClaimsPrincipal userClaimPrincipal;
            if (userSession == null) {
                userClaimPrincipal = _anonymousPrincipal;
                await this._protectedSessionStorage.DeleteAsync(this.SessionKey);
            }
            else {
                userClaimPrincipal = this.GetUserClaimPrincipal(userSession);
                await this._protectedSessionStorage.SetAsync(this.SessionKey, userSession);
            }

            var authState = new AuthenticationState(userClaimPrincipal);
            var authStateTask = Task.FromResult(authState);
            this.NotifyAuthenticationStateChanged(authStateTask);
        }

    }

}
