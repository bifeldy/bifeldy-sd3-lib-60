/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Model Kafka di `appsettings.json`
*              :: Tidak Untuk Didaftarkan Ke DI Container
* 
*/

namespace bifeldy_sd3_lib_60.Models {

    public sealed class KafkaInstance {
        public string HOST_PORT { get; set; }
        public string TOPIC { get; set; }
        public string LOG_TABLE_NAME { get; set; }
        public string GROUP_ID { get; set; }
        public bool SUFFIX_KODE_DC { get; set; }
        public short REPLICATION { get; set; }
        public int PARTITION { get; set; }
    }

}
