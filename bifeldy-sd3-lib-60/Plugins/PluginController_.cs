using Microsoft.AspNetCore.Mvc;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Plugins {

    [ApiController]
    [MinRole(UserSessionRole.EXTERNAL_BOT)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("plugins")]
    public sealed class PluginController : ControllerBase {

        private readonly PluginManager _pluginManager;

        public PluginController(PluginContext context) {
            this._pluginManager = context.Manager;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PluginInfoDto>> GetPlugins() {
            List<PluginInfoDto> plugins = this._pluginManager.GetLoadedPluginInfos();
            return this.Ok(new ResponseJsonMulti<PluginInfoDto> {
                info = $"200 - {this.GetType().Name} :: All Plugins",
                results = plugins
            });
        }

        [HttpPost("reload")]
        public IActionResult ReloadPlugin([FromQuery] string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {this.GetType().Name} :: Reload Plugin",
                    result = new ResponseJsonMessage() {
                        message = "Nama Plugin Tidak Tersedia"
                    }
                });
            }

            this._pluginManager.LoadPlugin(name);

            return this.Ok(new ResponseJsonSingle<ResponseJsonMessage> {
                info = $"200 - {this.GetType().Name} :: Reload Plugin",
                result = new ResponseJsonMessage() {
                    message = "Berhasil Restart Plugin"
                }
            });
        }

        [HttpPost("unload")]
        public IActionResult UnloadPlugin([FromQuery] string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {this.GetType().Name} :: Reload Plugin",
                    result = new ResponseJsonMessage() {
                        message = "Nama Plugin Tidak Tersedia"
                    }
                });
            }

            this._pluginManager.UnloadPlugin(name);

            return this.Ok(new ResponseJsonSingle<ResponseJsonMessage> {
                info = $"200 - {this.GetType().Name} :: Unload Plugin",
                result = new ResponseJsonMessage() {
                    message = "Berhasil Stop Plugin"
                }
            });
        }

    }

}
