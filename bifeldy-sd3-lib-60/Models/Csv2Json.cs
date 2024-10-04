/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Model Convert CSV Ke JSON
* 
*/

namespace bifeldy_sd3_lib_60.Models {

    public sealed class CCsv2Json {
        public string ColumnName { get; set; }
        public Type DataType { get; set; }
        public int Position { get; set; } = 0;
    }

}
