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

    public class RouteExcludeDcHoAttribute : Attribute {
        //
    }

    public class RouteExcludeAllDcAttribute : Attribute {
        //
    }

    public class RouteExcludeIndukAttribute : RouteExcludeAllDcAttribute {
        //
    }

    public class RouteExcludeDepoAttribute : RouteExcludeAllDcAttribute {
        //
    }

    public class RouteExcludeKonvinienceAttribute : RouteExcludeAllDcAttribute {
        //
    }

    public class RouteExcludeIplazaAttribute : RouteExcludeAllDcAttribute {
        //
    }

    public class RouteExcludeFrozenAttribute : RouteExcludeAllDcAttribute {
        //
    }

    public class RouteExcludePerishableAttribute : RouteExcludeAllDcAttribute {
        //
    }

    public class RouteExcludeLpgAttribute : RouteExcludeAllDcAttribute {
        //
    }

    public class RouteExcludeSewaAttribute : RouteExcludeAllDcAttribute {
        //
    }

}
