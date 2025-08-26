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
using Microsoft.Extensions.Options;

using Confluent.Kafka;

using System.Reactive.Subjects;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Backgrounds {

    public sealed class CKafkaConsumer : BackgroundService {

        private readonly EnvVar _env;
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

        private readonly List<EJenisDc> _excludeJenisDc;

        private readonly IServiceScope _scopedService = null;

        private string KAFKA_NAME => "KAFKA_" + this._pubSubName ?? $"CONSUMER_{this._hostPort.ToUpper()}#{this._topicName.ToUpper()}";

        private const ulong COMMIT_AFTER_N_MESSAGES = 1;

        public CKafkaConsumer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, string logTableName = null, string groupId = null,
            bool suffixKodeDc = false, List<EJenisDc> excludeJenisDc = null, string pubSubName = null
        ) {
            this._env = serviceProvider.GetRequiredService<IOptions<EnvVar>>().Value;
            this._logger = serviceProvider.GetRequiredService<ILogger<CKafkaConsumer>>();
            this._app = serviceProvider.GetRequiredService<IApplicationService>();
            this._converter = serviceProvider.GetRequiredService<IConverterService>();
            this._pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            this._kafka = serviceProvider.GetRequiredService<IKafkaService>();
            this._generalRepo = serviceProvider.GetRequiredService<IGeneralRepository>();

            this._scopedService = serviceProvider.CreateScope();

            this._hostPort = hostPort;
            this._topicName = topicName;
            this._groupId = !string.IsNullOrEmpty(groupId) ? groupId : this._app.AppName;

            this._logTableName = logTableName ?? "KAFKA_CONSUMER_AUTO_LOG";
            this._suffixKodeDc = suffixKodeDc;
            this._pubSubName = pubSubName;
            this._excludeJenisDc = excludeJenisDc;
        }

        public override void Dispose() {
            this._pubSub.DisposeAndRemoveSubscriber(this.KAFKA_NAME);
            this._scopedService.Dispose();
            base.Dispose();
        }

        private async Task DoWorkMultiDc(IServiceProvider sp, CancellationToken stoppingToken) {
            BehaviorSubject<Message<string, dynamic>> observeable = null;
            IConsumer<string, string> consumer = null;

            try {
                IOraPg orapg = sp.GetRequiredService<IOraPg>();

                if (this._excludeJenisDc != null) {
                    EJenisDc jenisDc = await this._generalRepo.GetJenisDc(this._env.IS_USING_POSTGRES, orapg);
                    if (this._excludeJenisDc.Contains(jenisDc)) {
                        return;
                    }
                }

                if (string.IsNullOrEmpty(this._hostPort)) {
                    KAFKA_SERVER_T kafka = await this._generalRepo.GetKafkaServerInfo(this._env.IS_USING_POSTGRES, orapg, this._topicName);
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

                    string kodeDc = await this._generalRepo.GetKodeDc(this._env.IS_USING_POSTGRES, orapg);
                    this._groupId += kodeDc;
                    this._topicName += kodeDc;
                }

                observeable = this._pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(this.KAFKA_NAME);

                await this._kafka.CreateTopicIfNotExist(this._hostPort, this._topicName);
                consumer = this._kafka.CreateKafkaConsumerInstance<string, string>(this._hostPort, this._groupId);

                TopicPartition topicPartition = this._kafka.CreateKafkaConsumerTopicPartition(this._topicName, -1);
                TopicPartitionOffset topicPartitionOffset = this._kafka.CreateKafkaConsumerTopicPartitionOffset(topicPartition, 0);

                consumer.Assign(topicPartitionOffset);
                consumer.Subscribe(this._topicName);

                ulong i = 0;
                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Yield();

                    try {
                        ConsumeResult<string, string> result = consumer.Consume(stoppingToken);
                        _ = await this._generalRepo.SaveKafkaToTable(this._env.IS_USING_POSTGRES, orapg, result.Topic, result.Offset.Value, result.Partition.Value, result.Message, this._logTableName);

                        var msg = new Message<string, dynamic>() {
                            Headers = result.Message.Headers,
                            Key = result.Message.Key,
                            Value = result.Message.Value.StartsWith("{") ? this._converter.JsonToObject<dynamic>(result.Message.Value) : result.Message.Value,
                            Timestamp = result.Message.Timestamp
                        };

                        observeable.OnNext(msg);
                        if (++i % COMMIT_AFTER_N_MESSAGES == 0) {
                            _ = consumer.Commit();
                            i = 0;
                        }
                    }
                    catch (Exception e) {
                        this._logger.LogError("[KAFKA_CONSUMER_MESSAGE] 🏗 {e}", e.Message);
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[KAFKA_CONSUMER_ERROR] 🏗 {ex}", ex.Message);
            }
            finally {
                consumer?.Dispose();
                observeable?.Dispose();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                var tasks = new List<Task>();

                IServerConfigRepository scr = this._scopedService.ServiceProvider.GetRequiredService<IServerConfigRepository>();

                List<ServerConfigKunci> kunci = await scr.GetKodeServerKunciDc();
                foreach (ServerConfigKunci k in kunci) {
                    Task task = await Task.Factory.StartNew(async () => {
                        using (IServiceScope scope = this._scopedService.ServiceProvider.CreateScope()) {
                            IServiceProvider _sp = scope.ServiceProvider;
                            IServerConfigRepository _scr = _sp.GetRequiredService<IServerConfigRepository>();

                            _ = await _scr.UseKodeServerKunciDc(k.kode_dc, k.kunci_gxxx);

                            await this.DoWorkMultiDc(_sp, stoppingToken);
                        }
                    }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex) {
                this._logger.LogError("[KAFKA_CONSUMER_HOST] 💉 {ex}", ex.Message);
            }
        }

    }

}
