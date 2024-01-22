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

    public sealed class CKafkaProducer : BackgroundService {

        private readonly ILogger<CKafkaProducer> _logger;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;

        private readonly IGeneralRepository _generalRepo;

        private readonly string _hostPort;
        private string _topicName;

        private readonly bool _suffixKodeDc;

        private IProducer<string, string> producer = null;

        private BehaviorSubject<Message<string, dynamic>> observeable = null;

        private List<Message<string, string>> msgs = new List<Message<string, string>>();

        private IDisposable kafkaSubs = null;

        private string KAFKA_NAME {
            get {
                return $"KAFKA_PRODUCER_{_hostPort.ToUpper()}#{_topicName.ToUpper()}";
            }
        }

        public CKafkaProducer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, bool suffixKodeDc = false
        ) {
            _logger = serviceProvider.GetRequiredService<ILogger<CKafkaProducer>>();
            _converter = serviceProvider.GetRequiredService<IConverterService>();
            _pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            _kafka = serviceProvider.GetRequiredService<IKafkaService>();

            IServiceScope scopedService = serviceProvider.CreateScope();
            _generalRepo = scopedService.ServiceProvider.GetRequiredService<IGeneralRepository>();

            _hostPort = hostPort;
            _topicName = topicName;
            _suffixKodeDc = suffixKodeDc;
        }

        public override void Dispose() {
            producer?.Dispose();
            kafkaSubs?.Dispose();
            _pubSub.DisposeAndRemoveSubscriber(KAFKA_NAME);
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                await Task.Yield();

                if (_suffixKodeDc) {
                    if (!_topicName.EndsWith("_")) {
                        _topicName += "_";
                    }
                    string kodeDc = await _generalRepo.GetKodeDc();
                    _topicName += kodeDc;
                }
                if (producer == null) {
                    producer = _kafka.CreateKafkaProducerInstance<string, string>(_hostPort);
                }
                if (observeable == null) {
                    observeable = _pubSub.GetGlobalAppBehaviorSubject<Message<string, dynamic>>(KAFKA_NAME);
                    kafkaSubs = observeable.Subscribe(async data => {
                        if (data != null) {
                            Message<string, string> msg = new Message<string, string> {
                                Key = data.Key,
                                Value = typeof(string) == data.Value.GetType() ? data.Value : _converter.ObjectToJson(data.Value)
                            };
                            msgs.Add(msg);
                            await producer.ProduceAsync(_topicName, msg, stoppingToken);
                        }
                    });
                }
                while (!stoppingToken.IsCancellationRequested) {
                    foreach (Message<string, string> msg in msgs) {
                        try {
                            await producer.ProduceAsync(_topicName, msg, stoppingToken);
                        }
                        catch (Exception e) {
                            _logger.LogError($"[KAFKA_PRODUCER_MESSAGE] {e.Message}");
                        }
                        msgs.Remove(msg);
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex) {
                _logger.LogError($"[KAFKA_PRODUCER_ERROR] 🏗 {ex.Message}");
            }
        }

    }

}
