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
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class CPluginManager {

        private readonly string _pluginDir;
        private readonly IServiceCollection _services;
        private readonly ILogger _logger;

        private ApplicationPartManager _partManager;

        public event Action<string> PluginReloaded;

        private readonly ConcurrentDictionary<string, IPlugin> _pluginInstances = new();
        private readonly ConcurrentDictionary<string, ServiceProvider> _pluginServiceProviders = new();
        private readonly ConcurrentDictionary<string, (CPluginLoadContext, Assembly)> _loaded = new();

        public CPluginManager(string pluginDir, IServiceCollection services, ILogger logger) {
            this._pluginDir = pluginDir;
            this._services = services;
            this._logger = logger;
        }

        public void SetPartManager(ApplicationPartManager partManager) {
            this._partManager = partManager;
        }

        public void LoadPlugin(string name) {
            name = name.RemoveIllegalFileName();

            this._logger.LogInformation("[PLUGIN] Loading 💉 {name}", name);

            try {
                string dllAsFolderName = Path.Combine(this._pluginDir, name);
                string mainDllPath = Path.Combine(dllAsFolderName, $"{name}.dll");

                if (!Directory.Exists(dllAsFolderName) || !File.Exists(mainDllPath) || this._partManager == null) {
                    throw new Exception($"[PLUGIN] DLL Not Found 💉 {mainDllPath}");
                }

                DateTime lastWrite = File.GetLastWriteTimeUtc(mainDllPath);
                if (this._loaded.TryGetValue(name, out (CPluginLoadContext context, Assembly asm) oldEntry)) {
                    DateTime oldWrite = File.GetLastWriteTimeUtc(oldEntry.asm.Location);
                    if (lastWrite == oldWrite) {
                        return;
                    }
                }

                this.UnloadPlugin(name);

                string tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Bifeldy.App.Environment.ApplicationName}_{name}_{Guid.NewGuid()}.dll"
                );

                if (!TryCopyWithRetry(mainDllPath, tempPath)) {
                    throw new Exception($"[PLUGIN] Failed To Copy Plugin DLL To Temp Folder 💉 {mainDllPath}");
                }

                lock (this._loaded) {
                    // Butuh Yang Asli Karena Barang Kali Ada External Lib.dll Yang 1 Folder Dengannya ~
                    var plc = new CPluginLoadContext(mainDllPath);
                    Assembly asm = plc.LoadFromAssemblyPath(tempPath);

                    this._loaded[name] = (plc, asm);

                    this._logger.LogInformation("[PLUGIN] Loaded All Required Dependencies 💉 {name}", name);

                    var newTypes = this.SafeGetTypes(asm)
                        .Where(t => !string.IsNullOrWhiteSpace(t.FullName))
                        .Select(t => t.FullName!)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var existingAssemblies = new List<Assembly> {
                        Assembly.GetExecutingAssembly(),
                        Assembly.GetEntryAssembly()
                    };

                    existingAssemblies.AddRange(this._loaded.Values.Select(v => v.Item2));

                    foreach (Assembly existingAsm in existingAssemblies) {
                        foreach (Type existingType in this.SafeGetTypes(existingAsm)) {
                            if (!string.IsNullOrWhiteSpace(existingType.FullName) && newTypes.Contains(existingType.FullName)) {
                                _ = duplicates.Add(existingType.FullName);
                                this._logger.LogError("Duplicate '{type}' Found 💉 '{pluginName}'", existingType.FullName, name);
                            }
                        }
                    }

                    var allowedDuplicatePatterns = new HashSet<string> {
                        "bifeldy_sd3_lib_60.*",
                        "bifeldy_sd3_mbz_60.*"
                    };

                    IEnumerable<Regex> allowedRegexes = allowedDuplicatePatterns.Select(pattern => {
                        return new Regex(
                            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
                            RegexOptions.IgnoreCase
                        );
                    });

                    IEnumerable<string> filteredDuplicates = duplicates.Where(typeName => !allowedRegexes.Any(rx => rx.IsMatch(typeName)));
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

                    var asmFileInfo = FileVersionInfo.GetVersionInfo(asm.Location);
                    string asmFileVersion = string.Join("", asmFileInfo.ProductVersion.Split('.'));
                    if (info.Name != asmFileInfo.ProductName || info.Version != asmFileVersion || string.IsNullOrEmpty(info.Author)) {
                        throw new Exception($"[PLUGIN] Wrong / Invalid Assembly Metadata 💉 {name}");
                    }

                    this._logger.LogInformation("[PLUGIN] Metadata 💉 Name: {Name}, Version: {Version}, Author: {Author}", info.Name, info.Version, info.Author);

                    var plugin = (IPlugin)Activator.CreateInstance(type)!;
                    this._pluginInstances[name] = plugin;

                    this._logger.LogInformation("[PLUGIN] Instance Created 💉 {name}", name);

                    var pluginServices = new ServiceCollection();
                    plugin.RegisterServices(pluginServices);

                    foreach (ServiceDescriptor descriptor in this._services) {
                        _ = pluginServices.Add(descriptor);
                    }

                    ServiceProvider pluginServiceProvider = pluginServices.BuildServiceProvider();
                    this._pluginServiceProviders[name] = pluginServiceProvider;

                    this._logger.LogInformation("[PLUGIN] Dependency Injection Service Registered 💉 {name}", name);

                    if (this._partManager != null) {
                        if (!this._partManager.ApplicationParts.Any(p => string.Equals(p.Name, asm.GetName().Name, StringComparison.OrdinalIgnoreCase))) {
                            if (asm.GetCustomAttributes(typeof(RazorCompiledItemAttribute), inherit: false).Any()) {
                                this._partManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(asm));
                            }
                            else {
                                this._partManager.ApplicationParts.Add(new AssemblyPart(asm));
                            }
                        }

                        this._logger.LogInformation("[PLUGIN] Application Ready 💉 {name}", name);
                    }

                    CDynamicActionDescriptorChangeProvider.Instance.NotifyChanges();
                    PluginReloaded?.Invoke(name);
                }
            }
            catch (Exception ex) {
                this._logger.LogError(ex,"[PLUGIN] Error 💉 {name}", name);
                this.UnloadPlugin(name);
                throw;
            }
        }

        public void UnloadPlugin(string name, bool skipGC = false) {
            name = name.RemoveIllegalFileName();

            this._logger.LogInformation("[PLUGIN] Removing 💉 {name}", name);

            lock (this._loaded) {
                if (this._partManager != null) {
                    // ApplicationPart part = this._partManager.ApplicationParts
                    //     .FirstOrDefault(p => string.Equals(p.Name, entry.asm.GetName().Name, StringComparison.OrdinalIgnoreCase));   

                    AssemblyPart part = this._partManager.ApplicationParts
                    .OfType<AssemblyPart>()
                    .FirstOrDefault(p => p.Assembly == this._loaded[name].Item2);

                    if (part != null) {
                        _ = this._partManager.ApplicationParts.Remove(part);
                    }

                    string applicationParts = string.Join(", ", this._partManager.ApplicationParts.Select(p => p.Name));
                    this._logger.LogInformation("[PLUGIN] Remaining ApplicationParts 💉 {applicationParts}", applicationParts);
                }

                if (this._pluginServiceProviders.TryRemove(name, out ServiceProvider provider)) {
                    if (provider is IDisposable disposable) {
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

                if (this._loaded.TryRemove(name, out (CPluginLoadContext context, Assembly asm) entry)) {
                    entry.context.Unload();

                    this._logger.LogInformation("[PLUGIN] All Dependencies Removed 💉 {name}", name);

                    string tempFile = entry.asm.Location;
                    if (File.Exists(tempFile)) {
                        File.Delete(tempFile);

                        this._logger.LogInformation("[PLUGIN] Temporary File Removed 💉 {name}", name);
                    }
                }

                if (!skipGC) {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                CDynamicActionDescriptorChangeProvider.Instance.NotifyChanges();
                PluginReloaded?.Invoke(name);

                this._logger.LogInformation("[PLUGIN] Application Removed 💉 {name}", name);
            }
        }

        public void UnloadAll(bool skipGC = false) {
            foreach (string key in this._loaded.Keys.ToArray()) {
                try {
                    this.UnloadPlugin(key, skipGC);
                }
                catch {
                    this._logger.LogError("[PLUGIN] Failed to Unload 💉 {key}", key);
                }
            }
        }

        public ServiceProvider GetServiceProvider(string name) {
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

        private static bool TryCopyWithRetry(string sourcePath, string destinationPath, int maxRetries = 5, int delayMs = 200) {
            for (int i = 0; i < maxRetries; i++) {
                try {
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
            return this._loaded.ContainsKey(name);
        }

        public List<CPluginInfo> GetLoadedPluginInfos() {
            return this._loaded.Select(kvp => {
                Assembly asm = kvp.Value.Item2;
                Type pluginType = asm.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                CPluginInfoAttribute attr = pluginType?.GetCustomAttribute<CPluginInfoAttribute>();

                if (pluginType == null) {
                    return null;
                }

                return new CPluginInfo {
                    Name = attr.Name,
                    Version = attr.Version,
                    Author = attr.Author
                };
            }).Where(x => x != null).ToList();
        }

    }

}
