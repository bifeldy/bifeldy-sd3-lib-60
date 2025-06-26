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

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class PluginContext {

        public PluginManager Manager { get; }

        public ApplicationPartManager PartManager {
            get => _partManager;
            set {
                _partManager = value;
                this.Manager.SetPartManager(value);
            }
        }

        private ApplicationPartManager _partManager;

        public PluginContext(string pluginPath) {
            this.Manager = new PluginManager(pluginPath);
        }

    }

}
