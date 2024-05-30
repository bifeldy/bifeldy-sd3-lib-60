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

    public static class StringExtensions {

        public static byte[] ParseHexTextToByte(this string hex, string separator = null) {
            byte[] array;
            if (string.IsNullOrEmpty(separator)) {
                int numberChars = hex.Length;
                array = new byte[numberChars / 2];
                for (int i = 0; i < numberChars; i += 2) {
                    array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
            }
            else {
                string[] arr = hex.Split(separator);
                array = new byte[arr.Length];
                for (int i = 0; i < arr.Length; i++) {
                    array[i] = Convert.ToByte(arr[i], 16);
                }
            }

            return array;
        }

    }

}
