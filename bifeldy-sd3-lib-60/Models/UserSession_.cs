using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bifeldy_sd3_lib_60.Models {

    public sealed class UserSession {
        public string Nik { get; set; }
        public string Role { get; set; } = "USER";
        public string Name { get; set; }
    }

}
