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

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Repositories;

namespace bifeldy_sd3_lib_60.Backgrounds {

    public sealed class CKafkaProducer : BackgroundService {

        private readonly IServiceProvider _serviceProvider;

        private ILogger<CKafkaProducer> logger;
        private IConverterService converter;
        private IPubSubService pubSub;
        private IKafkaService kafka;

        private readonly string _hostPort;
        private string _topicName;

        private readonly bool _suffixKodeDc;

        private IProducer<string, string> producer = null;

        private RxBehaviorSubject<KafkaMessage<string, dynamic>> observeable = null;

        private List<Message<string, string>> msgs = new List<Message<string, string>>();

        private IDisposable kafkaSubs = null;

        private string KAFKA_NAME {
            get {
                return $"KAFKA_PRODUCER_{_topicName?.ToUpper()}";
            }
        }

        public CKafkaProducer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, bool suffixKodeDc = false
        ) {
            _serviceProvider = serviceProvider;
            _hostPort = hostPort;
            _topicName = topicName;
            _suffixKodeDc = suffixKodeDc;
        }

        public override void Dispose() {
            producer?.Dispose();
            kafkaSubs?.Dispose();
            pubSub.DisposeAndRemoveAllSubscriber(KAFKA_NAME);
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                await Task.Yield();

                logger = _serviceProvider.GetRequiredService<ILogger<CKafkaProducer>>();
                converter = _serviceProvider.GetRequiredService<IConverterService>();
                pubSub = _serviceProvider.GetRequiredService<IPubSubService>();
                kafka = _serviceProvider.GetRequiredService<IKafkaService>();

                IServiceScope scopedService = _serviceProvider.CreateScope();
                IGeneralRepository generalRepo = scopedService.ServiceProvider.GetRequiredService<IGeneralRepository>();

                if (_suffixKodeDc) {
                    if (!_topicName.EndsWith("_")) {
                        _topicName += "_";
                    }
                    string kodeDc = await generalRepo.GetKodeDc();
                    _topicName += kodeDc;
                }
                if (producer == null) {
                    producer = kafka.CreateKafkaProducerInstance<string, string>(_hostPort);
                }
                if (observeable == null) {
                    observeable = pubSub.GetGlobalAppBehaviorSubject<KafkaMessage<string, dynamic>>(KAFKA_NAME);
                    kafkaSubs = observeable.Subscribe(async data => {
                        if (data != null) {
                            Message<string, string> msg = new Message<string, string> {
                                Key = data.Key,
                                Value = typeof(string) == data.Value.GetType() ? data.Value : converter.ObjectToJson(data.Value)
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
                            logger.LogError($"[KAFKA_PRODUCER_MESSAGE] {e.Message}");
                        }
                        msgs.Remove(msg);
                    }
                    Thread.Sleep(1000);
                }

                scopedService.Dispose();
            }
            catch (Exception ex) {
                logger.LogError($"[KAFKA_PRODUCER_ERROR] 🏗 {ex.Message}");
            }
        }

    }

}
