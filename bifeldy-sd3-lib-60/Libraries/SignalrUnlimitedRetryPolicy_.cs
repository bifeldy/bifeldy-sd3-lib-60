/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Nimpa Retry Policy Milik SignalR
* 
*/

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace bifeldy_sd3_lib_60.Libraries {

    public sealed class SignalrUnlimitedRetryPolicy : IRetryPolicy {

        private readonly ILogger _logger;

        public SignalrUnlimitedRetryPolicy(ILogger logger) {
            this._logger = logger;
        }

        TimeSpan? IRetryPolicy.NextRetryDelay(RetryContext retryContext) {
            int delayMs = new Random().Next(0, 5) * 1000;

            this._logger.LogInformation("[SIGNALR_UNLIMITED_RETRY_POLICY] Retrying in {delayMs} with totals {count}x already ...", delayMs, retryContext.PreviousRetryCount);

            return TimeSpan.FromMilliseconds(delayMs);
        }

    }

}
