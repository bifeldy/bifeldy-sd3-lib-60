﻿/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Pengaturan Aplikasi
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Diagnostics;

using Microsoft.Extensions.Hosting;

namespace bifeldy_sd3_lib_60.Services {

    public interface IApplicationService {
        bool DebugMode { get; }
        string AppName { get; }
        string AppLocation { get; }
        string AppVersion { get; }
        string GetVariabel(string key, string kunci);
    }

    public sealed class CApplicationService : IApplicationService {

        public bool DebugMode => Bifeldy.App.Environment.IsDevelopment();
        public string AppName => Bifeldy.App.Environment.ApplicationName;
        public string AppLocation => AppDomain.CurrentDomain.BaseDirectory;
        public string AppVersion => string.Join("", Process.GetCurrentProcess().MainModule.FileVersionInfo.FileVersion.Split('.'));

        private readonly SettingLibb.Class1 _SettingLibb;

        public CApplicationService() {
            _SettingLibb = new SettingLibb.Class1();
        }

        public string GetVariabel(string key, string kunci) {
            try {
                // http://xxx.xxx.xxx.xxx/KunciGxxx
                string result = _SettingLibb.GetVariabel(key, kunci);
                if (result.ToUpper().Contains("ERROR")) {
                    throw new Exception("SettingLibb Gagal");
                }
                return result;
            }
            catch {
                return null;
            }
        }

    }

}
