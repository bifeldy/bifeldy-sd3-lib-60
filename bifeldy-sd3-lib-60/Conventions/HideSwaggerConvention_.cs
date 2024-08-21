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

namespace bifeldy_sd3_lib_60.Conventions {

    public sealed class HideSwaggerConvention : IApplicationModelConvention {

        private readonly IGeneralRepository _generalRepository;

        public HideSwaggerConvention(IServiceProvider sp) {
            _generalRepository = sp.GetRequiredService<IGeneralRepository>();
        }

        private void SwaggerHide(Type hideType, ActionModel action, string kodeDc, string jenisDc) {
            if (
                (hideType == typeof(SwaggerHideHoAttribute) && kodeDc == "DCHO") ||
                (hideType == typeof(SwaggerHideDcAttribute) && kodeDc != "DCHO") ||
                (hideType == typeof(SwaggerHideIndukAttribute) && jenisDc == "INDUK") ||
                (hideType == typeof(SwaggerHideDepoAttribute) && jenisDc == "DEPO") ||
                (hideType == typeof(SwaggerHideKonvinienceAttribute) && jenisDc == "KONVINIENCE") ||
                (hideType == typeof(SwaggerHideIplazaAttribute) && jenisDc == "IPLAZA") ||
                (hideType == typeof(SwaggerHideFrozenAttribute) && jenisDc == "FROZEN") ||
                (hideType == typeof(SwaggerHidePerishableAttribute) && jenisDc == "PERISHABLE") ||
                (hideType == typeof(SwaggerHideLpgAttribute) && jenisDc == "LPG") ||
                (hideType == typeof(SwaggerHideSewaAttribute) && jenisDc == "SEWA")
            ) {
                action.ApiExplorer.IsVisible = false;
            }
        }

        public void Apply(ApplicationModel application) {

            // Paksa Mode SYNC
            string kodeDc = this._generalRepository.GetKodeDc().Result;
            string jenisDc = "DEPO"; // this._generalRepository.GetJenisDc().Result;

            Type[] typesToCheck = new[] {
                typeof(SwaggerHideHoAttribute),
                typeof(SwaggerHideDcAttribute),
                typeof(SwaggerHideIndukAttribute),
                typeof(SwaggerHideDepoAttribute),
                typeof(SwaggerHideKonvinienceAttribute),
                typeof(SwaggerHideIplazaAttribute),
                typeof(SwaggerHideFrozenAttribute),
                typeof(SwaggerHidePerishableAttribute),
                typeof(SwaggerHideLpgAttribute),
                typeof(SwaggerHideSewaAttribute)
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
