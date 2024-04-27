/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Thread Safe Inter-Locking
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.Services {

    public interface ILockerService {
        SemaphoreSlim MutexGlobalApp { get; }
        SemaphoreSlim SemaphoreGlobalApp(string name, int initialCount = 1, int maximumCount = 1);
    }

    public sealed class CLockerService : ILockerService {

        private readonly IDictionary<string, SemaphoreSlim> semaphore_global_app = new Dictionary<string, SemaphoreSlim>();

        public CLockerService() {
            this.MutexGlobalApp = new SemaphoreSlim(1, 1);
        }

        public SemaphoreSlim MutexGlobalApp { get; } = null;

        public SemaphoreSlim SemaphoreGlobalApp(string name, int initialCount = 1, int maximumCount = 1) {
            if (!this.semaphore_global_app.ContainsKey(name)) {
                this.semaphore_global_app.Add(name, new SemaphoreSlim(initialCount, maximumCount));
            }

            return this.semaphore_global_app[name];
        }

    }

}
