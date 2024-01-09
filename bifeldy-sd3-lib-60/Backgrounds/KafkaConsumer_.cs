/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Confluent.Kafka;

using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Backgrounds {

    public sealed class CKafkaConsumer : BackgroundService {

        private readonly ILogger<CKafkaConsumer> _logger;
        private readonly IApplicationService _app;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;

        private readonly string _hostPort;
        private readonly string _groupId;
        private readonly string _topicName;

        private RxBehaviorSubject<KafkaMessage<string, dynamic>> observeable = null;

        private IConsumer<string, string> consumer = null;

        private string KAFKA_NAME {
            get {
                return $"KAFKA_CONSUMER_{_topicName?.ToUpper()}";
            }
        }

        const ulong COMMIT_AFTER_N_MESSAGES = 10;

        public CKafkaConsumer(
            ILogger<CKafkaConsumer> logger, IApplicationService app, IConverterService converter, IPubSubService pubSub, IKafkaService kafka,
            string hostPort, string topicName, string groupId = null
        ) {
            _logger = logger;
            _app = app;
            _converter = converter;
            _pubSub = pubSub;
            _kafka = kafka;
            _hostPort = hostPort;
            _topicName = topicName;
            _groupId = groupId;
        }

        public override void Dispose() {
            consumer?.Dispose();
            _pubSub.DisposeAndRemoveAllSubscriber(KAFKA_NAME);
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await Task.Yield();
            if (observeable == null) {
                observeable = _pubSub.GetGlobalAppBehaviorSubject<KafkaMessage<string, dynamic>>(KAFKA_NAME);
            }
            if (consumer == null) {
                consumer = _kafka.CreateKafkaConsumerInstance<string, string>(_hostPort, _groupId ?? _app.AppName);
                TopicPartition topicPartition = _kafka.CreateKafkaConsumerTopicPartition(_topicName, -1);
                TopicPartitionOffset topicPartitionOffset = _kafka.CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);
                consumer.Assign(topicPartitionOffset);
                consumer.Subscribe(_topicName);
            }
            ulong i = 0;
            while (!stoppingToken.IsCancellationRequested) {
                ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                _logger.LogInformation($"[KAFKA_CONSUMER] 🏗 {result.Message.Key} :: {result.Message.Value}");
                KafkaMessage<string, dynamic> message = new KafkaMessage<string, dynamic> {
                    Headers = result.Message.Headers,
                    Key = result.Message.Key,
                    Timestamp = result.Message.Timestamp,
                    Value = result.Message.Value
                };
                if (result.Message.Value.StartsWith("{")) {
                    message.Value = _converter.JsonToObject<dynamic>(result.Message.Value);
                }
                observeable.OnNext(message);
                if (++i % COMMIT_AFTER_N_MESSAGES == 0) {
                    consumer.Commit();
                    i = 0;
                }
            }
            consumer.Close();
        }

    }

}
