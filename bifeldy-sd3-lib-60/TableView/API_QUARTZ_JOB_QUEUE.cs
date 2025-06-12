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

    public sealed class API_QUARTZ_JOB_QUEUE : EntityTableView {
        [Key][JsonIgnore] public string APP_NAME { set; get; }
        [Key] public string JOB_NAME { set; get; }
        public string STATUS { set; get; } // Tambahan Doank Ini Di Tabel Database Aslinya Ga Ada ~
        public DateTime? START_AT { set; get; }
        public DateTime? COMPLETED_AT { set; get; }
        public string ERROR_MESSAGE { set; get; }
    }

}
