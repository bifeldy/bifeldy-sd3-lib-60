/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Sudah Otomatis Terpasang DbSet<T>
 * 
 */

using bifeldy_sd3_lib_60.Abstractions;

namespace bifeldy_sd3_lib_60.Tables {

    public sealed class DC_KAFKA_CONSUMER_T : EntityTable {
        public string TOPIC { set; get; }
        public decimal OFFS { set; get; }
        public string KEY { set; get; }
        public string VAL { set; get; }
        public DateTime TMSTAMP { set; get; }
    }

}
