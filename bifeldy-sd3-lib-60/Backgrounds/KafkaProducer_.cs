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

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using Microsoft.Extensions.DependencyInjection;

namespace bifeldy_sd3_lib_60.Backgrounds {

    public sealed class CKafkaProducer : BackgroundService {

        private readonly IServiceScope _scopedService;

        private ILogger<CKafkaProducer> logger;
        private IConverterService converter;
        private IPubSubService pubSub;
        private IKafkaService kafka;

        private readonly string _hostPort;
        private readonly string _topicName;

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
            IServiceScopeFactory scopedService,
            string hostPort, string topicName
        ) {
            _scopedService = scopedService.CreateScope();
            _hostPort = hostPort;
            _topicName = topicName;
        }

        public override void Dispose() {
            producer?.Dispose();
            kafkaSubs?.Dispose();
            pubSub.DisposeAndRemoveAllSubscriber(KAFKA_NAME);
            _scopedService.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await Task.Yield();

            logger = _scopedService.ServiceProvider.GetRequiredService<ILogger<CKafkaProducer>>();
            converter = _scopedService.ServiceProvider.GetRequiredService<IConverterService>();
            pubSub = _scopedService.ServiceProvider.GetRequiredService<IPubSubService>();
            kafka = _scopedService.ServiceProvider.GetRequiredService<IKafkaService>();

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
                        logger.LogError($"[KAFKA_PRODUCER] {e.Message}");
                    }
                    msgs.Remove(msg);
                }
                Thread.Sleep(1000);
            }
        }

    }

}
