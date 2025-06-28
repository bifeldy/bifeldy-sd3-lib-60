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
    public sealed class PluginInfoAttribute : Attribute {

        public string Name { get; }
        public string Version { get; }
        public string Author { get; }

        public PluginInfoAttribute(string name, string version, string author) {
            this.Name = name;
            this.Version = version;
            this.Author = author;
        }

    }

    public sealed class PluginInfoDto {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
    }

}
