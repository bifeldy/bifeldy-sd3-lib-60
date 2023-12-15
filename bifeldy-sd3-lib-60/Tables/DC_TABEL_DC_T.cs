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

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// using Microsoft.EntityFrameworkCore;

using bifeldy_sd3_lib_60.Abstractions;
// using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Tables {

    [Table("dc_tabel_dc_t")]
    public sealed class DC_TABEL_DC_T : EntityTable {

        [Key]
        [Column("tbl_dcid")]
        public decimal TBL_DCID { get; set; }

        [Column("tbl_dc_kode")]
        public string TBL_DC_KODE { get; set; }

        [Column("tbl_dc_nama")]
        public string TBL_DC_NAMA { get; set; }

        [Column("tbl_updrec_id")]
        public string TBL_UPDREC_ID { get; set; }

        [Column("tbl_updrec_date")]
        public DateTime TBL_UPDREC_DATE { get; set; }

        [Column("tbl_dc_mcg")]
        public string TBL_DC_MCG { get; set; }

        [Column("tbl_tag_error_beli")]
        public string TBL_TAG_ERROR_BELI { get; set; }

        [Column("tbl_npwp_dc")]
        public string TBL_NPWP_DC { get; set; }

        [Column("tbl_cabang_kode")]
        public string TBL_CABANG_KODE { get; set; }

        [Column("tbl_cabang_nama")]
        public string TBL_CABANG_NAMA { get; set; }

        [Column("tbl_tgl_buka")]
        public DateTime TBL_TGL_BUKA { get; set; }

        [Column("tbl_singkatan")]
        public string TBL_SINGKATAN { get; set; }

        [Column("tbl_jenis_dc")]
        public string TBL_JENIS_DC { get; set; }

        [Column("tbl_start_sync")]
        public string TBL_START_SYNC { get; set; }

        [Column("tbl_oracle")]
        public string TBL_ORACLE { get; set; }

        [Column("tbl_clipper")]
        public string TBL_CLIPPER { get; set; }

        [Column("tbl_tgl_tutup")]
        public DateTime TBL_TGL_TUTUP { get; set; }

        [Column("tbl_dblink_eis")]
        public string TBL_DBLINK_EIS { get; set; }

        [Column("tbl_dc_induk")]
        public string TBL_DC_INDUK { get; set; }

        [Column("max_pkm_otomatis")]
        public decimal MAX_PKM_OTOMATIS { get; set; }

        [Column("max_pkm_manual")]
        public decimal MAX_PKM_MANUAL { get; set; }

        [Column("tbl_8digit")]
        public DateTime TBL_8DIGIT { get; set; }

        [Column("tbl_tw")]
        public decimal TBL_TW { get; set; }

        [Column("flag_csv")]
        public string FLAG_CSV { get; set; }

        [Column("temp_csv")]
        public string TEMP_CSV { get; set; }

        [Column("po_csv")]
        public string PO_CSV { get; set; }

        [Column("tbl_process")]
        public decimal TBL_PROCESS { get; set; }

        [Column("flag_jawa")]
        public string FLAG_JAWA { get; set; }

        [Column("zona_waktu")]
        public string ZONA_WAKTU { get; set; }
    }

}

// 
// namespace bifeldy_sd3_lib_60.Abstractions {
// 
//     public partial interface IDatabase {
//         DbSet<DC_TABEL_DC_T> DC_TABEL_DC_T { get; set; }
//     }
// 
//     public abstract partial class CDatabase : DbContext, IDatabase {
//         public DbSet<DC_TABEL_DC_T> DC_TABEL_DC_T { get; set; }
//     }
// 
// }
// 
