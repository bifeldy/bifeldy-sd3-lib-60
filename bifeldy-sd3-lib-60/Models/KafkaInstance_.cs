/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Model Kafka di `appsettings.json`
* 
*/

namespace bifeldy_sd3_lib_60.Models {

    public sealed class KafkaInstance {
        public string HOSTPORT { get; set; }
        public string TOPIC { get; set; }
        public string GROUPID { get; set; }
        public bool SUFFIXKODEDC { get; set; }
        public short REPLICATION { get; set; }
        public int PARTITION { get; set; }
    }

}
