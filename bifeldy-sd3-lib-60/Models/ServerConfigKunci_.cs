/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Model Untuk Configurasi Server
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.Models {

    public class ServerConfigKunci {
        public string kode_dc { get; set; }
        public string kunci_gxxx { get; set; }
        public string server_target { get; set; }
    }

    public sealed class ServerConfigKunciAddEditDelete : InputJson {
        public string kode_dc { get; set; }
        public string kunci_gxxx { get; set; }
        public string server_target { get; set; }
        public string password { get; set; }
        public string type { get; set; } = "EDIT"; // "ADD", "DELETE"
    }

}
