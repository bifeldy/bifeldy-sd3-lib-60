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

namespace bifeldy_sd3_lib_60.Plugins {

    public static class PluginLoaderForSwagger {

        private static readonly Dictionary<string, DateTime> debounceMap = new();
        private static readonly object debounceLock = new();

        public static void LoadAllPlugins(PluginManager pluginManager, string pluginDir) {
            foreach (string dll in Directory.GetFiles(pluginDir, "*.dll", SearchOption.AllDirectories)) {
                string pluginName = Path.GetFileNameWithoutExtension(dll);
                pluginManager.LoadPlugin(pluginName);
            }
        }

        public static void WatchAndReload(PluginManager pluginManager, string pluginDir) {
            var watcher = new FileSystemWatcher(pluginDir, "*.dll") {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            void handler(object _, FileSystemEventArgs e) {
                lock (debounceLock) {
                    string key = e.FullPath;
                    DateTime now = DateTime.UtcNow;
                    if (!debounceMap.ContainsKey(key) || (now - debounceMap[key]).TotalMilliseconds > 300) {
                        debounceMap[key] = now;
                        TriggerReload(pluginManager, pluginDir);
                    }
                }
            }

            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Renamed += (_, e) => handler(_, new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath)));
            watcher.Deleted += handler;

            watcher.EnableRaisingEvents = true;
        }

        private static void TriggerReload(PluginManager pluginManager, string pluginDir) {
            Console.WriteLine("[PluginWatcher] Change detected, reloading for Swagger...");
            LoadAllPlugins(pluginManager, pluginDir);
        }

    }

}
