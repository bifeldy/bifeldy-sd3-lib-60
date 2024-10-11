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

    public sealed class KAFKA_SERVER_T : EntityTableView {
        [Key] public string HOST { get; set; }
        [Key] public decimal? PORT { get; set; }
        [Key] public string TOPIC { get; set; }
        public string GROUP_ID { get; set; }
        public decimal? REPLI { get; set; }
        public decimal? PARTI { get; set; }
    }

}
