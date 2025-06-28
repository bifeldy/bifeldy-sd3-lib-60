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

    public interface IPluginContext {
        ILogger Logger { get; }
        CPluginManager Manager { get; }
        CPluginFileWatcher FileWatcher { get; }
        ApplicationPartManager PartManager { get; set; }
    }

    public sealed class CPluginContext : IPluginContext {

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

        public CPluginContext(string pluginPath, IServiceCollection services, ILogger logger) {
            this.Logger = logger;

            this.Manager = new CPluginManager(pluginPath, services, logger);
            this.FileWatcher = new CPluginFileWatcher(pluginPath);

            AppDomain.CurrentDomain.DomainUnload += (_, __) => this.Manager.UnloadAll();
            AppDomain.CurrentDomain.ProcessExit += (_, __) => this.Manager.UnloadAll(true);
        }

    }

}
