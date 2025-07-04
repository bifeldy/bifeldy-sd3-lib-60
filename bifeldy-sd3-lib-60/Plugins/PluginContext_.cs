/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Registrasi Plugin
* 
*/

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class CPluginContext {

        public ILogger Logger { get; }

        public CPluginManager Manager { get; }
        public CPluginFileWatcher FileWatcher { get; }

        private ApplicationPartManager _partManager;
        public ApplicationPartManager PartManager {
            get => this._partManager;
            set {
                this._partManager = value;
                this.Manager.SetPartManager(value);
            }
        }

        public CPluginContext(string pluginPath, ILogger logger, IOptions<EnvVar> envVar) {
            this.Logger = logger;

            this.Manager = new CPluginManager(pluginPath, logger, envVar);
            this.FileWatcher = new CPluginFileWatcher(pluginPath);

            AppDomain.CurrentDomain.DomainUnload += (_, __) => this.Manager.UnloadAll();
            AppDomain.CurrentDomain.ProcessExit += (_, __) => this.Manager.UnloadAll(true);
        }

    }

}
