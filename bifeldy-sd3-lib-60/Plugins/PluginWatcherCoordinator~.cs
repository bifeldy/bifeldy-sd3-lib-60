/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Timer Nahan Perubahan File
* 
*/

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace bifeldy_sd3_lib_60.Plugins {

    public class CPluginWatcherCoordinator : IDisposable {

        private string _dataFolderName { get; }

        public CPluginContext Context { get; }

        private readonly BlockingCollection<string> _pluginQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly HashSet<string> _pendingSet = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public CPluginWatcherCoordinator(string dataFolderName, CPluginContext pluginContext) {
            this._dataFolderName = dataFolderName;
            this.Context = pluginContext;
            //
            this.Context.FileWatcher.RegisterHandler(dataFolderName, this.QueuePluginForReload);
            _ = Task.Factory.StartNew(this.ProcessQueue, this._cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void QueuePluginForReload(string pluginFilePath) {
            lock (this._lock) {
                if (this._pendingSet.Add(pluginFilePath)) {
                    this._pluginQueue.Add(pluginFilePath);
                }
            }
        }

        private void ProcessQueue() {
            foreach (string pluginFilePath in this._pluginQueue.GetConsumingEnumerable(this._cts.Token)) {
                if (!pluginFilePath.ToLower().EndsWith(".dll")) {
                    continue;
                }

                string pluginName = Path.GetFileNameWithoutExtension(pluginFilePath);

                try {
                    this.Context.Manager.LoadPlugin(pluginName);
                }
                catch (Exception ex) {
                    this.Context.Logger.LogError("[PluginWatcher] Failed To Reload '{pluginName}' 💉 {name}", pluginName, ex.Message);
                }
                finally {
                    lock (this._lock) {
                        _ = this._pendingSet.Remove(pluginFilePath);
                        this.Context.Manager.ReloadAllDynamicApiPluginRouteEndpoint();
                    }

                    Thread.Sleep(1500);
                }
            }
        }

        public void Dispose() {
            this._cts.Cancel();
            this._pluginQueue.CompleteAdding();
            this.Context.FileWatcher.UnregisterHandler(this._dataFolderName);
            this._cts.Dispose();
        }

    }

}
