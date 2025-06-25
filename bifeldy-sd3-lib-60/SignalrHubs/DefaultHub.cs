/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: /signalr/default
 *              :: Websocket Default Bisa Buat Contoh & Untuk Turunan ~
 * 
 */

// using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.SignalrHubs {

    public interface IDefaultHub : IClientProxy {
        void RegisterIdentity(string clientName);
        // Task BroadcastAnnouncement(string message);
        // Task JoinGroup(string groupName);
        // Task LeaveGroup(string groupName);
        // Task GroupMessage(string groupName, string message, string sender = null);
    }

    // [Authorize(Roles = "PROGRAM_SERVICE")]
    // [Authorize(Roles = "USER_SD_SSD_3")]
    public class DefaultHub : Hub<IDefaultHub> {

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
                string ipOrigin = this._gs.GetIpOriginData(http.Connection, http.Request, removeReverseProxyRoute: true);

                this._gs.SignalrClients.Add(this.Context.ConnectionId, $"NULL ({ipOrigin})");
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception) {
            if (this._gs.SignalrClients.ContainsKey(this.Context.ConnectionId)) {
                _ = this._gs.SignalrClients.Remove(this.Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        //
        // -- TODO :: Auth, Claim, User .. etc
        //

        public void RegisterIdentity(string clientName) {
            string clientId = this.Context.ConnectionId;
            if (this._gs.SignalrClients.ContainsKey(clientId)) {
                HttpContext http = this.Context.GetHttpContext();
                string ipOrigin = this._gs.GetIpOriginData(http.Connection, http.Request, removeReverseProxyRoute: true);
                this._gs.SignalrClients[clientId] = $"{clientName} ({ipOrigin})";
            }
        }

        // public async Task BroadcastAnnouncement(string message) {
        //     await this.Clients.All.SendAsync("BroadcastAnnouncement", message);
        // }

        // public async Task JoinGroup(string groupName) {
        //     string clientId = this.Context.ConnectionId;
        //     if (this._gs.SignalrClients.ContainsKey(clientId)) {
        //         string clientName = this._gs.SignalrClients[clientId];
        //         await this.Groups.AddToGroupAsync(clientId, groupName);
        //         await this.GroupMessage(groupName, $"`{clientName}` has joined the group `{groupName}`.");
        //     }
        //     else {
        //         await this.Clients.Caller.SendAsync("SystemMessage", "Please Register Your Identity!");
        //     }
        // }

        // public async Task LeaveGroup(string groupName) {
        //     string clientId = this.Context.ConnectionId;
        //     if (this._gs.SignalrClients.ContainsKey(clientId)) {
        //         string clientName = this._gs.SignalrClients[clientId];
        //         await this.Groups.RemoveFromGroupAsync(clientId, groupName);
        //         await this.GroupMessage(groupName, $"`{clientName}` has left the group `{groupName}`.");
        //     }
        //     else {
        //         await this.Clients.Caller.SendAsync("SystemMessage", "Please Register Your Identity!");
        //     }
        // }

        // public async Task GroupMessage(string groupName, string message, string sender = null) {
        //     await this.Clients.Group(groupName).SendAsync("GroupMessage", message, sender);
        // }

    }

}
