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

    public static class DecimalExtensions {

        // Tidak bisa override `decimal` (karena tipenya `struct`)
        public static string ToString(this decimal value, bool removeTrail) {
            if (removeTrail) {
                value = value.RemoveTrail();
            }

            return value.ToString();
        }

        public static decimal RemoveTrail(this decimal value) {
            // https://learn.microsoft.com/en-us/dotnet/api/system.decimal.getbits
            return value / 1.000000000000000000000000000000000m;
        }

    }

}
