/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Menolak API Di DC Tertentu
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    public class ApiRouteExcludeDcHoAttribute : Attribute {
        //
    }

    public class ApiRouteExcludeAllDcAttribute : Attribute {
        //
    }

    public class ApiRouteExcludeIndukAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

    public class ApiRouteExcludeDepoAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

    public class ApiRouteExcludeKonvinienceAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

    public class ApiRouteExcludeIplazaAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

    public class ApiRouteExcludeFrozenAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

    public class ApiRouteExcludePerishableAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

    public class ApiRouteExcludeLpgAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

    public class ApiRouteExcludeSewaAttribute : ApiRouteExcludeAllDcAttribute {
        //
    }

}
