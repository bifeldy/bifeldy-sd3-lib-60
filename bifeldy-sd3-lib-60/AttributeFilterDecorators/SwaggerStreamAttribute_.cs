/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Menginformasikan Cara Stream Di Swagger
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    public enum SwaggerStreamFormat {
        Ndjson,
        JsonArray
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SwaggerStreamAttribute : Attribute {

        public SwaggerStreamFormat Format { get; }

        public SwaggerStreamAttribute(SwaggerStreamFormat format) {
            this.Format = format;
        }

    }

}
