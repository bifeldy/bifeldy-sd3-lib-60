/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.Extensions {

    public static class TimeSpanExtensions {

        public static string ToEtaString(this TimeSpan ts) {
            return ts.Days != 0 ? $"{ts.Days} days"
                 : ts.Hours != 0 ? $"{ts.Hours} hours"
                 : ts.Minutes != 0 ? $"{ts.Minutes} minutes"
                 : ts.Seconds != 0 ? $"{ts.Seconds} seconds"
                 : $"{ts.Milliseconds} milli-seconds";
        }

    }

}
