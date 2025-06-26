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
        void RegisterServices(IServiceCollection services);
    }

}
