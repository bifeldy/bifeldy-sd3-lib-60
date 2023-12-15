/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Saran Penggunaan => /api
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace bifeldy_sd3_lib_60.Utilities {

    public sealed class RoutePrefix : IApplicationModelConvention {

        private readonly AttributeRouteModel _routePrefix;

        public RoutePrefix(IRouteTemplateProvider route) {
            _routePrefix = new AttributeRouteModel(route);
        }

        public void Apply(ApplicationModel application) {
            foreach (SelectorModel selector in application.Controllers.SelectMany(c => c.Selectors)) {
                if (selector.AttributeRouteModel != null) {
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
                }
                else {
                    selector.AttributeRouteModel = _routePrefix;
                }
            }
        }

    }

}
