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
        USER_SD_SSD_3 = 0,
        EXTERNAL_BOT
    }

    public sealed class UserWebSession {
        public string nik { get; set; }
        public string name { get; set; }
        public UserSessionRole role { get; set; } = UserSessionRole.USER_SD_SSD_3;
        [JsonIgnore] public DC_USER_T dc_user_t { get; set; } = null;
    }

    public sealed class UserApiSession {
        public string name { get; set; }
        public UserSessionRole role { get; set; } = UserSessionRole.EXTERNAL_BOT;
        [JsonIgnore] public API_TOKEN_T dc_api_token_t { get; set; } = null;
    }

}
