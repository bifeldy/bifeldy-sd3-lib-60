/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Buat Contoh Plugin ~
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Plugins;

namespace bifeldy_sd3_lib_60.Controllers {

    [ApiController]
    [Route("plugin")]
    [RouteExcludeAllDc]
    [MinRole(UserSessionRole.EXTERNAL_BOT)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class PluginController : ControllerBase {

        private readonly CPluginManager _pluginManager;

        public PluginController(IPluginContext context) {
            this._pluginManager = context.Manager;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CPluginInfo>> GetPlugins(
            [FromQuery, SwaggerParameter("Nama plugin (ex. blablabla.dll => blablabla)", Required = false)] string name = null,
            [FromQuery, SwaggerParameter("Aksi (ex. reload, unload)", Required = false)] string action = null
        ) {
            try {
                var user = (UserApiSession)this.HttpContext.Items["user"];

                if (string.IsNullOrEmpty(action)) {
                    List<CPluginInfo> plugins = this._pluginManager.GetLoadedPluginInfos();

                    if (!string.IsNullOrEmpty(name)) {
                        plugins = plugins.Where(p => p.Name?.ToUpper() == name.ToUpper()).ToList();
                    }

                    return this.Ok(new ResponseJsonMulti<CPluginInfo> {
                        info = $"200 - {this.GetType().Name} :: All Plugins",
                        results = plugins,
                        pages = 1,
                        count = plugins.Count
                    });
                }

                if (string.IsNullOrEmpty(name)) {
                    return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {this.GetType().Name} :: Detail Plugin",
                        result = new ResponseJsonMessage() {
                            message = "Nama Plugin Tidak Tersedia"
                        }
                    });
                }

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(action)) {
                    action = action.ToUpper();
                    if (action == "RELOAD") {
                        this._pluginManager.LoadPlugin(name);
                    }
                    else if (action == "UNLOAD") {
                        this._pluginManager.UnloadPlugin(name);
                    }

                    return this.Ok(new ResponseJsonSingle<ResponseJsonMessage> {
                        info = $"200 - {this.GetType().Name} :: Aksi Plugin",
                        result = new ResponseJsonMessage() {
                            message = $"Berhasil {action} Plugin"
                        }
                    });
                }

                throw new Exception("Data Tidak Lengkap");
            }
            catch {
                return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {this.GetType().Name} :: Plugin Manager",
                    result = new ResponseJsonMessage() {
                        message = "Data Tidak Lengkap"
                    }
                });
            }
        }

    }

}
