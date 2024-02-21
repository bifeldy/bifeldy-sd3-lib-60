/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Role Decorator Class & Function Di Controller
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Data;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.AttributeFilterDecorator {

    public class RolesDecorator : Attribute, IAuthorizationFilter {

        protected readonly IList<UserSessionRole> _roles = new List<UserSessionRole>() {
            UserSessionRole.USER_SD_SSD_3
        };

        protected UserApiSession user = null;

        public RolesDecorator(params UserSessionRole[] roles) {
            foreach (UserSessionRole role in roles) {
                if (!_roles.Contains(role)) {
                    _roles.Add(role);
                }
            }
            _roles = _roles.OrderBy(r => r).ToList();
        }

        public virtual void OnAuthorization(AuthorizationFilterContext context) {
            user = (UserApiSession) context.HttpContext.Items["user"];

            if (_roles == null || user == null) {
                throw new NotImplementedException();
            }
        }

        public void Failed(AuthorizationFilterContext context) {
            context.Result = new JsonResult(new {
                info = "🙄 401 - API Authorization :: Gagal Authentikasi Pengguna 😪",
                result = new {
                    message = "💩 Silahkan Login Terlebih Dahulu! 🤬"
                }
            }) {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        public void RejectRole(AuthorizationFilterContext context, string message) {
            context.Result = new JsonResult(new {
                info = "😡 403 - API Authorization :: Whoops, Akses Ditolak 😤",
                result = new {
                    message = $"💩 {message} 🤬"
                }
            }) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

    }

    // [MinRole(UserSessionRole.ADMIN)] attribute @ classes / functions controllers
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MinRole : RolesDecorator {

        public MinRole(UserSessionRole role) : base(new UserSessionRole[] { role }) { }

        public override void OnAuthorization(AuthorizationFilterContext context) {
            try {
                base.OnAuthorization(context);
                UserSessionRole minRole = _roles.LastOrDefault();
                if (user.role > minRole) {
                    RejectRole(context, $"Dibutuhkan Setidaknya Minimal :: {minRole}");
                }
            }
            catch {
                Failed(context);
            }
        }

    }

    // [AllowedRoles(..., UserSessionRole.ADMIN, ...)] attribute @ classes / functions controllers
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AllowedRoles : RolesDecorator {

        public AllowedRoles(params UserSessionRole[] roles) : base(roles) { }

        public override void OnAuthorization(AuthorizationFilterContext context) {
            try {
                base.OnAuthorization(context);
                if (!_roles.Contains(user.role)) {
                    string requiredRole = string.Join(" / ", _roles.Select(r => r.ToString()).ToArray());
                    RejectRole(context, $"Khusus :: {requiredRole}");
                }
            }
            catch {
                Failed(context);
            }
        }

    }

}
