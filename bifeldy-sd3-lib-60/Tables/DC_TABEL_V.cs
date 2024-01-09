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

    public sealed class DC_TABEL_V : EntityTable {
        public string TBL_DC_KODE { get; set; }
        public string TBL_JENIS_DC { get; set; }
        public string TBL_DC_INDUK { get; set; }
        public string IP_DB { get; set; }
        public string DB_USER_NAME { get; set; }
        public string DB_PASSWORD { get; set; }
        public string DB_PORT { get; set; }
        public string DB_SID { get; set; }
        public string DBPG_IP { get; set; }
        public string DBPG_NAME { get; set; }
        public string DBPG_USER { get; set; }
        public string DBPG_PASS { get; set; }
        public string DBPG_PORT { get; set; }
        public string FLAG_DBPG { get; set; }
    }

}
