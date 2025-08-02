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

using Microsoft.Extensions.DependencyInjection;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class PluginServiceProvider : IServiceProvider {

        private readonly IServiceProvider _pluginProvider;
        private readonly IServiceProvider _hostProvider;

        public PluginServiceProvider(IServiceProvider pluginProvider, IServiceProvider hostProvider) {
            this._pluginProvider = pluginProvider;
            this._hostProvider = hostProvider;
        }

        public object GetService(Type serviceType) {
            object service = this._pluginProvider.GetService(serviceType);

            if (service == null) {
                service = this._hostProvider.GetService(serviceType);
            }

            return service;
        }
    }

    public sealed class PluginServiceScope : IServiceScope {

        public IServiceProvider ServiceProvider { get; }

        private readonly IServiceScope _pluginScoped;
        private readonly IServiceScope _hostScoped;

        public PluginServiceScope(IServiceScope pluginScoped, IServiceScope hostScoped) {
            this._pluginScoped = pluginScoped;
            this._hostScoped = hostScoped;
            //
            this.ServiceProvider = new PluginServiceProvider(
                pluginScoped.ServiceProvider,
                hostScoped.ServiceProvider
            );
        }

        public void Dispose() {
            this._pluginScoped.Dispose();
            this._hostScoped.Dispose();
        }

    }

    public class PluginServiceScopeFactory : IServiceScopeFactory {

        private readonly IServiceProvider _pluginRootProvider;
        private readonly IServiceProvider _hostRootProvider;

        public PluginServiceScopeFactory(IServiceProvider pluginRootProvider, IServiceProvider hostRootProvider) {
            this._pluginRootProvider = pluginRootProvider;
            this._hostRootProvider = hostRootProvider;
        }

        public IServiceScope CreateScope() {
            IServiceScope pluginScoped = this._pluginRootProvider.CreateScope();
            IServiceScope hostScoped = this._hostRootProvider.CreateScope();
            return new PluginServiceScope(pluginScoped, hostScoped);
        }
    }

}
