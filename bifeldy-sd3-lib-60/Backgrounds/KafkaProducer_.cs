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
        private readonly string _topic;

        IProducer<string, string> producer = null;

        BehaviorSubject<KafkaMessage<string, dynamic>> observeable = null;

        private string KAFKA_NAME {
            get {
                return $"KAFKA_PRODUCER_{_topic?.ToUpper()}";
            }
        }

        public CKafkaProducer(
            ILogger<CKafkaProducer> logger, IConverterService converter, IPubSubService pubSub, IKafkaService kafka,
            string hostPort, string topic
        ) {
            _logger = logger;
            _converter = converter;
            _pubSub = pubSub;
            _kafka = kafka;
            _hostPort = hostPort;
            _topic = topic;
        }

        public override void Dispose() {
            producer?.Dispose();
            _pubSub.Unsubscribe(KAFKA_NAME);
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await Task.Yield();
            if (producer == null) {
                producer = _kafka.CreateKafkaProducerInstance<string, string>(_hostPort);
            }
            if (observeable == null) {
                observeable = _pubSub.CreateGlobalAppBehaviorSubject<KafkaMessage<string, dynamic>>(KAFKA_NAME, null);
                observeable.Subscribe(async data => {
                    Message<string, string> msg = new Message<string, string> {
                        Key = data.Key,
                        Value = typeof(string) == data.Value.GetType() ? data.Value : _converter.ObjectToJson(data.Value)
                    };
                    await producer.ProduceAsync(_topic, msg, stoppingToken);
                });
            }
            while (!stoppingToken.IsCancellationRequested) {
                //
            }
        }

    }

}
