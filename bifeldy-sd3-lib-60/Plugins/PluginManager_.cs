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
using System.Reflection;

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class PluginManager {

        private readonly string _pluginDir;
        private ApplicationPartManager _partManager;
        private readonly ConcurrentDictionary<string, (PluginLoadContext, Assembly)> _loaded = new();

        public PluginManager(string pluginDir) {
            this._pluginDir = pluginDir;
            this.WatchPlugins();
        }

        public void SetPartManager(ApplicationPartManager partManager) {
            this._partManager = partManager;
        }

        public void LoadPlugin(string name) {
            string unsafePath = Path.Combine(this._pluginDir, $"{name}.dll");
            string fullPath = Path.GetFullPath(unsafePath);
            string rootPath = Path.GetFullPath(this._pluginDir);

            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)) {
                Console.WriteLine($"[Security] Blocked plugin load outside root: {fullPath}");
                return;
            }

            if (!File.Exists(fullPath) || this._partManager == null) {
                return;
            }

            DateTime lastWrite = File.GetLastWriteTimeUtc(fullPath);
            if (this._loaded.TryGetValue(name, out (PluginLoadContext context, Assembly asm) oldEntry)) {
                DateTime oldWrite = File.GetLastWriteTimeUtc(oldEntry.asm.Location);
                if (lastWrite == oldWrite) {
                    return;
                }
            }

            this.UnloadPlugin(name);

            var plc = new PluginLoadContext(fullPath);
            Assembly asm = plc.LoadFromAssemblyPath(fullPath);
            this._loaded[name] = (plc, asm);

            if (this.TryGetPluginType(asm, out Type type)) {
                var plugin = (IPlugin)Activator.CreateInstance(type)!;
                plugin.RegisterServices(Bifeldy.Services);
            }

            this._partManager.ApplicationParts.Add(new AssemblyPart(asm));
            DynamicActionDescriptorChangeProvider.Instance.NotifyChanges();
        }

        public void UnloadPlugin(string name) {
            if (!this._loaded.TryRemove(name, out (PluginLoadContext context, Assembly asm) entry) || this._partManager == null) {
                return;
            }

            AssemblyPart part = this._partManager.ApplicationParts
                .OfType<AssemblyPart>()
                .FirstOrDefault(p => p.Assembly == entry.asm);

            if (part != null) {
                _ = this._partManager.ApplicationParts.Remove(part);
            }

            entry.context.Unload();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            DynamicActionDescriptorChangeProvider.Instance.NotifyChanges();
        }

        private void WatchPlugins() {
            var watcher = new FileSystemWatcher(this._pluginDir, "*.dll") {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            watcher.Changed += (s, e) => this.ReloadPlugin(Path.GetFileNameWithoutExtension(e.Name!));
            watcher.Created += (s, e) => this.ReloadPlugin(Path.GetFileNameWithoutExtension(e.Name!));
            watcher.Deleted += (s, e) => this.UnloadPlugin(Path.GetFileNameWithoutExtension(e.Name!));
            watcher.Renamed += (s, e) => this.ReloadPlugin(Path.GetFileNameWithoutExtension(e.Name!));
            watcher.EnableRaisingEvents = true;
        }

        private void ReloadPlugin(string name) {
            _ = Task.Delay(200).ContinueWith(_ => this.LoadPlugin(name));
        }

        private bool TryGetPluginType(Assembly asm, out Type type) {
            type = asm.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract)!;
            return type != null;
        }

    }

}
