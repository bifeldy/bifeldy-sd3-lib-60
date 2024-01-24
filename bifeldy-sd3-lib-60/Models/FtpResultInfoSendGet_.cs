/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Model Hasil Up/Down FTP
* 
*/

using FluentFTP;

namespace bifeldy_sd3_lib_60.Models {

    public sealed class CFtpResultInfo {
        public List<CFtpResultSendGet> Success { get; } = new List<CFtpResultSendGet>();
        public List<CFtpResultSendGet> Fail { get; } = new List<CFtpResultSendGet>();
    }

    public sealed class CFtpResultSendGet {
        public bool FtpStatusSendGet { get; set; }
        public FileInfo FileInformation { get; set; }
    }

}
