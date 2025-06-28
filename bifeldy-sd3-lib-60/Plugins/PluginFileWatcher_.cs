/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Mengamati Perubahan File
* 
*/

using System.Collections.Concurrent;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class CPluginFileWatcher {

        private readonly string _pluginDir;
        private readonly FileSystemWatcher _watcher;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, Action<string>> _namedCallbacks = new();

        public CPluginFileWatcher(string pluginDir) {
            this._pluginDir = pluginDir;

            this._watcher = new FileSystemWatcher(this._pluginDir, "*") {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            this._watcher.Changed += this.OnChanged;
            this._watcher.Created += this.OnChanged;
            this._watcher.Deleted += this.OnChanged;
            this._watcher.Renamed += (s, e) => this.OnChanged(s, new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath)));

            this._watcher.EnableRaisingEvents = true;
        }

        public void RegisterHandler(string name, Action<string> callback) {
            this._namedCallbacks[name] = callback;
        }

        public void UnregisterHandler(string name) {
            _ = this._namedCallbacks.TryRemove(name, out _);
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            string pluginName = Path.GetFileNameWithoutExtension(e.Name!);
            lock (this._lock) {
                foreach (Action<string> callback in _namedCallbacks.Values) {
                    callback(pluginName);
                }
            }
        }

    }

}
