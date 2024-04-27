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

        public static byte[] ToByte(this string hex, string separator = null) {
            byte[] array;
            if (string.IsNullOrEmpty(separator)) {
                int length = (hex.Length + 1) / 3;
                array = new byte[length];
                for (int i = 0; i < length; i++) {
                    array[i] = Convert.ToByte(hex.Substring(3 * i, 2), 16);
                }
            }
            else {
                string[] arr = hex.Split('-');
                array = new byte[arr.Length];
                for (int i = 0; i < arr.Length; i++) {
                    array[i] = Convert.ToByte(arr[i], 16);
                }
            }

            return array;
        }

    }

}
