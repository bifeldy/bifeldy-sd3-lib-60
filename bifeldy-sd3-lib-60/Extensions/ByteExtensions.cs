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

    public static class ByteExtensions {

        public static string ToString(this byte[] bytes, bool removeHypens = true) {
            string hex = BitConverter.ToString(bytes);
            if (removeHypens) {
                return hex.Replace("-", "");
            }
            return hex;
        }

    }

}
