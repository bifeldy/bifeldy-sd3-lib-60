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

using System.Dynamic;

using Microsoft.Extensions.Logging;

using Confluent.Kafka;
using Confluent.Kafka.Admin;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

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
        Task CreateKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null, string pubSubName = null, CancellationToken stoppingToken = default);
        void DisposeAndRemoveKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null, string pubSubName = null);
        (string, string) GetTopicNameConsumerListener(string topicName, string groupId, string suffixKodeDc = null);
        Task CreateKafkaConsumerListener<T>(string hostPort, string topicName, string groupId, string suffixKodeDc = null, Action<Message<string, T>> execLambda = null, string pubSubName = null, CancellationToken stoppingToken = default);
    }

    [SingletonServiceRegistration]
    public sealed class CKafkaService : IKafkaService {

        private readonly ILogger<CKafkaService> _logger;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(600);

        readonly IDictionary<string, dynamic> keyValuePairs = new ExpandoObject();

        public CKafkaService(ILogger<CKafkaService> logger, IConverterService converter, IPubSubService pubSub) {
            this._logger = logger;
            this._converter = converter;
            this._pubSub = pubSub;
        }

        public async Task CreateTopicIfNotExist(string hostPort, string topicName, short replication = 1, int partition = 1) {
            try {
                var adminConfig = new AdminClientConfig() {
                    BootstrapServers = hostPort
                };
                using (IAdminClient adminClient = new AdminClientBuilder(adminConfig).Build()) {
                    Metadata metadata = adminClient.GetMetadata(this.timeout);
                    List<TopicMetadata> topicsMetadata = metadata.Topics;
                    bool isExist = metadata.Topics.Select(a => a.Topic).Contains(topicName);
                    if (!isExist) {
                        await adminClient.CreateTopicsAsync(new List<TopicSpecification>() {
                            new() { Name = topicName, ReplicationFactor = replication, NumPartitions = partition }
                        });
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[KAFKA_TOPIC] 📝 {ex}", ex.Message);
            }
        }

        public ProducerConfig GenerateKafkaProducerConfig(string hostPort) {
            return new ProducerConfig() {
                BootstrapServers = hostPort
            };
        }

        public IProducer<T1, T2> GenerateProducerBuilder<T1, T2>(ProducerConfig config) => new ProducerBuilder<T1, T2>(config).Build();

        public IProducer<T1, T2> CreateKafkaProducerInstance<T1, T2>(string hostPort) => this.GenerateProducerBuilder<T1, T2>(this.GenerateKafkaProducerConfig(hostPort));

        public async Task<List<DeliveryResult<string, string>>> ProduceSingleMultipleMessages(string hostPort, string topicName, List<Message<string, dynamic>> data) {
            await this.CreateTopicIfNotExist(hostPort, topicName);
            using (IProducer<string, string> producer = this.CreateKafkaProducerInstance<string, string>(hostPort)) {
                var results = new List<DeliveryResult<string, string>>();
                foreach (Message<string, dynamic> d in data) {
                    var msg = new Message<string, string>() {
                        Headers = d.Headers,
                        Key = d.Key,
                        Timestamp = d.Timestamp,
                        Value = typeof(string) == d.Value.GetType() ? d.Value : this._converter.ObjectToJson(d.Value)
                    };
                    results.Add(await producer.ProduceAsync(topicName, msg));
                    this._logger.LogInformation("[KAFKA_PRODUCE] 📝 {Key} :: {Value}", msg.Key, msg.Value);
                }

                return results;
            }
        }

        public ConsumerConfig GenerateKafkaConsumerConfig(string hostPort, string groupId, AutoOffsetReset autoOffsetReset) {
            return new ConsumerConfig() {
                BootstrapServers = hostPort,
                GroupId = groupId,
                AutoOffsetReset = autoOffsetReset,
                EnableAutoCommit = false
            };
        }

        public IConsumer<T1, T2> GenerateConsumerBuilder<T1, T2>(ConsumerConfig config) => new ConsumerBuilder<T1, T2>(config).Build();

        public IConsumer<T1, T2> CreateKafkaConsumerInstance<T1, T2>(string hostPort, string groupId) => this.GenerateConsumerBuilder<T1, T2>(this.GenerateKafkaConsumerConfig(hostPort, groupId, AutoOffsetReset.Earliest));

        public TopicPartition CreateKafkaConsumerTopicPartition(string topicName, int partition) => new(topicName, Math.Max(Partition.Any, partition));

        public TopicPartitionOffset CreateKafkaConsumerTopicPartitionOffset(TopicPartition topicPartition, long offset) => new(topicPartition, new Offset(offset));

        public async Task<List<Message<string, T>>> ConsumeSingleMultipleMessages<T>(string hostPort, string groupId, string topicName, int partition = 0, long offset = -1, ulong nMessagesBlock = 1) {
            await this.CreateTopicIfNotExist(hostPort, topicName);
            using (IConsumer<string, string> consumer = this.CreateKafkaConsumerInstance<string, string>(hostPort, groupId)) {
                TopicPartition topicPartition = this.CreateKafkaConsumerTopicPartition(topicName, partition);
                if (offset < 0) {
                    WatermarkOffsets watermarkOffsets = consumer.QueryWatermarkOffsets(topicPartition, this.timeout);
                    offset = watermarkOffsets.High.Value - 1;
                }

                TopicPartitionOffset topicPartitionOffset = this.CreateKafkaConsumerTopicPartitionOffset(topicPartition, offset);
                consumer.Assign(topicPartitionOffset);
                var results = new List<Message<string, T>>();
                for (ulong i = 0; i < nMessagesBlock; i++) {
                    ConsumeResult<string, string> result = consumer.Consume(this.timeout);
                    this._logger.LogInformation("[KAFKA_CONSUME] 📝 {Key} :: {Value}", result.Message.Key, result.Message.Value);
                    var message = new Message<string, T>() {
                        Headers = result.Message.Headers,
                        Key = result.Message.Key,
                        Timestamp = result.Message.Timestamp,
                        Value = typeof(T) == typeof(string) ? (dynamic) result.Message.Value : this._converter.JsonToObject<T>(result.Message.Value)
                    };
                    results.Add(message);
                }

                return results;
            }
        }

        public string GetKeyProducerListener(string hostPort, string topicName, string pubSubName = null) => !string.IsNullOrEmpty(pubSubName) ? pubSubName : $"KAFKA_PRODUCER_{hostPort.ToUpper()}#{topicName.ToUpper()}";

        public string GetTopicNameProducerListener(string topicName, string suffixKodeDc) {
            if (!string.IsNullOrEmpty(suffixKodeDc)) {
                if (!topicName.EndsWith("_")) {
                    topicName += "_";
                }

                topicName += suffixKodeDc;
            }

            return topicName;
        }

        public async Task CreateKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc, string pubSubName = null, CancellationToken stoppingToken = default) {
            topicName = this.GetTopicNameProducerListener(topicName, suffixKodeDc);
            await this.CreateTopicIfNotExist(hostPort, topicName); ;
            string key = this.GetKeyProducerListener(hostPort, topicName, pubSubName);
            if (!this.keyValuePairs.ContainsKey(key)) {
                this.keyValuePairs.Add(key, this.CreateKafkaProducerInstance<string, string>(hostPort));
            }

            _ = this._pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(key).Subscribe(async data => {
                if (data != null) {
                    var msg = new Message<string, string>() {
                        Headers = data.Headers,
                        Key = data.Key,
                        Timestamp = data.Timestamp,
                        Value = typeof(string) == data.Value.GetType() ? data.Value : this._converter.ObjectToJson(data.Value)
                    };
                    _ = await this.keyValuePairs[key].ProduceAsync(topicName, msg, stoppingToken);
                }
            });
        }

        public void DisposeAndRemoveKafkaProducerListener(string hostPort, string topicName, string suffixKodeDc = null, string pubSubName = null) {
            topicName = this.GetTopicNameProducerListener(topicName, suffixKodeDc);
            string key = this.GetKeyProducerListener(hostPort, topicName, pubSubName);

            if (this.keyValuePairs.ContainsKey(key)) {
                this.keyValuePairs[key].Dispose();
                _ = this.keyValuePairs.Remove(key);
            }

            this._pubSub.DisposeAndRemoveSubscriber(key);
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

        public async Task CreateKafkaConsumerListener<T>(string hostPort, string topicName, string groupId, string suffixKodeDc = null, Action<Message<string, T>> execLambda = null, string pubSubName = null, CancellationToken stoppingToken = default) {
            const ulong COMMIT_AFTER_N_MESSAGES = 10; 
            (topicName, groupId) = this.GetTopicNameConsumerListener(topicName, groupId, suffixKodeDc);
            await this.CreateTopicIfNotExist(hostPort, topicName);
            string key = !string.IsNullOrEmpty(pubSubName) ? pubSubName : $"KAFKA_CONSUMER_{hostPort.ToUpper()}#{topicName.ToUpper()}";
            using (IConsumer<string, string> consumer = this.CreateKafkaConsumerInstance<string, string>(hostPort, groupId)) {
                TopicPartition topicPartition = this.CreateKafkaConsumerTopicPartition(topicName, -1);
                TopicPartitionOffset topicPartitionOffset = this.CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);
                consumer.Assign(topicPartitionOffset);
                consumer.Subscribe(topicName);
                ulong i = 0;
                while (!stoppingToken.IsCancellationRequested) {
                    ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                    var message = new Message<string, T>() {
                        Headers = result.Message.Headers,
                        Key = result.Message.Key,
                        Timestamp = result.Message.Timestamp,
                        Value = typeof(T) == typeof(string) ? (dynamic) result.Message.Value : this._converter.JsonToObject<T>(result.Message.Value)
                    };
                    execLambda?.Invoke(message);

                    this._pubSub.GetGlobalAppBehaviorSubject<Message<string, T>>(key).OnNext(message);
                    if (++i % COMMIT_AFTER_N_MESSAGES == 0) {
                        _ = consumer.Commit();
                        i = 0;
                    }
                }

                this._pubSub.DisposeAndRemoveSubscriber(key);
            }
        }

    }

}
