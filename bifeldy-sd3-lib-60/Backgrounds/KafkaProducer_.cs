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

    public sealed class CKafkaProducer : BackgroundService {

        private readonly EnvVar _env;
        private readonly ILogger<CKafkaProducer> _logger;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;
        private readonly ILockerService _locker;

        private string _hostPort;
        private string _topicName;
        private short _replication;
        private int _partition;

        private readonly bool _suffixKodeDc;
        private readonly string _pubSubName;

        private readonly List<EJenisDc> _excludeJenisDc;

        private readonly IServiceScope _scopedService = null;

        private string KAFKA_NAME => "KAFKA_" + this._pubSubName ?? $"PRODUCER_{this._hostPort.ToUpper()}#{this._topicName.ToUpper()}";

        public CKafkaProducer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, short replication = 1, int partition = 1,
            bool suffixKodeDc = false, List<EJenisDc> excludeJenisDc = null, string pubSubName = null
        ) {
            this._env = serviceProvider.GetRequiredService<IOptions<EnvVar>>().Value;
            this._logger = serviceProvider.GetRequiredService<ILogger<CKafkaProducer>>();
            this._converter = serviceProvider.GetRequiredService<IConverterService>();
            this._pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            this._kafka = serviceProvider.GetRequiredService<IKafkaService>();
            this._locker = serviceProvider.GetRequiredService<ILockerService>();

            this._scopedService = serviceProvider.CreateScope();

            this._hostPort = hostPort;
            this._topicName = topicName;
            this._replication = replication;
            this._partition = partition;

            this._suffixKodeDc = suffixKodeDc;
            this._pubSubName = pubSubName;
            this._excludeJenisDc = excludeJenisDc;
        }

        public override void Dispose() {
            this._pubSub?.DisposeAndRemoveSubscriber(this.KAFKA_NAME);
            this._scopedService?.Dispose();
            base.Dispose();
        }

        private async Task DoWorkMultiDc(IServiceProvider sp, CancellationToken stoppingToken) {
            BehaviorSubject<Message<string, dynamic>> observeable = null;
            IProducer<string, string> producer = null;

            IDisposable subs = null;
            List<Message<string, string>> msgs = new();

            try {
                IOraPg orapg = sp.GetRequiredService<IOraPg>();
                IGeneralRepository generalRepo = sp.GetRequiredService<IGeneralRepository>();

                if (this._excludeJenisDc != null) {
                    EJenisDc jenisDc = await generalRepo.GetJenisDc(this._env.IS_USING_POSTGRES, orapg);
                    if (this._excludeJenisDc.Contains(jenisDc)) {
                        return;
                    }
                }

                if (string.IsNullOrEmpty(this._hostPort)) {
                    KAFKA_SERVER_T kafka = await generalRepo.GetKafkaServerInfo(this._env.IS_USING_POSTGRES, orapg, this._topicName);
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

                    string kodeDc = await generalRepo.GetKodeDc(this._env.IS_USING_POSTGRES, orapg);
                    this._topicName += kodeDc;
                }

                await this._kafka.CreateTopicIfNotExist(this._hostPort, this._topicName, this._replication, this._partition);
                producer = this._kafka.CreateKafkaProducerInstance<string, string>(this._hostPort);

                observeable = this._pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(this.KAFKA_NAME);
                subs = observeable.Subscribe(async data => {
                    if (data != null) {
                        var msg = new Message<string, string>() {
                            Key = data.Key,
                            Value = typeof(string) == data.Value.GetType() ? data.Value : this._converter.ObjectToJson(data.Value)
                        };

                        await this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).WaitAsync(stoppingToken);

                        msgs.Add(msg);

                        _ = this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).Release();
                    }
                });

                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Yield();

                    if (msgs.Count > 0) {
                        await this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).WaitAsync(stoppingToken);

                        Message<string, string>[] cpMsgs = msgs.ToArray();
                        msgs.Clear();

                        foreach (Message<string, string> msg in cpMsgs) {
                            try {
                                _ = await producer.ProduceAsync(this._topicName, msg, stoppingToken);
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
            finally {
                subs?.Dispose();
                producer?.Dispose();
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
                this._logger.LogError("[KAFKA_PRODUCER_HOST] 💉 {ex}", ex.Message);
            }
        }

    }

}
