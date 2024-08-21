/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Menyembunyikan Data API Di Swagger
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    public class SwaggerHideJsonPropertyAttribute : Attribute {
        //
    }

    public class SwaggerHideHoAttribute : Attribute {
        //
    }

    public class SwaggerHideDcAttribute : Attribute {
        //
    }

    public class SwaggerHideIndukAttribute : SwaggerHideDcAttribute {
        //
    }

    public class SwaggerHideDepoAttribute : SwaggerHideDcAttribute {
        //
    }

    public class SwaggerHideKonvinienceAttribute : SwaggerHideDcAttribute {
        //
    }

    public class SwaggerHideIplazaAttribute : SwaggerHideDcAttribute {
        //
    }

    public class SwaggerHideFrozenAttribute : SwaggerHideDcAttribute {
        //
    }

    public class SwaggerHidePerishableAttribute : SwaggerHideDcAttribute {
        //
    }

    public class SwaggerHideLpgAttribute : SwaggerHideDcAttribute {
        //
    }

    public class SwaggerHideSewaAttribute : SwaggerHideDcAttribute {
        //
    }

}
