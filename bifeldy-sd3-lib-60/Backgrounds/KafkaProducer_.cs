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

using System.Reactive.Subjects;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Confluent.Kafka;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Backgrounds {

    public sealed class CKafkaProducer : BackgroundService {

        private readonly ILogger<CKafkaProducer> _logger;
        private readonly IConverterService _converter;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;

        private readonly string _hostPort;
        private readonly string _topicName;

        private IProducer<string, string> producer = null;

        private BehaviorSubject<KafkaMessage<string, dynamic>> observeable = null;

        private List<Message<string, string>> msgs = new List<Message<string, string>>();

        private IDisposable kafkaSubs = null;

        private string KAFKA_NAME {
            get {
                return $"KAFKA_PRODUCER_{_topicName?.ToUpper()}";
            }
        }

        public CKafkaProducer(
            ILogger<CKafkaProducer> logger, IConverterService converter, IPubSubService pubSub, IKafkaService kafka,
            string hostPort, string topicName
        ) {
            _logger = logger;
            _converter = converter;
            _pubSub = pubSub;
            _kafka = kafka;
            _hostPort = hostPort;
            _topicName = topicName;
        }

        public override void Dispose() {
            producer?.Dispose();
            kafkaSubs?.Dispose();
            _pubSub.DisposeAndRemoveAllSubscriber(KAFKA_NAME);
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await Task.Yield();
            if (producer == null) {
                producer = _kafka.CreateKafkaProducerInstance<string, string>(_hostPort);
            }
            if (observeable == null) {
                observeable = _pubSub.GetGlobalAppBehaviorSubject<KafkaMessage<string, dynamic>>(KAFKA_NAME);
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
                        _logger.LogError($"[KAFKA_PRODUCER] {e.Message}");
                    }
                    msgs.Remove(msg);
                }
                Thread.Sleep(1000);
            }
        }

    }

}
