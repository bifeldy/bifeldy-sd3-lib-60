/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Struktur Model Kelas Tabel Database
* 
*/

using bifeldy_sd3_lib_60.Libraries;

namespace bifeldy_sd3_lib_60.Models {

    public sealed class CTableClassModel {
        public string table_name { get; set; }
        public List<CDynamicClassProperty> properties { get; set; }
    }

}
