/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: ApiHide Hanya Menyembunyikan Dari Halaman Dokumentasi
 *              :: DenyAccess Menolak API Request Tertentu (Sudah Termasuk Disembunyikan)
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    public abstract class RouteExcludeAttribute : Attribute {
        //
    }

    /* ** */

    public class ApiHideKonsolidasiCbnAttribute : RouteExcludeAttribute {
        //
    }

    public class ApiHideDcHoAttribute : RouteExcludeAttribute {
        //
    }

    public class ApiHideWhHoAttribute : RouteExcludeAttribute {
        //
    }

    public class ApiHideAllDcAttribute : RouteExcludeAttribute {
        //
    }

    /* ** */

    public class DenyAccessIndukAttribute : ApiHideAllDcAttribute {
        //
    }

    public class DenyAccessDepoAttribute : ApiHideAllDcAttribute {
        //
    }

    public class DenyAccessKonvinienceAttribute : ApiHideAllDcAttribute {
        //
    }

    public class DenyAccessIplazaAttribute : ApiHideAllDcAttribute {
        //
    }

    public class DenyAccessFrozenAttribute : ApiHideAllDcAttribute {
        //
    }

    public class DenyAccessPerishableAttribute : ApiHideAllDcAttribute {
        //
    }

    public class DenyAccessLpgAttribute : ApiHideAllDcAttribute {
        //
    }

    public class DenyAccessSewaAttribute : ApiHideAllDcAttribute {
        //
    }

}
