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

    public sealed class DC_APIKEY_T : EntityTable {
        public string KEY { set; get; }
        public string IP_ORIGIN { set; get; }
        public string APP_NAME { set; get; }
    }

}
