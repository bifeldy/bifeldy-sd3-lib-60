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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class PluginContext {

        public ILogger Logger { get; }

        public PluginManager Manager { get; }
        public PluginFileWatcher FileWatcher { get; }

        private ApplicationPartManager _partManager;
        public ApplicationPartManager PartManager {
            get => this._partManager;
            set {
                this._partManager = value;
                this.Manager.SetPartManager(value);
            }
        }

        public PluginContext(string pluginPath, IServiceCollection services, ILogger logger) {
            this.Logger = logger;

            this.Manager = new PluginManager(pluginPath, services, logger);
            this.FileWatcher = new PluginFileWatcher(pluginPath);

            AppDomain.CurrentDomain.DomainUnload += (_, __) => this.Manager.UnloadAll();
            AppDomain.CurrentDomain.ProcessExit += (_, __) => this.Manager.UnloadAll(true);
        }

    }

}
