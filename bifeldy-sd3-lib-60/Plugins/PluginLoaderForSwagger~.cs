/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Plugin Untuk Swagger Re/Load
* 
*/

using System.Reflection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

using Swashbuckle.AspNetCore.Swagger;

namespace bifeldy_sd3_lib_60.Plugins {

    public static class CPluginLoaderForSwagger {

        public static void LoadAllPlugins(IPluginContext pluginContext, string pluginDir) {
            foreach (string dllAsFolderName in Directory.GetDirectories(pluginDir, "*", SearchOption.TopDirectoryOnly)) {
                string pluginName = Path.GetFileName(dllAsFolderName);
                pluginContext.Manager.LoadPlugin(pluginName);
            }
        }

        public static void RegisterSwaggerReload(IPluginContext pluginContext) {
            pluginContext.Manager.PluginReloaded += pluginName => {
                pluginContext.Logger.LogInformation("[SWAGGER] Reloading Plugin 💉 {pluginName}", pluginName);

                try {
                    IWebHostEnvironment environment = Bifeldy.App.Services.GetRequiredService<IWebHostEnvironment>();
                    ISwaggerProvider provider = Bifeldy.App.Services.GetRequiredService<ISwaggerProvider>();

                    if (!Directory.Exists(environment.WebRootPath)) {
                        _ = Directory.CreateDirectory(environment.WebRootPath);
                    }

                    string appVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
                    OpenApiDocument swaggerDoc = provider.GetSwagger(appVersion);

                    string jsonPath = Path.Combine(environment.WebRootPath, "swagger.json");
                    using (var streamWriter = new StreamWriter(jsonPath)) {
                        var writer = new OpenApiJsonWriter(streamWriter);
                        swaggerDoc.SerializeAsV3(writer);
                    }

                    pluginContext.Logger.LogInformation("[SWAGGER] JSON Updated Successfully.");
                }
                catch (Exception ex) {
                    pluginContext.Logger.LogError(ex, "[SWAGGER] Failed To Reload Plugin 💉 {pluginName}", pluginName);
                }
            };
        }

    }

}
