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

using bifeldy_sd3_lib_60.Conventions;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class MvcOptionsExtensions {

        private static void UseRoutePrefix(this MvcOptions opts, IRouteTemplateProvider routeAttribute) => opts.Conventions.Add(new RoutePrefixConvention(routeAttribute));

        public static void UseRoutePrefix(this MvcOptions opts, string prefix) => opts.UseRoutePrefix(new RouteAttribute(prefix));

        public static void UseHideControllerEndPointDc(this MvcOptions opts, IServiceCollection sc) {
            IServiceProvider sp = sc.BuildServiceProvider();
            opts.Conventions.Add(new HideSwaggerConvention(sp));
        }

    }

}
