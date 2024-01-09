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
using bifeldy_sd3_lib_60.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace bifeldy_sd3_lib_60.Backgrounds
{

    public sealed class CKafkaConsumer : BackgroundService {

        private readonly IServiceScope _scopedService;

        private ILogger<CKafkaConsumer> logger;
        private IConverterService converter;
        private IPubSubService pubSub;
        private IKafkaService kafka;

        private IGeneralRepository generalRepo;

        private readonly string _hostPort;
        private readonly string _groupId;
        private string _topicName;

        private readonly bool _suffixKodeDc;

        private RxBehaviorSubject<KafkaMessage<string, dynamic>> observeable = null;

        private IConsumer<string, string> consumer = null;

        private string KAFKA_NAME {
            get {
                return $"KAFKA_CONSUMER_{_topicName?.ToUpper()}";
            }
        }

        const ulong COMMIT_AFTER_N_MESSAGES = 10;

        public CKafkaConsumer(
            IServiceScopeFactory scopedService,
            string hostPort, string topicName, string groupId, bool suffixKodeDc = false
        ) {
            _scopedService = scopedService.CreateScope();
            _hostPort = hostPort;
            _topicName = topicName;
            _groupId = groupId;
            _suffixKodeDc = suffixKodeDc;
        }

        public override void Dispose() {
            consumer?.Dispose();
            pubSub.DisposeAndRemoveAllSubscriber(KAFKA_NAME);
            _scopedService.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                await Task.Yield();

                logger = _scopedService.ServiceProvider.GetRequiredService<ILogger<CKafkaConsumer>>();
                converter = _scopedService.ServiceProvider.GetRequiredService<IConverterService>();
                pubSub = _scopedService.ServiceProvider.GetRequiredService<IPubSubService>();
                kafka = _scopedService.ServiceProvider.GetRequiredService<IKafkaService>();
                generalRepo = _scopedService.ServiceProvider.GetRequiredService<IGeneralRepository>();

                if (_suffixKodeDc) {
                    string kodeDc = await generalRepo.GetKodeDc();
                    _topicName += $"_{kodeDc}";
                }
                if (observeable == null) {
                    observeable = pubSub.GetGlobalAppBehaviorSubject<KafkaMessage<string, dynamic>>(KAFKA_NAME);
                }
                if (consumer == null) {
                    consumer = kafka.CreateKafkaConsumerInstance<string, string>(_hostPort, _groupId);
                    TopicPartition topicPartition = kafka.CreateKafkaConsumerTopicPartition(_topicName, -1);
                    TopicPartitionOffset topicPartitionOffset = kafka.CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);
                    consumer.Assign(topicPartitionOffset);
                    consumer.Subscribe(_topicName);
                }
                ulong i = 0;
                while (!stoppingToken.IsCancellationRequested) {
                    ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                    logger.LogInformation($"[KAFKA_CONSUMER] 🏗 {result.Message.Key} :: {result.Message.Value}");
                    KafkaMessage<string, dynamic> message = new KafkaMessage<string, dynamic> {
                        Headers = result.Message.Headers,
                        Key = result.Message.Key,
                        Timestamp = result.Message.Timestamp,
                        Value = result.Message.Value
                    };
                    if (result.Message.Value.StartsWith("{")) {
                        message.Value = converter.JsonToObject<dynamic>(result.Message.Value);
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
                logger.LogInformation($"[KAFKA_CONSUMER_ERROR] 🏗 {ex.Message}");
            }
        }

    }

}
