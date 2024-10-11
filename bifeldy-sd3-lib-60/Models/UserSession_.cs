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

using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Models {

    public enum UserSessionRole {
        PROGRAM_SERVICE = 0,
        USER_SD_SSD_3,
        EXTERNAL_BOT
    }

    public abstract class UserSession {
        public string name { get; set; }
        public UserSessionRole role { get; set; }
    }

    public sealed class UserWebSession : UserSession {
        public string nik { get; set; }
        [JsonIgnore] public DC_USER_T dc_user_t { get; set; }
    }

    public sealed class UserApiSession : UserSession {
        // [JsonIgnore] public API_TOKEN_T dc_api_token_t { get; set; }
        // [JsonIgnore] public DC_USER_T dc_user_t { get; set; }
    }

}
