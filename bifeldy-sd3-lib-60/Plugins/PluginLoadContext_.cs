/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Load Plugin
* 
*/

using System.Reflection;
using System.Runtime.Loader;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class CPluginLoadContext : AssemblyLoadContext {

        private readonly AssemblyDependencyResolver _resolver;

        public CPluginLoadContext(string path) : base(isCollectible: true) {
            this._resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly Load(AssemblyName name) {
            string path = this._resolver.ResolveAssemblyToPath(name);
            return path != null ? this.LoadFromAssemblyPath(path) : null;
        }

    }

}
