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

using bifeldy_sd3_lib_60.Conventions;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class MvcOptionsExtensions {

        public static void UseRoutePrefix(this MvcOptions opts, IRouteTemplateProvider routeAttribute) => opts.Conventions.Add(new RoutePrefixConvention(routeAttribute));

        public static void UseRoutePrefix(this MvcOptions opts, string prefix) => opts.UseRoutePrefix(new RouteAttribute(prefix));

    }

}
