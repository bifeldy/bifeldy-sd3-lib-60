/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: List API Dc Model
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.Models {

    public sealed class ListApiDc {
        public string DC_KODE { get; set; }
        public string FLAG_DBPG { get; set; }
        public string IP_NGINX { get; set; }
        public string USER_NGINX { get; set; }
        public string PASS_NGINX { get; set; }
        public string APP_NAME { get; set; }
        public string API_HOST { get; set; }
        public string API_PATH { get; set; }
        public DateTime? LAST_ONLINE { get; set; }
        public string VERSION { get; set; }
        public decimal? PORT_GRPC { get; set; }
        public string DEFAULT_API_PATH { get; set; }
        public decimal? PING_PONG { get; set; }
    }

}
