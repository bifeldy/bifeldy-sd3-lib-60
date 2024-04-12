/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: RDLC Report Model
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using DinkToPdf;

namespace bifeldy_sd3_lib_60.Models {

    public sealed class RdlcReport {
        public byte[] Report { get; set; }
        public string HtmlContent { get; set; }
        public string RenderType { get; set; }
        public string DisplayName { get; set; }
        public PaperKind? PaperType { get; set; } = PaperKind.Custom;
        public MarginSettings Margins { get; set; } = new MarginSettings() { Top = 1, Bottom = 1, Left = 1, Right = 1, Unit = Unit.Centimeters };
        public Orientation? PageOrientation { get; set; } = Orientation.Portrait;
    }

}
