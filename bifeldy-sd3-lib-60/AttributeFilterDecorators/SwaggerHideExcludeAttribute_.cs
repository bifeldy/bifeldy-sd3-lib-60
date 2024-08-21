/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Menyembunyikan Data / Menolak API Di Swagger
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    public class SwaggerHideJsonPropertyAttribute : Attribute {
        //
    }

    public class SwaggerExcludeHoAttribute : Attribute {
        //
    }

    public class SwaggerExcludeDcAttribute : Attribute {
        //
    }

    public class SwaggerExcludeIndukAttribute : SwaggerExcludeDcAttribute {
        //
    }

    public class SwaggerExcludeDepoAttribute : SwaggerExcludeDcAttribute {
        //
    }

    public class SwaggerExcludeKonvinienceAttribute : SwaggerExcludeDcAttribute {
        //
    }

    public class SwaggerExcludeIplazaAttribute : SwaggerExcludeDcAttribute {
        //
    }

    public class SwaggerExcludeFrozenAttribute : SwaggerExcludeDcAttribute {
        //
    }

    public class SwaggerExcludePerishableAttribute : SwaggerExcludeDcAttribute {
        //
    }

    public class SwaggerExcludeLpgAttribute : SwaggerExcludeDcAttribute {
        //
    }

    public class SwaggerExcludeSewaAttribute : SwaggerExcludeDcAttribute {
        //
    }

}
