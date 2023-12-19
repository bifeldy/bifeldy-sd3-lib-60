/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Kafka Pub-Sub
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Confluent.Kafka;

namespace bifeldy_sd3_lib_60.Services {

    public interface IKafkaService {
        ProducerConfig GenerateProducerConfig(string hostPort);
        IProducer<T1, T2> GenerateProducerBuilder<T1, T2>(ProducerConfig config);
        ConsumerConfig GenerateConsumerConfig(string hostPort, string groupId, AutoOffsetReset autoOffsetReset);
        IConsumer<T1, T2> GenerateConsumerBuilder<T1, T2>(ConsumerConfig config);
    }

    public sealed class CKafkaService : IKafkaService {

        private readonly IPubSubService _pubSub;

        public CKafkaService(IPubSubService pubSub) {
            _pubSub = pubSub;
        }

        public ProducerConfig GenerateProducerConfig(string hostPort) {
            return new ProducerConfig {
                BootstrapServers = hostPort
            };
        }

        public IProducer<T1, T2> GenerateProducerBuilder<T1, T2>(ProducerConfig config) {
            return new ProducerBuilder<T1, T2>(config).Build();
        }

        public ConsumerConfig GenerateConsumerConfig(string hostPort, string groupId, AutoOffsetReset autoOffsetReset) {
            return new ConsumerConfig {
                BootstrapServers = hostPort,
                GroupId = groupId,
                AutoOffsetReset = autoOffsetReset
            };
        }

        public IConsumer<T1, T2> GenerateConsumerBuilder<T1, T2>(ConsumerConfig config) {
            return new ConsumerBuilder<T1, T2>(config).Build();
        }

    }

}
