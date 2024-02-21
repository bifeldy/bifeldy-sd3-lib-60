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

namespace bifeldy_sd3_lib_60.Tables {

    public sealed class DC_USER_T : EntityTable {
        [Key] public string USER_NAME { get; set; }
        [JsonIgnore] public string USER_PASSWORD { get; set; }
        public string USER_APP_MODUL { get; set; }
        public string USER_PRIVS { get; set; }
        public string USER_GROUP { get; set; }
        public decimal USER_FK_TBL_DCID { get; set; }
        public DateTime? USER_UPDREC_DATE { get; set; }
        public string USER_UPDREC_ID { get; set; }
        public decimal USER_FK_TBL_LOKASIID { get; set; }
        public decimal USER_FK_TBL_GUDANGID { get; set; }
        public decimal USER_FK_TBL_DEPOID { get; set; }
        public string USER_FLAG_HANDHELD { get; set; }
        public string USER_NIK { get; set; }
        public string USER_FLAG_HO { get; set; }
        public DateTime? LAST_PASS_CHANGE { get; set; }
        public decimal PASS_VALID_DAYS { get; set; }
    }

}
