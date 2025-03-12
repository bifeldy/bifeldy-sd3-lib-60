/**
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

using System.Net.NetworkInformation;
using System.Reflection;

using Microsoft.Extensions.Hosting;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IApplicationService {
        DateTime? BuildTime { get; }
        bool DebugMode { get; }
        string AppName { get; }
        string AppLocation { get; }
        string AppVersion { get; }
        string GetVariabel(string key, string kunci);
        CIpMacAddress[] GetIpMacAddress();
        string[] GetAllIpAddress();
        string[] GetAllMacAddress();
    }

    [SingletonServiceRegistration]
    public sealed class CApplicationService : IApplicationService {

        private readonly Assembly _prgAsm = Assembly.GetEntryAssembly();
        private readonly Assembly _libAsm = Assembly.GetExecutingAssembly();

        public DateTime? BuildTime => this._prgAsm.GetLinkerBuildTime() ?? this._libAsm.GetLinkerBuildTime();
        public bool DebugMode => Bifeldy.App.Environment.IsDevelopment();
        public string AppName => Bifeldy.App.Environment.ApplicationName;
        public string AppLocation => AppDomain.CurrentDomain.BaseDirectory;
        public string AppVersion => string.Join("", this._prgAsm.GetName().Version.ToString().Split('.'));

        private readonly SettingLibb.Class1 _SettingLibb;

        public CApplicationService() {
            this._SettingLibb = new SettingLibb.Class1();
        }

        public string GetVariabel(string key, string kunci) {
            try {
                // http://xxx.xxx.xxx.xxx/KunciGxxx
                string result = this._SettingLibb.GetVariabel(key, kunci);
                if (result.ToUpper().Contains("ERROR")) {
                    throw new Exception($"Gagal Mengambil Kunci {key} @ {kunci} :: {result}");
                }

                if (!string.IsNullOrEmpty(result)) {
                    result = result.Split(';').FirstOrDefault();
                }

                return result;
            }
            catch {
                return null;
            }
        }

        public CIpMacAddress[] GetIpMacAddress() {
            var IpMacAddress = new List<CIpMacAddress>();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics) {
                if (nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                    PhysicalAddress mac = nic.GetPhysicalAddress();
                    string iv4 = null;
                    string iv6 = null;
                    IPInterfaceProperties ipInterface = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation ua in ipInterface.UnicastAddresses) {
                        if (!ua.Address.IsIPv4MappedToIPv6 && !ua.Address.IsIPv6LinkLocal && !ua.Address.IsIPv6Teredo && !ua.Address.IsIPv6SiteLocal) {
                            if (ua.PrefixLength <= 32) {
                                iv4 = ua.Address.ToString();
                            }
                            else if (ua.PrefixLength <= 64) {
                                iv6 = ua.Address.ToString();
                            }
                        }
                    }

                    IpMacAddress.Add(new CIpMacAddress() {
                        NAME = nic.Name,
                        DESCRIPTION = nic.Description,
                        MAC_ADDRESS = string.IsNullOrEmpty(mac.ToString()) ? null : mac.ToString(),
                        IP_V4_ADDRESS = iv4,
                        IP_V6_ADDRESS = iv6
                    });
                }
            }

            return IpMacAddress.ToArray();
        }

        public string[] GetAllIpAddress() {
            string[] iv4 = this.GetIpMacAddress().Where(d => !string.IsNullOrEmpty(d.IP_V4_ADDRESS)).Select(d => d.IP_V4_ADDRESS.ToUpper()).ToArray();
            string[] iv6 = this.GetIpMacAddress().Where(d => !string.IsNullOrEmpty(d.IP_V6_ADDRESS)).Select(d => d.IP_V6_ADDRESS.ToUpper()).ToArray();
            string[] ip = new string[iv4.Length + iv6.Length];
            iv4.CopyTo(ip, 0);
            iv6.CopyTo(ip, iv4.Length);
            return ip;
        }

        public string[] GetAllMacAddress() => this.GetIpMacAddress().Where(d => !string.IsNullOrEmpty(d.MAC_ADDRESS)).Select(d => d.MAC_ADDRESS.ToUpper()).ToArray();

    }

}
