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

using Microsoft.Extensions.Logging;

using Confluent.Kafka;

using bifeldy_sd3_lib_60.Repositories;
using Confluent.Kafka.Admin;

namespace bifeldy_sd3_lib_60.Services {

    public interface IKafkaService {
        Task CreateTopicIfNotExist(string hostPort, string topicName, short replication = 1, int partition = 1);
        ProducerConfig GenerateKafkaProducerConfig(string hostPort);
        IProducer<T1, T2> GenerateProducerBuilder<T1, T2>(ProducerConfig config);
        IProducer<T1, T2> CreateKafkaProducerInstance<T1, T2>(string hostPort);
        Task<DeliveryResult<string, string>> ProduceSingleMessage(string hostPort, string topic, Message<string, dynamic> data);
        ConsumerConfig GenerateKafkaConsumerConfig(string hostPort, string groupId, AutoOffsetReset autoOffsetReset);
        IConsumer<T1, T2> GenerateConsumerBuilder<T1, T2>(ConsumerConfig config);
        IConsumer<T1, T2> CreateKafkaConsumerInstance<T1, T2>(string hostPort, string groupId);
        TopicPartition CreateKafkaConsumerTopicPartition(string topicName, int partition);
        TopicPartitionOffset CreateKafkaConsumerTopicPartitionOffset(TopicPartition topicPartition, long offset);
        Message<string, dynamic> ConsumeSingleMessage<T>(string hostPort, string groupId, string topicName, int partition = 0, long offset = -1);
        string GetKeyProducerListener(string hostPort, string topicName);
        string GetTopicNameProducerListener(string topicName, string suffixKodeDc = null);
        void CreateKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null, CancellationToken stoppingToken = default);
        void DisposeAndRemoveKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null);
        (string, string) GetTopicNameConsumerListener(string topicName, string groupId, string suffixKodeDc = null);
        void CreateKafkaConsumerListener(string hostPort, string topicName, string groupId, string suffixKodeDc = null, CancellationToken stoppingToken = default, Action<Message<string, dynamic>> execLambda = null);
    }

    public sealed class CKafkaService : IKafkaService {

        private readonly ILogger<CKafkaService> _logger;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;

        TimeSpan timeout = TimeSpan.FromSeconds(10);

        public CKafkaService(ILogger<CKafkaService> logger, IConverterService converter, IPubSubService pubSub) {
            _logger = logger;
            _converter = converter;
            _pubSub = pubSub;
        }

        public async Task CreateTopicIfNotExist(string hostPort, string topicName, short replication = 1, int partition = 1) {
            try {
                AdminClientConfig adminConfig = new AdminClientConfig {
                    BootstrapServers = hostPort
                };
                using (IAdminClient adminClient = new AdminClientBuilder(adminConfig).Build()) {
                    Metadata metadata = adminClient.GetMetadata(timeout);
                    List<TopicMetadata> topicsMetadata = metadata.Topics;
                    bool isExist = metadata.Topics.Select(a => a.Topic).Contains(topicName);
                    if (!isExist) {
                        await adminClient.CreateTopicsAsync(new List<TopicSpecification> {
                            new TopicSpecification { Name = topicName, ReplicationFactor = replication, NumPartitions = partition }
                        });
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError($"[KAFKA_CONSUMER_TOPIC] 📝 {ex.Message}");
            }
        }

        public ProducerConfig GenerateKafkaProducerConfig(string hostPort) {
            return new ProducerConfig {
                BootstrapServers = hostPort
            };
        }

        public IProducer<T1, T2> GenerateProducerBuilder<T1, T2>(ProducerConfig config) {
            return new ProducerBuilder<T1, T2>(config).Build();
        }

        public IProducer<T1, T2> CreateKafkaProducerInstance<T1, T2>(string hostPort) {
            return GenerateProducerBuilder<T1, T2>(GenerateKafkaProducerConfig(hostPort));
        }

        public async Task<DeliveryResult<string, string>> ProduceSingleMessage(string hostPort, string topic, Message<string, dynamic> data) {
            using (IProducer<string, string> producer = CreateKafkaProducerInstance<string, string>(hostPort)) {
                if (typeof(string) != data.Value.GetType()) {
                    data.Value = _converter.ObjectToJson(data.Value);
                }
                return await producer.ProduceAsync(topic, (dynamic) data);
            }
        }

        public ConsumerConfig GenerateKafkaConsumerConfig(string hostPort, string groupId, AutoOffsetReset autoOffsetReset) {
            return new ConsumerConfig {
                BootstrapServers = hostPort,
                GroupId = groupId,
                AutoOffsetReset = autoOffsetReset,
                EnableAutoCommit = false
            };
        }

        public IConsumer<T1, T2> GenerateConsumerBuilder<T1, T2>(ConsumerConfig config) {
            return new ConsumerBuilder<T1, T2>(config).Build();
        }

        public IConsumer<T1, T2> CreateKafkaConsumerInstance<T1, T2>(string hostPort, string groupId) {
            return GenerateConsumerBuilder<T1, T2>(GenerateKafkaConsumerConfig(hostPort, groupId, AutoOffsetReset.Earliest));
        }

        public TopicPartition CreateKafkaConsumerTopicPartition(string topicName, int partition) {
            return new TopicPartition(topicName, Math.Max(Partition.Any, partition));
        }

        public TopicPartitionOffset CreateKafkaConsumerTopicPartitionOffset(TopicPartition topicPartition, long offset) {
            return new TopicPartitionOffset(topicPartition, new Offset(offset));
        }

        public Message<string, dynamic> ConsumeSingleMessage<T>(string hostPort, string groupId, string topicName, int partition = 0, long offset = -1) {
            using (IConsumer<string, dynamic> consumer = CreateKafkaConsumerInstance<string, dynamic>(hostPort, groupId)) {
                TopicPartition topicPartition = CreateKafkaConsumerTopicPartition(topicName, partition);
                if (offset < 0) {
                    WatermarkOffsets watermarkOffsets = consumer.QueryWatermarkOffsets(topicPartition, timeout);
                    offset = watermarkOffsets.High.Value - 1;
                }
                TopicPartitionOffset topicPartitionOffset = CreateKafkaConsumerTopicPartitionOffset(topicPartition, offset);
                consumer.Assign(topicPartitionOffset);
                ConsumeResult<string, dynamic> result = consumer.Consume(timeout);
                return result.Message;
            }
        }

        public string GetKeyProducerListener(string hostPort, string topicName) {
            return $"KAFKA_PRODUCER_{hostPort.ToUpper()}#{topicName.ToUpper()}";
        }

        public string GetTopicNameProducerListener(string topicName, string suffixKodeDc) {
            if (!string.IsNullOrEmpty(suffixKodeDc)) {
                if (!topicName.EndsWith("_")) {
                    topicName += "_";
                }
                topicName += suffixKodeDc;
            }
            return topicName;
        }

        public void CreateKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc, CancellationToken stoppingToken = default) {
            topicName = GetTopicNameProducerListener(topicName, suffixKodeDc);
            string key = GetKeyProducerListener(hostPort, topicName);
            IProducer<string, string> producer = CreateKafkaProducerInstance<string, string>(hostPort);
            _pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(key).Subscribe(async data => {
                if (data != null) {
                    if (typeof(string) != data.Value.GetType()) {
                        data.Value = _converter.ObjectToJson(data.Value);
                    }
                    await producer.ProduceAsync(topicName, (dynamic) data, stoppingToken);
                }
            });
        }

        public void DisposeAndRemoveKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null) {
            topicName = GetTopicNameProducerListener(topicName, suffixKodeDc);
            string key = GetKeyProducerListener(hostPort, topicName);
            _pubSub.DisposeAndRemoveSubscriber(key);
        }

        public (string, string) GetTopicNameConsumerListener(string topicName, string groupId, string suffixKodeDc = null) {
            if (!string.IsNullOrEmpty(suffixKodeDc)) {
                if (!groupId.EndsWith("_")) {
                    groupId += "_";
                }
                if (!topicName.EndsWith("_")) {
                    topicName += "_";
                }
                groupId += suffixKodeDc;
                topicName += suffixKodeDc;
            }
            return (topicName, groupId);
        }

        public void CreateKafkaConsumerListener(string hostPort, string topicName, string groupId, string suffixKodeDc = null, CancellationToken stoppingToken = default, Action<Message<string, dynamic>> execLambda = null) {
            const ulong COMMIT_AFTER_N_MESSAGES = 10; 
            (topicName, groupId) = GetTopicNameConsumerListener(topicName, groupId, suffixKodeDc);
            string key = $"KAFKA_CONSUMER_{hostPort.ToUpper()}#{topicName.ToUpper()}";
            IConsumer<string, string> consumer = CreateKafkaConsumerInstance<string, string>(hostPort, groupId);
            TopicPartition topicPartition = CreateKafkaConsumerTopicPartition(topicName, -1);
            TopicPartitionOffset topicPartitionOffset = CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);
            consumer.Assign(topicPartitionOffset);
            consumer.Subscribe(topicName);
            ulong i = 0;
            while (!stoppingToken.IsCancellationRequested) {
                ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                Message<string, string> message = result.Message;
                _pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(key).OnNext((dynamic) message);
                if (++i % COMMIT_AFTER_N_MESSAGES == 0) {
                    consumer.Commit();
                    i = 0;
                }
            }
            consumer.Close();
            _pubSub.DisposeAndRemoveSubscriber(key);
        }

    }

}
