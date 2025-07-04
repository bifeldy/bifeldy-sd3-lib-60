/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Bridge Ke Service Hostnya ~
* 
*/

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class PluginServiceProvider : IServiceProvider {

        private readonly IServiceProvider _pluginProvider;
        private readonly IServiceProvider _hostProvider;

        public PluginServiceProvider(IServiceProvider pluginProvider, IServiceProvider hostProvider) {
            this._pluginProvider = pluginProvider;
            this._hostProvider = hostProvider;
        }

        public object GetService(Type serviceType) {
            try {
                object service = this._pluginProvider.GetService(serviceType);

                if (service == null) {
                    service = this._hostProvider.GetService(serviceType);
                }

                return service;
            }
            catch {
                return this._hostProvider.GetService(serviceType);
            }
        }

    }

}
