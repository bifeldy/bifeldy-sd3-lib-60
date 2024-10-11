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

namespace bifeldy_sd3_lib_60.TableView {

    public sealed class API_KEY_T : EntityTableView {
        [Key][JsonIgnore] public string KEY { set; get; }
        public string IP_ORIGIN { set; get; }
        [Key] public string APP_NAME { set; get; }
    }

}
