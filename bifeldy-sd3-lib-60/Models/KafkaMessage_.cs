/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Template Kafka Message
*              :: Model Supaya Tidak Perlu Install Package Nuget Kafka
* 
*/

using Confluent.Kafka;

namespace bifeldy_sd3_lib_60.Models {

    public sealed class KafkaMessage<T1, T2> : Message<T1, T2> {
        //
    }

}
