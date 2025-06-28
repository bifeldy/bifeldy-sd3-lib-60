/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Notifikasi Perubahan
* 
*/

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace bifeldy_sd3_lib_60.Plugins {

    public sealed class CDynamicActionDescriptorChangeProvider : IActionDescriptorChangeProvider {

        public static readonly CDynamicActionDescriptorChangeProvider Instance = new();
        private CancellationTokenSource _cts = new();

        public IChangeToken GetChangeToken() => new CancellationChangeToken(_cts.Token);

        public void NotifyChanges() {
            CancellationTokenSource prev = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            prev.Cancel();
        }

    }

}
