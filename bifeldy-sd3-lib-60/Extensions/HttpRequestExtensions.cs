

using Microsoft.AspNetCore.Http;



using System.Text;

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

    public static class HttpRequestExtensions {

        public static async Task<string> GetRequestBodyStringAsync(this HttpRequest request, Encoding encoding = null) {
            string body = string.Empty;

            request.EnableBuffering();
            if (request.ContentLength == null || !(request.ContentLength > 0) || !request.Body.CanSeek) {
                return body;
            }

            _ = request.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(request.Body, encoding ?? Encoding.Default, true, 1024, true)) {
                body = await reader.ReadToEndAsync();
            }

            request.Body.Position = 0;

            return body;
        }

    }

}
