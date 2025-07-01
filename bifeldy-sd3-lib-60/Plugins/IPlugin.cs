/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Template Injeksi Plugin
* 
*/

using Microsoft.Extensions.DependencyInjection;

namespace bifeldy_sd3_lib_60.Plugins {

    public interface IPlugin {
        void RegisterServices(IServiceCollection serviceProvider);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CPluginInfoAttribute : Attribute {

        public string Name { get; }
        public string Version { get; }
        public string Author { get; }

        public CPluginInfoAttribute(string name, string version, string author) {
            this.Name = name;
            this.Version = version;
            this.Author = author;
        }

    }

}
