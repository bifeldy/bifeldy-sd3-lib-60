/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Conventions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class MvcOptionsExtensions {

        private static void UseRoutePrefix(this MvcOptions opts, IRouteTemplateProvider routeAttribute) => opts.Conventions.Add(new RoutePrefixConvention(routeAttribute));

        public static void UseRoutePrefix(this MvcOptions opts, string prefix) => opts.UseRoutePrefix(new RouteAttribute(prefix));

        public static void UseHideControllerEndPointDc(this MvcOptions opts, IServiceProvider sp) {
            IOptions<EnvVar> env = sp.GetRequiredService<IOptions<EnvVar>>();
            IApplicationService app = sp.GetRequiredService<IApplicationService>();

            var hsc = new HideSwaggerConvention(env, app);

            opts.Conventions.Add(hsc);
        }

    }

}
