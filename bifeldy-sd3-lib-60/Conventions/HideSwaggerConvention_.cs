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
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Conventions {

    public sealed class HideSwaggerConvention : IApplicationModelConvention {

        private readonly EnvVar _env;

        private readonly IOraPg _orapg;
        private readonly IApplicationService _application;
        private readonly IGeneralRepository _generalRepository;

        public HideSwaggerConvention(IServiceProvider sp) {
            this._env = sp.GetRequiredService<IOptions<EnvVar>>().Value;
            this._orapg = sp.GetRequiredService<IOraPg>();
            this._application = sp.GetRequiredService<IApplicationService>();
            this._generalRepository = sp.GetRequiredService<IGeneralRepository>();
        }

        private void SwaggerHide(Type hideType, ActionModel action, string kodeDc, EJenisDc jenisDc) {
            if (this._application.DebugMode) {
                action.ApiExplorer.IsVisible = true;
            }
            else if (
                (hideType == typeof(RouteExcludeKonsolidasiCbnAttribute) && kodeDc == "KCBN") ||
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
            string kodeDc = this._generalRepository.GetKodeDc(this._env.IS_USING_POSTGRES, this._orapg).Result;

            EJenisDc jenisDc = this._generalRepository.GetJenisDc(this._env.IS_USING_POSTGRES, this._orapg).Result;

            Type[] typesToCheck = new[] {
                typeof(RouteExcludeKonsolidasiCbnAttribute),
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
                var typeToHideController = new List<Type>();

                foreach (object ctrlAttrib in controller.Attributes) {
                    Type type = ctrlAttrib.GetType();
                    if (typesToCheck.Contains(type)) {
                        typeToHideController.Add(type);
                    }
                }

                foreach (ActionModel action in controller.Actions) {
                    foreach (object actAttrib in action.Attributes) {
                        Type typeToHideAction = actAttrib.GetType();
                        if (typesToCheck.Contains(typeToHideAction)) {
                            this.SwaggerHide(typeToHideAction, action, kodeDc, jenisDc);
                        }
                        else if (this._application.DebugMode) {
                            action.ApiExplorer.IsVisible = true;
                        }
                    }
                    
                    foreach (Type type in typeToHideController) {
                        this.SwaggerHide(type, action, kodeDc, jenisDc);
                    }

                    if (
                        !this._application.DebugMode &&
                        controller.DisplayName.ToLower().Contains("bifeldy_sd3_lib_60") &&
                        action.ActionName.ToUpper().Contains("GRPC") &&
                        this._env.GRPC_PORT <= 0
                    ) {
                        action.ApiExplorer.IsVisible = false;
                    }
                }
            }
        }

    }

}
