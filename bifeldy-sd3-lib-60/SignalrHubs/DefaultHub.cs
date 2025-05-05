/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: /signalr/default
 *              :: Websocket Pemberitahuan & Informasi Dari Server
 * 
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.SignalrHubs {

    public sealed class DefaultHub : Hub {

        private readonly EnvVar _envVar;
        private readonly IGlobalService _gs;

        public DefaultHub(
            IOptions<EnvVar> env,
            IGlobalService gs
        ) {
            this._envVar = env.Value;
            this._gs = gs;
        }

        public override Task OnConnectedAsync() {
            if (!this._gs.SignalrClients.ContainsKey(this.Context.ConnectionId)) {
                HttpContext http = this.Context.GetHttpContext();
                string ipOrigin = this._gs.GetIpOriginData(http.Connection, http.Request);

                this._gs.SignalrClients.Add(this.Context.ConnectionId, ipOrigin);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception) {
            if (this._gs.SignalrClients.ContainsKey(this.Context.ConnectionId)) {
                _ = this._gs.SignalrClients.Remove(this.Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public void RegisterIdentity(string clientName) {
            if (this._gs.SignalrClients.ContainsKey(this.Context.ConnectionId)) {
                this._gs.SignalrClients[this.Context.ConnectionId] = clientName;
            }
        }

        public async Task BroadcastAnnouncement(string message) {
            await this.Clients.All.SendAsync("BroadcastAnnouncement", message);
        }

    }

}
