﻿/**
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
using Confluent.Kafka.Admin;

namespace bifeldy_sd3_lib_60.Services {

    public interface IKafkaService {
        Task CreateTopicIfNotExist(string hostPort, string topicName, short replication = 1, int partition = 1);
        ProducerConfig GenerateKafkaProducerConfig(string hostPort);
        IProducer<T1, T2> GenerateProducerBuilder<T1, T2>(ProducerConfig config);
        IProducer<T1, T2> CreateKafkaProducerInstance<T1, T2>(string hostPort);
        Task<List<DeliveryResult<string, string>>> ProduceSingleMultipleMessages(string hostPort, string topicName, List<Message<string, dynamic>> data);
        ConsumerConfig GenerateKafkaConsumerConfig(string hostPort, string groupId, AutoOffsetReset autoOffsetReset);
        IConsumer<T1, T2> GenerateConsumerBuilder<T1, T2>(ConsumerConfig config);
        IConsumer<T1, T2> CreateKafkaConsumerInstance<T1, T2>(string hostPort, string groupId);
        TopicPartition CreateKafkaConsumerTopicPartition(string topicName, int partition);
        TopicPartitionOffset CreateKafkaConsumerTopicPartitionOffset(TopicPartition topicPartition, long offset);
        Task<List<Message<string, T>>> ConsumeSingleMultipleMessages<T>(string hostPort, string groupId, string topicName, int partition = 0, long offset = -1, ulong nMessagesBlock = 1);
        string GetKeyProducerListener(string hostPort, string topicName, string pubSubName = null);
        string GetTopicNameProducerListener(string topicName, string suffixKodeDc = null);
        void CreateKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null, CancellationToken stoppingToken = default, string pubSubName = null);
        void DisposeAndRemoveKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null, string pubSubName = null);
        (string, string) GetTopicNameConsumerListener(string topicName, string groupId, string suffixKodeDc = null);
        void CreateKafkaConsumerListener<T>(string hostPort, string topicName, string groupId, string suffixKodeDc = null, CancellationToken stoppingToken = default, Action<Message<string, T>> execLambda = null, string pubSubName = null);
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
                _logger.LogError($"[KAFKA_TOPIC] 📝 {ex.Message}");
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

        public async Task<List<DeliveryResult<string, string>>> ProduceSingleMultipleMessages(string hostPort, string topicName, List<Message<string, dynamic>> data) {
            await CreateTopicIfNotExist(hostPort, topicName);
            using (IProducer<string, string> producer = CreateKafkaProducerInstance<string, string>(hostPort)) {
                List<DeliveryResult<string, string>> results = new List<DeliveryResult<string, string>>();
                foreach (Message<string, dynamic> d in data) {
                    Message<string, string> msg = new Message<string, string> {
                        Headers = d.Headers,
                        Key = d.Key,
                        Timestamp = d.Timestamp,
                        Value = typeof(string) == d.Value.GetType() ? d.Value : _converter.ObjectToJson(d.Value)
                    };
                    results.Add(await producer.ProduceAsync(topicName, msg));
                    _logger.LogInformation($"[KAFKA_PRODUCE] 📝 {msg.Key} :: {msg.Value}");
                }
                return results;
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

        public async Task<List<Message<string, T>>> ConsumeSingleMultipleMessages<T>(string hostPort, string groupId, string topicName, int partition = 0, long offset = -1, ulong nMessagesBlock = 1) {
            await CreateTopicIfNotExist(hostPort, topicName);
            using (IConsumer<string, string> consumer = CreateKafkaConsumerInstance<string, string>(hostPort, groupId)) {
                TopicPartition topicPartition = CreateKafkaConsumerTopicPartition(topicName, partition);
                if (offset < 0) {
                    WatermarkOffsets watermarkOffsets = consumer.QueryWatermarkOffsets(topicPartition, timeout);
                    offset = watermarkOffsets.High.Value - 1;
                }
                TopicPartitionOffset topicPartitionOffset = CreateKafkaConsumerTopicPartitionOffset(topicPartition, offset);
                consumer.Assign(topicPartitionOffset);
                List<Message<string, T>> results = new List<Message<string, T>>();
                for (ulong i = 0; i < nMessagesBlock; i++) {
                    ConsumeResult<string, string> result = consumer.Consume(timeout);
                    _logger.LogInformation($"[KAFKA_CONSUME] 📝 {result.Message.Key} :: {result.Message.Value}");
                    Message<string, T> message = new Message<string, T> {
                        Headers = result.Message.Headers,
                        Key = result.Message.Key,
                        Timestamp = result.Message.Timestamp,
                        Value = typeof(T) == typeof(string) ? (dynamic) result.Message.Value : _converter.JsonToObject<T>(result.Message.Value)
                    };
                    results.Add(message);
                }
                consumer.Close();
                return results;
            }
        }

        public string GetKeyProducerListener(string hostPort, string topicName, string pubSubName = null) {
            return !string.IsNullOrEmpty(pubSubName) ? pubSubName : $"KAFKA_PRODUCER_{hostPort.ToUpper()}#{topicName.ToUpper()}";
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

        public async void CreateKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc, CancellationToken stoppingToken = default, string pubSubName = null) {
            topicName = GetTopicNameProducerListener(topicName, suffixKodeDc);
            await CreateTopicIfNotExist(hostPort, topicName); ;
            string key = GetKeyProducerListener(hostPort, topicName, pubSubName);
            IProducer<string, string> producer = CreateKafkaProducerInstance<string, string>(hostPort);
            _pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(key).Subscribe(async data => {
                if (data != null) {
                    Message<string, string> msg = new Message<string, string> {
                        Headers = data.Headers,
                        Key = data.Key,
                        Timestamp = data.Timestamp,
                        Value = typeof(string) == data.Value.GetType() ? data.Value : _converter.ObjectToJson(data.Value)
                    };
                    await producer.ProduceAsync(topicName, msg, stoppingToken);
                }
            });
        }

        public void DisposeAndRemoveKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null, string pubSubName = null) {
            topicName = GetTopicNameProducerListener(topicName, suffixKodeDc);
            string key = GetKeyProducerListener(hostPort, topicName, pubSubName);
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

        public async void CreateKafkaConsumerListener<T>(string hostPort, string topicName, string groupId, string suffixKodeDc = null, CancellationToken stoppingToken = default, Action<Message<string, T>> execLambda = null, string pubSubName = null) {
            const ulong COMMIT_AFTER_N_MESSAGES = 10; 
            (topicName, groupId) = GetTopicNameConsumerListener(topicName, groupId, suffixKodeDc);
            await CreateTopicIfNotExist(hostPort, topicName);
            string key = !string.IsNullOrEmpty(pubSubName) ? pubSubName : $"KAFKA_CONSUMER_{hostPort.ToUpper()}#{topicName.ToUpper()}";
            IConsumer<string, string> consumer = CreateKafkaConsumerInstance<string, string>(hostPort, groupId);
            TopicPartition topicPartition = CreateKafkaConsumerTopicPartition(topicName, -1);
            TopicPartitionOffset topicPartitionOffset = CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);
            consumer.Assign(topicPartitionOffset);
            consumer.Subscribe(topicName);
            ulong i = 0;
            while (!stoppingToken.IsCancellationRequested) {
                ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                Message<string, T> message = new Message<string, T> {
                    Headers = result.Message.Headers,
                    Key = result.Message.Key,
                    Timestamp = result.Message.Timestamp,
                    Value = typeof(T) == typeof(string) ? (dynamic) result.Message.Value : _converter.JsonToObject<T>(result.Message.Value)
                };
                if (execLambda != null) {
                    execLambda(message);
                }
                _pubSub.GetGlobalAppBehaviorSubject<Message<string, T>>(key).OnNext(message);
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
