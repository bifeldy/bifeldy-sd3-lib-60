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

namespace bifeldy_sd3_lib_60.Tables {

    public sealed class KAFKA_CONSUMER_AUTO_LOG : EntityTable {
        [Key] public string TPC { set; get; }
        [Key] public decimal? OFFS { set; get; }
        public string KEY { set; get; }
        public string VAL { set; get; }
        public DateTime? TMSTAMP { set; get; }
    }

}
