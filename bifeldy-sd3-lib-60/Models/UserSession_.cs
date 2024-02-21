/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Informasi User Yang Sedang Login
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Text.Json.Serialization;

using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Models {

    public enum UserSessionRole {
        ADMIN = 0,
        MODERATOR,
        USER,
        BOT
    }

    public sealed class UserWebSession {
        public string nik { get; set; }
        public string name { get; set; }
        public UserSessionRole role { get; set; } = UserSessionRole.USER;
        [JsonIgnore] public DC_USER_T dc_user_t { get; set; } = null;
    }

    public sealed class UserApiSession {
        public string name { get; set; }
        public UserSessionRole role { get; set; } = UserSessionRole.BOT;
        [JsonIgnore] public DC_AUTH_T dc_auth_t { get; set; } = null;
    }

}
