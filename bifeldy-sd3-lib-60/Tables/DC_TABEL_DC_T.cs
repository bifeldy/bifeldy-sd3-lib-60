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

    public sealed class DC_TABEL_DC_T : EntityTable {
        public decimal? TBL_DCID { get; set; }
        public string TBL_DC_KODE { get; set; }
        public string TBL_DC_NAMA { get; set; }
        public string TBL_UPDREC_ID { get; set; }
        public DateTime? TBL_UPDREC_DATE { get; set; }
        public string TBL_DC_MCG { get; set; }
        public string TBL_TAG_ERROR_BELI { get; set; }
        public string TBL_NPWP_DC { get; set; }
        public string TBL_CABANG_KODE { get; set; }
        public string TBL_CABANG_NAMA { get; set; }
        public DateTime? TBL_TGL_BUKA { get; set; }
        public string TBL_SINGKATAN { get; set; }
        public string TBL_JENIS_DC { get; set; }
        public string TBL_START_SYNC { get; set; }
        public string TBL_ORACLE { get; set; }
        public string TBL_CLIPPER { get; set; }
        public DateTime? TBL_TGL_TUTUP { get; set; }
        public string TBL_DBLINK_EIS { get; set; }
        public string TBL_DC_INDUK { get; set; }
        public decimal? MAX_PKM_OTOMATIS { get; set; }
        public decimal? MAX_PKM_MANUAL { get; set; }
        public DateTime? TBL_8DIGIT { get; set; }
        public decimal? TBL_TW { get; set; }
        public string FLAG_CSV { get; set; }
        // public string TEMP_CSV { get; set; }
        // public string PO_CSV { get; set; }
        // public decimal TBL_PROCESS { get; set; }
        // public string FLAG_JAWA { get; set; }
        // public string ZONA_WAKTU { get; set; }
    }

}
