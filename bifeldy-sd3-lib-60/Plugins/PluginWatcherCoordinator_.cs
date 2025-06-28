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

    public class PluginWatcherCoordinator : IDisposable {

        private string _dataFolderName { get; }

        public PluginContext Context { get; }

        private readonly BlockingCollection<string> _pluginQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly HashSet<string> _pendingSet = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public PluginWatcherCoordinator(string dataFolderName, PluginContext pluginContext) {
            this._dataFolderName = dataFolderName;
            this.Context = pluginContext;
            //
            this.Context.FileWatcher.RegisterHandler(dataFolderName, this.QueuePluginForReload);
            _ = Task.Factory.StartNew(this.ProcessQueue, this._cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void QueuePluginForReload(string pluginName) {
            lock (this._lock) {
                if (this._pendingSet.Add(pluginName)) {
                    this._pluginQueue.Add(pluginName);
                }
            }
        }

        private void ProcessQueue() {
            foreach (string pluginName in this._pluginQueue.GetConsumingEnumerable(this._cts.Token)) {
                try {
                    this.Context.Manager.UnloadPlugin(pluginName);
                    this.Context.Manager.LoadPlugin(pluginName);
                }
                catch (Exception ex) {
                    this.Context.Logger.LogError(ex, $"[PluginWatcher] Failed To Reload 💉 {pluginName}");
                }
                finally {
                    lock (this._lock) {
                        _ = this._pendingSet.Remove(pluginName);
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
