/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Sudah Otomatis Terpasang DbSet<T>
 *              :: Keyless Tidak Bisa Pakai Insert, Update, Delete
 * 
 */

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using bifeldy_sd3_lib_60.Abstractions;

namespace bifeldy_sd3_lib_60.Tables {

    public sealed class DC_API_TOKEN_T : EntityTable {
        [Key] public string USER_NAME { set; get; }
        [JsonIgnore] public string PASSWORD { set; get; }
        [Key] public string APP_NAME { set; get; }
        public DateTime? LAST_LOGIN { set; get; }
        [JsonIgnore] public string TOKEN_SEKALI_PAKAI { set; get; }
    }

}
