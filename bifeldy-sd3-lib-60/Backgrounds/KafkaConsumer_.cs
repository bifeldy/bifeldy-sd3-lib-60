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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Confluent.Kafka;

using System.Reactive.Subjects;

using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Repositories;

namespace bifeldy_sd3_lib_60.Backgrounds {

    public sealed class CKafkaConsumer : BackgroundService {

        private readonly ILogger<CKafkaConsumer> _logger;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;

        private readonly IGeneralRepository _generalRepo;

        private readonly string _hostPort;
        private string _groupId;
        private string _topicName;

        private readonly bool _suffixKodeDc;

        private BehaviorSubject<Message<string, dynamic>> observeable = null;

        private IConsumer<string, string> consumer = null;

        private string KAFKA_NAME {
            get {
                return $"KAFKA_CONSUMER_{_hostPort.ToUpper()}#{_topicName.ToUpper()}";
            }
        }

        const ulong COMMIT_AFTER_N_MESSAGES = 10;

        public CKafkaConsumer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, string groupId, bool suffixKodeDc = false
        ) {
            _logger = serviceProvider.GetRequiredService<ILogger<CKafkaConsumer>>();
            _converter = serviceProvider.GetRequiredService<IConverterService>();
            _pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            _kafka = serviceProvider.GetRequiredService<IKafkaService>();

            IServiceScope scopedService = serviceProvider.CreateScope();
            _generalRepo = scopedService.ServiceProvider.GetRequiredService<IGeneralRepository>();

            _hostPort = hostPort;
            _topicName = topicName;
            _groupId = groupId;
            _suffixKodeDc = suffixKodeDc;
        }

        public override void Dispose() {
            consumer?.Dispose();
            _pubSub.DisposeAndRemoveSubscriber(KAFKA_NAME);
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                await Task.Yield();

                if (_suffixKodeDc) {
                    if (!_groupId.EndsWith("_")) {
                        _groupId += "_";
                    }
                    if (!_topicName.EndsWith("_")) {
                        _topicName += "_";
                    }
                    string kodeDc = await _generalRepo.GetKodeDc();
                    _groupId += kodeDc;
                    _topicName += kodeDc;
                }
                if (observeable == null) {
                    observeable = _pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(KAFKA_NAME);
                }
                if (consumer == null) {
                    consumer = _kafka.CreateKafkaConsumerInstance<string, string>(_hostPort, _groupId);
                    TopicPartition topicPartition = _kafka.CreateKafkaConsumerTopicPartition(_topicName, -1);
                    TopicPartitionOffset topicPartitionOffset = _kafka.CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);
                    consumer.Assign(topicPartitionOffset);
                    consumer.Subscribe(_topicName);
                }
                ulong i = 0;
                while (!stoppingToken.IsCancellationRequested) {
                    ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                    _logger.LogInformation($"[KAFKA_CONSUMER_MESSAGE] 🏗 {result.Message.Key} :: {result.Message.Value}");
                    Message<string, dynamic> message = new Message<string, dynamic> {
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
            catch (Exception ex) {
                _logger.LogError($"[KAFKA_CONSUMER_ERROR] 🏗 {ex.Message}");
            }
        }

    }

}
