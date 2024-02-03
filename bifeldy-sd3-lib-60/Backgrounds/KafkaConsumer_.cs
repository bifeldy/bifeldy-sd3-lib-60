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
        private readonly IApplicationService _app;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;

        private readonly IGeneralRepository _generalRepo;

        private readonly string _hostPort;
        private string _groupId;
        private string _topicName;

        private readonly string _logTableName;
        private readonly bool _suffixKodeDc;
        private readonly string _pubSubName;

        private BehaviorSubject<Message<string, dynamic>> observeable = null;

        private IConsumer<string, string> consumer = null;

        private string KAFKA_NAME {
            get {
                return "KAFKA_" + _pubSubName ?? $"CONSUMER_{_hostPort.ToUpper()}#{_topicName.ToUpper()}";
            }
        }

        const ulong COMMIT_AFTER_N_MESSAGES = 10;

        public CKafkaConsumer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, string logTableName,
            string groupId = null, bool suffixKodeDc = false, string pubSubName = null
        ) {
            _logger = serviceProvider.GetRequiredService<ILogger<CKafkaConsumer>>();
            _app = serviceProvider.GetRequiredService<IApplicationService>();
            _converter = serviceProvider.GetRequiredService<IConverterService>();
            _pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            _kafka = serviceProvider.GetRequiredService<IKafkaService>();

            IServiceScope scopedService = serviceProvider.CreateScope();
            _generalRepo = scopedService.ServiceProvider.GetRequiredService<IGeneralRepository>();

            _hostPort = hostPort;
            _topicName = topicName;
            _groupId = !string.IsNullOrEmpty(groupId) ? groupId : _app.AppName;

            _logTableName = logTableName ?? "KAFKA_CONSUMER_AUTO_LOG";
            _suffixKodeDc = suffixKodeDc;
            _pubSubName = pubSubName;
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
                    await _kafka.CreateTopicIfNotExist(_hostPort, _topicName);
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
                    try {
                        await _generalRepo.SaveKafkaToTable(result.Topic, result.Offset.Value, result.Partition.Value, result.Message, _logTableName);
                    }
                    catch (Exception e) {
                        _logger.LogError($"[KAFKA_CONSUMER_SAVEDB] 🏗 {e.Message}");
                    }
                    Message<string, dynamic> msg = new Message<string, dynamic> {
                        Headers = result.Message.Headers,
                        Key = result.Message.Key,
                        Value = result.Message.Value.StartsWith("{") ? _converter.JsonToObject<dynamic>(result.Message.Value) : result.Message.Value,
                        Timestamp = result.Message.Timestamp
                    };
                    observeable.OnNext(msg);
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
