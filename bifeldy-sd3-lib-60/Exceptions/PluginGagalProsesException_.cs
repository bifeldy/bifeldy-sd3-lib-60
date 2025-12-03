/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Custom Exception
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

namespace bifeldy_sd3_lib_60.Exceptions {

    public sealed class PluginGagalProsesException : Exception {

        public PluginGagalProsesException() { }

        public PluginGagalProsesException(string message) : base(message) { }

        public PluginGagalProsesException(string message, Exception inner) : base(message, inner) { }

    }

}
