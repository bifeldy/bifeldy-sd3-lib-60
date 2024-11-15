/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Model STD IN/OUT Terminal SSH
* 
*/

namespace bifeldy_sd3_lib_60.Models {

    public sealed class CScriptOutputLine {

        public string Line { get; private set; }
        public bool IsErrorLine { get; private set; }

        public CScriptOutputLine(string line, bool isErrorLine) {
            this.Line = line;
            this.IsErrorLine = isErrorLine;
        }

    }

}
