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

    public sealed class CKafkaProducer : BackgroundService {

        private readonly IServiceScope _scopedService;

        private readonly ILogger<CKafkaProducer> _logger;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;
        private readonly ILockerService _locker;

        private readonly IGeneralRepository _generalRepo;

        private string _hostPort;
        private string _topicName;
        private short _replication;
        private int _partition;

        private readonly bool _suffixKodeDc;
        private readonly string _pubSubName;
        private readonly List<string> _excludeJenisDc;

        private IProducer<string, string> producer = null;

        private BehaviorSubject<Message<string, dynamic>> observeable = null;

        private readonly List<Message<string, string>> msgs = new();

        private IDisposable kafkaSubs = null;

        private string KAFKA_NAME => "KAFKA_" + this._pubSubName ?? $"PRODUCER_{this._hostPort.ToUpper()}#{this._topicName.ToUpper()}";

        public CKafkaProducer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, short replication = 1, int partition = 1,
            bool suffixKodeDc = false, List<string> excludeJenisDc = null, string pubSubName = null
        ) {
            this._logger = serviceProvider.GetRequiredService<ILogger<CKafkaProducer>>();
            this._converter = serviceProvider.GetRequiredService<IConverterService>();
            this._pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            this._kafka = serviceProvider.GetRequiredService<IKafkaService>();
            this._locker = serviceProvider.GetRequiredService<ILockerService>();

            this._scopedService = serviceProvider.CreateScope();
            this._generalRepo = this._scopedService.ServiceProvider.GetRequiredService<IGeneralRepository>();

            this._hostPort = hostPort;
            this._topicName = topicName;
            this._replication = replication;
            this._partition = partition;

            this._suffixKodeDc = suffixKodeDc;
            this._pubSubName = pubSubName;
            this._excludeJenisDc = excludeJenisDc;
        }

        public override void Dispose() {
            this.producer?.Dispose();
            this.kafkaSubs?.Dispose();
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
                    this._replication = (short) kafka.REPLI;
                    this._partition = (int) kafka.PARTI;
                }

                if (this._suffixKodeDc) {
                    if (!this._topicName.EndsWith("_")) {
                        this._topicName += "_";
                    }

                    string kodeDc = await this._generalRepo.GetKodeDc();
                    this._topicName += kodeDc;
                }

                if (this.producer == null) {
                    await this._kafka.CreateTopicIfNotExist(this._hostPort, this._topicName, this._replication, this._partition);
                    this.producer = this._kafka.CreateKafkaProducerInstance<string, string>(this._hostPort);
                }

                if (this.observeable == null) {
                    this.observeable = this._pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(this.KAFKA_NAME);
                    this.kafkaSubs = this.observeable.Subscribe(async data => {
                        if (data != null) {
                            var msg = new Message<string, string>() {
                                Key = data.Key,
                                Value = typeof(string) == data.Value.GetType() ? data.Value : this._converter.ObjectToJson(data.Value)
                            };
                            await this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).WaitAsync();
                            this.msgs.Add(msg);
                            _ = this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).Release();
                        }
                    });
                }

                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Yield();

                    if (this.msgs.Count > 0) {
                        await this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).WaitAsync(stoppingToken);

                        Message<string, string>[] cpMsgs = this.msgs.ToArray();
                        this.msgs.Clear();

                        foreach (Message<string, string> msg in cpMsgs) {
                            try {
                                _ = await this.producer.ProduceAsync(this._topicName, msg, stoppingToken);
                            }
                            catch (Exception e) {
                                this._logger.LogError("[KAFKA_PRODUCER_MESSAGE] {e}", e.Message);
                            }
                        }

                        _ = this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).Release();
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[KAFKA_PRODUCER_ERROR] 🏗 {ex}", ex.Message);
            }
        }

    }

}
