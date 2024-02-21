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

    public sealed class DC_AUTH_T : EntityTable {
        [Key] public string USERNAME { set; get; }
        public string PASS { set; get; }
        public string TOKEN { set; get; }
        public string NOTES { set; get; }
    }

}
