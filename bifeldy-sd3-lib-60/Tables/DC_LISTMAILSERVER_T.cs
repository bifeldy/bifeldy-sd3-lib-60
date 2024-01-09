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

    public sealed class DC_LISTMAILSERVER_T : EntityTable {
        public string MAIL_DCKODE { set; get; }
        public string MAIL_DCNAME { set; get; }
        public string MAIL_IP { set; get; }
        public string MAIL_HOSTNAME { set; get; }
        public string MAIL_PORT { set; get; }
        public string MAIL_USERNAME { set; get; }
        public string MAIL_PASSWORD { set; get; }
        public string MAIL_SENDER { set; get; }
        public DateTime? MAIL_UPDREC_DATE { set; get; }
    }

}
