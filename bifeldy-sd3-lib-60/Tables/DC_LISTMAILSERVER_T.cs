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

    [Table("dc_listmailserver_t")]
    public sealed class DC_LISTMAILSERVER_T : EntityTable {

        [Key]
        [Column("mail_dckode")]
        public string MAIL_DCKODE { set; get; }

        [Column("mail_dcname")]
        public string MAIL_DCNAME { set; get; }

        [Column("mail_ip")]
        public string MAIL_IP { set; get; }

        [Column("mail_hostname")]
        public string MAIL_HOSTNAME { set; get; }

        [Column("mail_port")]
        public string MAIL_PORT { set; get; }

        [Column("mail_username")]
        public string MAIL_USERNAME { set; get; }

        [Column("mail_password")]
        public string MAIL_PASSWORD { set; get; }

        [Column("mail_sender")]
        public string MAIL_SENDER { set; get; }

        [Column("mail_updrec_date")]
        public DateTime MAIL_UPDREC_DATE { set; get; }
    }

}

// 
// namespace bifeldy_sd3_lib_60.Abstractions {
// 
//     public partial interface IDatabase {
//         DbSet<DC_LISTMAILSERVER_T> DC_LISTMAILSERVER_T { get; set; }
//     }
// 
//     public abstract partial class CDatabase : DbContext, IDatabase {
//         public DbSet<DC_LISTMAILSERVER_T> DC_LISTMAILSERVER_T { get; set; }
//     }
// 
// }
// 
