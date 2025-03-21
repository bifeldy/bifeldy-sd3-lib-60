/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Menyembunyikan Controller & Action Untuk DC Tertentu
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Conventions {

    public sealed class HideSwaggerConvention : IApplicationModelConvention {

        private readonly IApplicationService _application;
        private readonly IGeneralRepository _generalRepository;

        public HideSwaggerConvention(IServiceProvider sp) {
            _application = sp.GetRequiredService<IApplicationService>();
            _generalRepository = sp.GetRequiredService<IGeneralRepository>();
        }

        private void SwaggerHide(Type hideType, ActionModel action, string kodeDc, EJenisDc jenisDc) {
            if (this._application.DebugMode) {
                action.ApiExplorer.IsVisible = true;
            }
            else if (
                (hideType == typeof(RouteExcludeDcHoAttribute) && kodeDc == "DCHO") ||
                (hideType == typeof(RouteExcludeWhHoAttribute) && kodeDc == "WHHO") ||
                (hideType == typeof(RouteExcludeAllDcAttribute) && kodeDc != "DCHO" && kodeDc != "WHHO") ||
                (hideType == typeof(RouteExcludeIndukAttribute) && jenisDc == EJenisDc.INDUK) ||
                (hideType == typeof(RouteExcludeDepoAttribute) && jenisDc == EJenisDc.DEPO) ||
                (hideType == typeof(RouteExcludeKonvinienceAttribute) && jenisDc == EJenisDc.KONVINIENCE) ||
                (hideType == typeof(RouteExcludeIplazaAttribute) && jenisDc == EJenisDc.IPLAZA) ||
                (hideType == typeof(RouteExcludeFrozenAttribute) && jenisDc == EJenisDc.FROZEN) ||
                (hideType == typeof(RouteExcludePerishableAttribute) && jenisDc == EJenisDc.PERISHABLE) ||
                (hideType == typeof(RouteExcludeLpgAttribute) && jenisDc == EJenisDc.LPG) ||
                (hideType == typeof(RouteExcludeSewaAttribute) && jenisDc == EJenisDc.SEWA)
            ) {
                action.ApiExplorer.IsVisible = false;
            }
        }

        public void Apply(ApplicationModel application) {
            string kodeDc = this._generalRepository.GetKodeDc().Result;

            EJenisDc jenisDc = this._generalRepository.GetJenisDc().Result;

            Type[] typesToCheck = new[] {
                typeof(RouteExcludeDcHoAttribute),
                typeof(RouteExcludeWhHoAttribute),
                typeof(RouteExcludeAllDcAttribute),
                typeof(RouteExcludeIndukAttribute),
                typeof(RouteExcludeDepoAttribute),
                typeof(RouteExcludeKonvinienceAttribute),
                typeof(RouteExcludeIplazaAttribute),
                typeof(RouteExcludeFrozenAttribute),
                typeof(RouteExcludePerishableAttribute),
                typeof(RouteExcludeLpgAttribute),
                typeof(RouteExcludeSewaAttribute)
            };

            foreach (ControllerModel controller in application.Controllers) {
                var typeToHide = new List<Type>();

                foreach (object ctrlAttrib in controller.Attributes) {
                    Type type = ctrlAttrib.GetType();
                    if (typesToCheck.Contains(type)) {
                        typeToHide.Add(type);
                    }
                }

                foreach (ActionModel action in controller.Actions) {
                    foreach(object actAttrib in action.Attributes) {
                        Type type = actAttrib.GetType();
                        if (typesToCheck.Contains(type)) {
                            this.SwaggerHide(type, action, kodeDc, jenisDc);
                        }
                    }
                    
                    foreach(Type type in typeToHide) {
                        this.SwaggerHide(type, action, kodeDc, jenisDc);
                    }
                }
            }
        }

    }

}
