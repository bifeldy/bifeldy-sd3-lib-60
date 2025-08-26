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
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Conventions {

    public sealed class HideSwaggerConvention : IApplicationModelConvention {

        private readonly EnvVar _env;
        private readonly IApplicationService _app;

        public HideSwaggerConvention(IOptions<EnvVar> env, IApplicationService app) {
            this._env = env.Value;
            this._app = app;
        }

        public void Apply(ApplicationModel applicationModel) {
            foreach (ControllerModel controller in applicationModel.Controllers) {
                foreach (ActionModel action in controller.Actions) {
                    if (this._app.DebugMode) {
                        action.ApiExplorer.IsVisible = true;
                    }

                    if (
                        !this._app.DebugMode && this._env.GRPC_PORT <= 0 &&
                        action.ActionName.ToUpper().Contains("GRPC") &&
                        controller.DisplayName.ToLower().Contains("bifeldy_sd3_lib_60")
                    ) {
                        action.ApiExplorer.IsVisible = false;
                    }
                }
            }
        }

    }

}
