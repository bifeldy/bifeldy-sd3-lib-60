/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Megang Semua Plugin Aktif + Re/Load
* 
*/

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class CPluginManager {

        private readonly EnvVar _envVar;

        private readonly string _pluginDir;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        private ApplicationPartManager _partManager;

        private readonly ConcurrentDictionary<string, IPlugin> _pluginInstances = new();
        private readonly ConcurrentDictionary<string, IServiceProvider> _pluginServiceProviders = new();

        public static ConcurrentDictionary<string, (CPluginLoadContext, Assembly, FileStream, string)> LOADED_PLUGIN = new();

        public CPluginManager(string pluginDir, ILogger logger, IOptions<EnvVar> envVar, IServiceProvider serviceProvider) {
            this._pluginDir = pluginDir;
            this._logger = logger;
            this._envVar = envVar.Value;
            this._serviceProvider = serviceProvider.CreateScope().ServiceProvider;
        }

        public void SetPartManager(ApplicationPartManager partManager) {
            this._partManager = partManager;
        }

        public void LoadPlugin(string name, bool reloadDynamicApiPluginRouteEndpoint = false) {
            name = name.RemoveIllegalFileName();

            this._logger.LogInformation("[PLUGIN] Loading 💉 {name}", name);

            try {
                string dllAsFolderName = Path.Combine(this._pluginDir, name);
                string mainDllPath = Path.Combine(dllAsFolderName, $"{name}.dll");

                if (!Directory.Exists(dllAsFolderName) || !File.Exists(mainDllPath) || this._partManager == null) {
                    this._logger.LogError($"[PLUGIN] DLL Not Found 💉 {mainDllPath}");
                    return;
                }

                DateTime lastWrite = File.GetLastWriteTimeUtc(mainDllPath);
                if (LOADED_PLUGIN.TryGetValue(name, out (CPluginLoadContext context, Assembly asm, FileStream fs, string tempPath) oldEntry)) {
                    DateTime oldWrite = File.GetLastWriteTimeUtc(oldEntry.tempPath);
                    if (lastWrite == oldWrite) {
                        return;
                    }
                }

                this.UnloadPlugin(name);

                string tempPath = Path.Combine(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.TEMP_FOLDER_PATH),
                    $"{Bifeldy.App.Environment.ApplicationName}_{name}_{Guid.NewGuid()}.dll"
                );

                if (!TryCopyWithRetry(mainDllPath, tempPath)) {
                    throw new Exception($"[PLUGIN] Failed To Copy Plugin DLL To Temp Folder 💉 {mainDllPath}");
                }

                lock (LOADED_PLUGIN) {
                    // Butuh Yang Asli Karena Barang Kali Ada External Lib.dll Yang 1 Folder Dengannya ~
                    var plc = new CPluginLoadContext(mainDllPath);

                    var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                    Assembly asm = plc.LoadFromStream(fs);

                    string[] newTypes = this.SafeGetTypes(asm)
                        .Where(t => !string.IsNullOrWhiteSpace(t.FullName))
                        .Select(t => t.FullName)
                        .ToArray();

                    var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var existingAssemblies = new List<Assembly> {
                        Assembly.GetExecutingAssembly(),
                        Assembly.GetEntryAssembly()
                    };

                    existingAssemblies.AddRange(LOADED_PLUGIN.Values.Select(v => v.Item2));

                    foreach (Assembly existingAsm in existingAssemblies) {
                        foreach (Type existingType in this.SafeGetTypes(existingAsm)) {
                            if (!string.IsNullOrWhiteSpace(existingType.FullName) && newTypes.Any(nt => nt.ToUpper() == existingType.FullName.ToUpper())) {
                                _ = duplicates.Add(existingType.FullName);
                                this._logger.LogError("Duplicate '{type}' Found 💉 '{pluginName}'", existingType.FullName, name);
                            }
                        }
                    }

                    LOADED_PLUGIN[name] = (plc, asm, fs, tempPath);

                    this._logger.LogInformation("[PLUGIN] Loaded All Required Dependencies 💉 {name}", name);

                    var notAllowedDuplicateNamespace = new List<string> {
                        this.GetType().Namespace
                    };

                    if (!string.IsNullOrEmpty(Bifeldy.PLUGINS_PROJECT_NAMESPACE)) {
                        notAllowedDuplicateNamespace.Add(Bifeldy.PLUGINS_PROJECT_NAMESPACE);
                    }

                    IEnumerable<string> filteredDuplicates = duplicates.Where(typeName => {
                        return notAllowedDuplicateNamespace.Any(prefix => {
                            return typeName.ToUpper().StartsWith(prefix.ToUpper());
                        });
                    });

                    if (filteredDuplicates.Count() > 0) {
                        throw new Exception($"[PLUGIN] Duplicate Type Names Found 💉 {string.Join(", ", filteredDuplicates)}");
                    }

                    if (!this.TryGetPluginType(asm, out Type type)) {
                        throw new Exception($"[PLUGIN] No Valid IPlugin Implementation Found 💉 {name}");
                    }

                    CPluginInfoAttribute info = type.GetCustomAttribute<CPluginInfoAttribute>();
                    if (info == null) {
                        throw new Exception($"[PLUGIN] No Metadata Attribute Found To Read 💉 {name}");
                    }

                    var controllers = this.GetPluginControllers(asm).ToList();
                    if (controllers.Count != 1) {
                        throw new Exception($"[PLUGIN] Maximum Allowed Only 1 Controller 💉 {name}");
                    }
                    else {
                        Type ctrl = controllers.First();
                        var attribs = ctrl.CustomAttributes.Where(attrib => attrib.AttributeType.Name == "RouteAttribute").ToList();
                        if (attribs.Count != 1) {
                            throw new Exception($"[PLUGIN] Maximum Allowed Only 1 Unique Route URL 💉 {name}");
                        }
                        else {
                            CustomAttributeData routeAttrib = attribs.First();
                            CustomAttributeTypedArgument urlPath = routeAttrib.ConstructorArguments.First();
                            if (urlPath.Value?.ToString() != name) {
                                throw new Exception($"[PLUGIN] Route URL Must Be Same With *.DLL File And Folder Name 💉 {name}");
                            }
                        }
                    }

                    var asmFileInfo = FileVersionInfo.GetVersionInfo(tempPath);
                    if (info.Name != asmFileInfo.ProductName || !asmFileInfo.ProductVersion.StartsWith(info.Version) || string.IsNullOrEmpty(info.Author)) {
                        throw new Exception($"[PLUGIN] Wrong / Invalid Assembly Metadata 💉 {name}");
                    }

                    this._logger.LogInformation(
                        "[PLUGIN] Metadata 💉 Name: {Name}, Version: {Version}, Author: {Author}",
                        info.Name, info.Version, info.Author
                    );

                    var plugin = (IPlugin)Activator.CreateInstance(type);
                    this._pluginInstances[name] = plugin;

                    this._logger.LogInformation("[PLUGIN] Instance Created 💉 {name}", name);

                    var isolatedPluginServiceCollection = new ServiceCollection();

                    plugin.RegisterServices(isolatedPluginServiceCollection);

                    _ = isolatedPluginServiceCollection.SetupConfigOptions(this._serviceProvider);
                    _ = isolatedPluginServiceCollection.AddForwardAllService(Bifeldy.Services, this._serviceProvider);

                    this._pluginServiceProviders[name] = isolatedPluginServiceCollection.BuildServiceProvider(new ServiceProviderOptions());

                    this._logger.LogInformation("[PLUGIN] Dependency Injection Service Registered 💉 {name}", name);

                    if (this._partManager != null) {
                        if (!this._partManager.ApplicationParts.Any(p => p.Name.ToUpper() == asm.GetName().Name.ToUpper())) {
                            if (asm.GetCustomAttributes(typeof(RazorCompiledItemAttribute), inherit: false).Any()) {
                                this._partManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(asm));
                            }
                            else {
                                this._partManager.ApplicationParts.Add(new AssemblyPart(asm));
                            }
                        }

                        this._logger.LogInformation("[PLUGIN] Application Ready 💉 {name}", name);
                    }

                    if (reloadDynamicApiPluginRouteEndpoint) {
                        CDynamicActionDescriptorChangeProvider.Instance.NotifyChanges();
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[PLUGIN] Error '{pluginName}' 💉 {name}", name, ex.Message);
                this.UnloadPlugin(name);
                throw;
            }
        }

        public void UnloadPlugin(string name, bool skipGC = false, bool reloadDynamicApiPluginRouteEndpoint = false) {
            name = name.RemoveIllegalFileName();

            this._logger.LogInformation("[PLUGIN] Removing 💉 {name}", name);

            lock (LOADED_PLUGIN) {
                if (this._partManager != null) {
                    ApplicationPart part = this._partManager.ApplicationParts
                        .FirstOrDefault(p => p.Name.ToUpper() == name.ToUpper());

                    // AssemblyPart part = this._partManager.ApplicationParts
                    //     .OfType<AssemblyPart>()
                    //     .FirstOrDefault(p => p.Assembly == LOADED_PLUGIN[name].Item2);

                    if (part != null) {
                        _ = this._partManager.ApplicationParts.Remove(part);
                    }

                    string applicationParts = string.Join(", ", this._partManager.ApplicationParts.Select(p => p.Name));
                    this._logger.LogInformation("[PLUGIN] Remaining ApplicationParts 💉 {applicationParts}", applicationParts);
                }

                if (this._pluginServiceProviders.TryRemove(name, out IServiceProvider serviceProvider)) {
                    if (serviceProvider is IDisposable disposable) {
                        disposable.Dispose();
                    }
                }

                this._logger.LogInformation("[PLUGIN] Dependency Injection Service Removed 💉 {name}", name);

                if (this._pluginInstances.TryRemove(name, out IPlugin plugin)) {
                    if (plugin is IDisposable d) {
                        d.Dispose();
                    }
                }

                this._logger.LogInformation("[PLUGIN] Instance Removed 💉 {name}", name);

                if (LOADED_PLUGIN.TryRemove(name, out (CPluginLoadContext context, Assembly asm, FileStream fs, string tempPath) entry)) {
                    entry.context.Unload();
                    entry.fs.Dispose();

                    if (File.Exists(entry.tempPath)) {
                        File.Delete(entry.tempPath);
                    }

                    this._logger.LogInformation("[PLUGIN] All Dependencies & Temporary File Removed 💉 {name}", name);
                }

                if (!skipGC) {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                if (reloadDynamicApiPluginRouteEndpoint) {
                    CDynamicActionDescriptorChangeProvider.Instance.NotifyChanges();
                }

                this._logger.LogInformation("[PLUGIN] Application Removed 💉 {name}", name);
            }
        }

        public void UnloadAll(bool skipGC = false) {
            foreach (string key in LOADED_PLUGIN.Keys.ToArray()) {
                try {
                    this.UnloadPlugin(key, skipGC);
                }
                catch {
                    this._logger.LogError("[PLUGIN] Failed to Unload 💉 {key}", key);
                }
            }

            CDynamicActionDescriptorChangeProvider.Instance.NotifyChanges();
        }

        public IServiceProvider GetServiceProvider(string name) {
            name = name.RemoveIllegalFileName();
            return this._pluginServiceProviders[name];
        }

        private IEnumerable<Type> SafeGetTypes(Assembly asm) {
            try {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex) {
                return ex.Types.Where(t => t != null)!;
            }
        }

        private bool TryGetPluginType(Assembly asm, out Type type) {
            type = asm.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
            return type != null;
        }

        private IEnumerable<Type> GetPluginControllers(Assembly asm) {
            return asm.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);
        }

        private static bool TryCopyWithRetry(string sourcePath, string destinationPath, int maxRetries = 5, int delayMs = 200) {
            for (int i = 0; i < maxRetries; i++) {
                try {
                    string tempFolderPath = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(tempFolderPath)) {
                        _ = Directory.CreateDirectory(tempFolderPath);
                    }

                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    return true;
                }
                catch (IOException) when (i < maxRetries - 1) {
                    Thread.Sleep(delayMs);
                }
            }

            return false;
        }

        public bool IsPluginLoaded(string name) {
            name = name.RemoveIllegalFileName();
            return LOADED_PLUGIN.ContainsKey(name);
        }

        public List<CPluginInfoAttribute> GetLoadedPluginInfos() {
            return LOADED_PLUGIN.Select(kvp => {
                Assembly asm = kvp.Value.Item2;
                Type pluginType = asm.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                CPluginInfoAttribute attr = pluginType?.GetCustomAttribute<CPluginInfoAttribute>();

                if (pluginType == null) {
                    return null;
                }

                return attr;
            }).Where(x => x != null).ToList();
        }

    }

}
