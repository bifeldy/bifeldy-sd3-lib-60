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
                (hideType == typeof(SwaggerExcludeHoAttribute) && kodeDc == "DCHO") ||
                (hideType == typeof(SwaggerExcludeDcAttribute) && kodeDc != "DCHO") ||
                (hideType == typeof(SwaggerExcludeIndukAttribute) && jenisDc == "INDUK") ||
                (hideType == typeof(SwaggerExcludeDepoAttribute) && jenisDc == "DEPO") ||
                (hideType == typeof(SwaggerExcludeKonvinienceAttribute) && jenisDc == "KONVINIENCE") ||
                (hideType == typeof(SwaggerExcludeIplazaAttribute) && jenisDc == "IPLAZA") ||
                (hideType == typeof(SwaggerExcludeFrozenAttribute) && jenisDc == "FROZEN") ||
                (hideType == typeof(SwaggerExcludePerishableAttribute) && jenisDc == "PERISHABLE") ||
                (hideType == typeof(SwaggerExcludeLpgAttribute) && jenisDc == "LPG") ||
                (hideType == typeof(SwaggerExcludeSewaAttribute) && jenisDc == "SEWA")
            ) {
                action.ApiExplorer.IsVisible = false;
            }
        }

        public void Apply(ApplicationModel application) {

            // Paksa Mode SYNC
            string kodeDc = this._generalRepository.GetKodeDc().Result;
            string jenisDc = this._generalRepository.GetJenisDc().Result;

            Type[] typesToCheck = new[] {
                typeof(SwaggerExcludeHoAttribute),
                typeof(SwaggerExcludeDcAttribute),
                typeof(SwaggerExcludeIndukAttribute),
                typeof(SwaggerExcludeDepoAttribute),
                typeof(SwaggerExcludeKonvinienceAttribute),
                typeof(SwaggerExcludeIplazaAttribute),
                typeof(SwaggerExcludeFrozenAttribute),
                typeof(SwaggerExcludePerishableAttribute),
                typeof(SwaggerExcludeLpgAttribute),
                typeof(SwaggerExcludeSewaAttribute)
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
