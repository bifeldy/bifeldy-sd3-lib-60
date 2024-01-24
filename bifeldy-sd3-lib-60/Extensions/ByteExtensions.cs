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

        public static string ToStringHex(this byte[] bytes, bool removeHypens = true) {
            string hex = BitConverter.ToString(bytes);
            if (removeHypens) {
                return hex.Replace("-", "");
            }
            return hex;
        }

        public static IEnumerable<byte[]> Split(this byte[] value, int bufferLength) {
            int countOfArray = value.Length / bufferLength;
            if (value.Length % bufferLength > 0) {
                countOfArray++;
            }
            for (int i = 0; i < countOfArray; i++) {
                yield return value.Skip(i * bufferLength).Take(bufferLength).ToArray();
            }
        }

    }

}
