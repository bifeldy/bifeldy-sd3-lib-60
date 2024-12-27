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

using bifeldy_sd3_lib_60.Abstractions;

namespace bifeldy_sd3_lib_60.TableView {

    public sealed class API_DC_T : EntityTableView {
        [Key] public string DC_KODE { set; get; }
        [Key] public string APP_NAME { set; get; }
        public string API_HOST { set; get; }
        public string API_PATH { set; get; }
    }

}
