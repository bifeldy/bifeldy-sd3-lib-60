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

using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Backgrounds {

    public sealed class CKafkaConsumer : BackgroundService {

        private readonly IServiceScope _scopedService;

        private readonly ILogger<CKafkaConsumer> _logger;
        private readonly IApplicationService _app;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;

        private readonly IGeneralRepository _generalRepo;

        private string _hostPort;
        private string _groupId;
        private string _topicName;

        private readonly string _logTableName;
        private readonly bool _suffixKodeDc;
        private readonly string _pubSubName;
        private readonly List<string> _excludeJenisDc;

        private BehaviorSubject<Message<string, dynamic>> observeable = null;

        private IConsumer<string, string> consumer = null;

        private string KAFKA_NAME => "KAFKA_" + this._pubSubName ?? $"CONSUMER_{this._hostPort.ToUpper()}#{this._topicName.ToUpper()}";

        const ulong COMMIT_AFTER_N_MESSAGES = 1;

        public CKafkaConsumer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, string logTableName = null, string groupId = null,
            bool suffixKodeDc = false, List<string> excludeJenisDc = null, string pubSubName = null
        ) {
            this._logger = serviceProvider.GetRequiredService<ILogger<CKafkaConsumer>>();
            this._app = serviceProvider.GetRequiredService<IApplicationService>();
            this._converter = serviceProvider.GetRequiredService<IConverterService>();
            this._pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            this._kafka = serviceProvider.GetRequiredService<IKafkaService>();

            this._scopedService = serviceProvider.CreateScope();
            this._generalRepo = this._scopedService.ServiceProvider.GetRequiredService<IGeneralRepository>();

            this._hostPort = hostPort;
            this._topicName = topicName;
            this._groupId = !string.IsNullOrEmpty(groupId) ? groupId : this._app.AppName;

            this._logTableName = logTableName ?? "KAFKA_CONSUMER_AUTO_LOG";
            this._suffixKodeDc = suffixKodeDc;
            this._pubSubName = pubSubName;
            this._excludeJenisDc = excludeJenisDc;
        }

        public override void Dispose() {
            this.consumer?.Close();
            this._pubSub?.DisposeAndRemoveSubscriber(this.KAFKA_NAME);
            this._scopedService?.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                if (this._excludeJenisDc != null) {
                    string jenisDc = await _generalRepo.GetJenisDc();
                    if (this._excludeJenisDc.Contains(jenisDc)) {
                        return;
                    }
                }

                if (string.IsNullOrEmpty(this._hostPort)) {
                    KAFKA_SERVER_T kafka = await this._generalRepo.GetKafkaServerInfo(this._topicName);
                    if (kafka == null) {
                        throw new Exception("KAFKA Tidak Tersedia!");
                    }

                    this._hostPort = $"{kafka.HOST}:{kafka.PORT}";
                }

                if (this._suffixKodeDc) {
                    if (!this._groupId.EndsWith("_")) {
                        this._groupId += "_";
                    }

                    if (!this._topicName.EndsWith("_")) {
                        this._topicName += "_";
                    }

                    string kodeDc = await this._generalRepo.GetKodeDc();
                    this._groupId += kodeDc;
                    this._topicName += kodeDc;
                }

                this.observeable ??= this._pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(this.KAFKA_NAME);

                if (this.consumer == null) {
                    await this._kafka.CreateTopicIfNotExist(this._hostPort, this._topicName);
                    this.consumer = this._kafka.CreateKafkaConsumerInstance<string, string>(this._hostPort, this._groupId);
                    TopicPartition topicPartition = this._kafka.CreateKafkaConsumerTopicPartition(this._topicName, -1);
                    TopicPartitionOffset topicPartitionOffset = this._kafka.CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);
                    this.consumer.Assign(topicPartitionOffset);
                    this.consumer.Subscribe(this._topicName);
                }

                ulong i = 0;
                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Yield();

                    try {
                        ConsumeResult<string, string> result = this.consumer.Consume(stoppingToken);
                        _ = await this._generalRepo.SaveKafkaToTable(result.Topic, result.Offset.Value, result.Partition.Value, result.Message, this._logTableName);

                        var msg = new Message<string, dynamic>() {
                            Headers = result.Message.Headers,
                            Key = result.Message.Key,
                            Value = result.Message.Value.StartsWith("{") ? this._converter.JsonToObject<dynamic>(result.Message.Value) : result.Message.Value,
                            Timestamp = result.Message.Timestamp
                        };

                        this.observeable.OnNext(msg);
                        if (++i % COMMIT_AFTER_N_MESSAGES == 0) {
                            _ = this.consumer.Commit();
                            i = 0;
                        }
                    }
                    catch (Exception e) {
                        this._logger.LogError("[KAFKA_CONSUMER_MESSAGE] 🏗 {e}", e.Message);
                    }
                }

                this.consumer.Close();
            }
            catch (Exception ex) {
                this._logger.LogError("[KAFKA_CONSUMER_ERROR] 🏗 {ex}", ex.Message);
            }
        }

    }

}
