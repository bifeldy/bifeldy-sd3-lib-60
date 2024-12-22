/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: GRPCs
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Grpc.Core;
using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Services {

    public interface IGRpcClientService {
        GrpcChannel CreateChannel(string host, int port);
        GrpcClient ClientGetService<T>(string host, int port);
    }

    [SingletonServiceRegistration]
    public sealed class CGRpcClientService : IGRpcClientService {

        public CGRpcClientService() {
            //
        }

        public GrpcChannel CreateChannel(string host, int port) {
            var opt = new GrpcChannelOptions() {
                Credentials = ChannelCredentials.Insecure
            };
            if (!host.StartsWith("http")) {
                host = $"http://{host}";
            }

            return GrpcChannel.ForAddress($"{host}:{port}", opt);
        }

        public GrpcClient ClientGetService<T>(string host, int port) {
            GrpcChannel channel = this.CreateChannel(host, port);
            return channel.CreateGrpcService(typeof(T));
        }

    }

}
