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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// using Microsoft.EntityFrameworkCore;

using bifeldy_sd3_lib_60.Abstractions;
// using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Tables {

    [Table("dc_apikey_t")]
    public sealed class DC_APIKEY_T : EntityTable {

        [Key]
        [Column("key")]
        public string KEY { set; get; }

        [Column("ip_origin")]
        public string IP_ORIGIN { set; get; }

        [Column("app_name")]
        public string APP_NAME { set; get; }
    }

}

// 
// namespace bifeldy_sd3_lib_60.Abstractions {
// 
//     public partial interface IDatabase {
//         DbSet<DC_APIKEY_T> DC_APIKEY_T { get; set; }
//     }
// 
//     public abstract partial class CDatabase : DbContext, IDatabase {
//         public DbSet<DC_APIKEY_T> DC_APIKEY_T { get; set; }
//     }
// 
// }
// 
