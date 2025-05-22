/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: SignalR CLient
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Libraries;

namespace bifeldy_sd3_lib_60.Services {

    public interface ISignalrService {
        HubConnection CreateClient(Uri uri, bool autoReconnect = true, Func<Exception, Task> closed = null, Func<Exception, Task> reconnecting = null, Func<string, Task> reconnected = null);
    }

    [SingletonServiceRegistration]
    public sealed class CSignalrService : ISignalrService {

        private readonly ILogger<CSignalrService> _logger;

        public CSignalrService(
            ILogger<CSignalrService> logger
        ) {
            this._logger = logger;
        }

        public HubConnection CreateClient(Uri uri, bool autoReconnect = true, Func<Exception, Task> closed = null, Func<Exception, Task> reconnecting = null, Func<string, Task> reconnected = null) {
            IHubConnectionBuilder connection = new HubConnectionBuilder().WithUrl(uri);

            if (autoReconnect) {
                connection = connection.WithAutomaticReconnect(new SignalrUnlimitedRetryPolicy(this._logger));
            }

            HubConnection client = connection.Build();

            if (autoReconnect) {
                client.Closed += async (error) => {
                    this._logger.LogError("[SIGNALR_CLOSED] {error}", error);

                    if (autoReconnect && error != null) {
                        if (client.State != HubConnectionState.Disconnected) {
                            await client.StopAsync();
                        }

                        await Task.Delay(new Random().Next(5, 10) * 1000);
                        await client.StartAsync();
                    }

                    if (closed != null) {
                        await closed(error);
                    }
                };

                client.Reconnecting += async (error) => {
                    this._logger.LogError("[SIGNALR_RECONNECTING] {error}", error);

                    if (reconnecting != null) {
                        await reconnecting(error);
                    }
                };

                client.Reconnected += async (connectionId) => {
                    this._logger.LogInformation("[SIGNALR_RECONNECTED] {connectionId}", connectionId);

                    if (reconnected != null) {
                        await reconnected(connectionId);
                    }
                };
            }

            return client;
        }

    }

}
