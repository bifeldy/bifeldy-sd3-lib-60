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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using Microsoft.Extensions.Logging;

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

        private readonly ILogger<CApplicationService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _hca;
        private readonly ILockerService _locker;
        private readonly IConverterService _converter;

        private readonly Assembly _prgAsm = Assembly.GetEntryAssembly();
        private readonly Assembly _libAsm = Assembly.GetExecutingAssembly();

        public DateTime? BuildTime => this._prgAsm.GetLinkerBuildTime() ?? this._libAsm.GetLinkerBuildTime();

        public bool DebugMode {
            get {
                #if DEBUG
                    return true;
                #else
                    return false;
                #endif
            }
        }

        public string AppName => Bifeldy.App.Environment.ApplicationName;
        public string AppLocation => AppDomain.CurrentDomain.BaseDirectory;
        public string AppVersion => this._prgAsm.GetName().Version.ToString();

        private readonly SettingLibb.Class1 _SettingLibb;

        public CApplicationService(
            ILogger<CApplicationService> logger,
            IDistributedCache cache,
            IHttpContextAccessor hca,
            ILockerService locker,
            IConverterService converter
        ) {
            this._logger = logger;
            this._cache = cache;
            this._hca = hca;
            this._locker = locker;
            this._converter = converter;
            this._SettingLibb = new SettingLibb.Class1();
        }

        public string GetVariabel(string key, string kunci) {
            string cacheKey = $"{kunci}_{key}".ToLower().Trim();

            try {
                _ = this._locker.SemaphoreGlobalApp("KUNCI").Wait(-1);

                string result = this._cache.GetString(cacheKey);
                if (!string.IsNullOrEmpty(result?.Trim())) {
                    return result;
                }

                // http://xxx.xxx.xxx.xxx/KunciGxxx
                result = this._SettingLibb.GetVariabel(key, kunci);
                result = result?.Split(';').FirstOrDefault();
                result = result?.Trim();

                if (!string.IsNullOrEmpty(result)) {
                    string jsonPathKunci = Path.Combine(this.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "Kunci.json");

                    if (result.ToUpper().Contains("ERROR") || result.ToUpper().Contains("EXCEPTION") || result.ToUpper().Contains("GAGAL") || result.ToUpper().Contains("NGINX")) {
                        bool fromSavedJsonFile = false;

                        if (File.Exists(jsonPathKunci)) {
                            try {
                                string jsonContent = File.ReadAllText(jsonPathKunci);

                                IDictionary<string, object> dictKunci = this._converter.JsonToObject<Dictionary<string, object>>(jsonContent);

                                if (dictKunci != null) {
                                    if (dictKunci.ContainsKey(cacheKey)) {
                                        object val = dictKunci[cacheKey];

                                        if (val != null) {
                                            result = val?.ToString();

                                            result = result?.Split(';').FirstOrDefault();
                                            result = result?.Trim();

                                            fromSavedJsonFile = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex) {
                                this._logger.LogError("[KUNCI_ERR_JSON_ERR] {ex}", ex.Message);
                                File.Delete(jsonPathKunci);
                            }
                        }

                        if (!fromSavedJsonFile) {
                            if (this._hca.HttpContext != null) {
                                string reqPath = this._hca.HttpContext.Request.Path.Value;
                                if (!string.IsNullOrEmpty(reqPath)) {
                                    if (reqPath.StartsWith($"/{Bifeldy.API_PREFIX}/", StringComparison.InvariantCultureIgnoreCase)) {
                                        return null;
                                    }
                                }
                            }

                            throw new KunciServerTidakTersediaException($"Terjadi Kesalahan Saat Mendapatkan Kunci {key} @ {kunci} :: {result}");
                        }
                    }
                    else {
                        var dictKunci = new Dictionary<string, object>();

                        if (File.Exists(jsonPathKunci)) {
                            string jsonContent = File.ReadAllText(jsonPathKunci);

                            try {
                                dictKunci = this._converter.JsonToObject<Dictionary<string, object>>(jsonContent);
                            }
                            catch (Exception ex) {
                                this._logger.LogError("[KUNCI_OK_JSON_ERR] {ex}", ex.Message);
                                File.Delete(jsonPathKunci);
                            }
                        }

                        if (!dictKunci.ContainsKey(cacheKey)) {
                            dictKunci.Add(cacheKey, result);
                        }

                        if (result != dictKunci[cacheKey]?.ToString()) {
                            dictKunci[cacheKey] = result;
                        }

                        File.WriteAllText(jsonPathKunci, this._converter.ObjectToJson(dictKunci));
                    }

                    if (!string.IsNullOrEmpty(result)) {
                        this._cache.SetString(cacheKey, result, new DistributedCacheEntryOptions() {
                            SlidingExpiration = TimeSpan.FromMinutes(15)
                        });
                    }
                }

                return result;
            }
            catch (Exception e) {
                this._logger.LogError("[KUNCI_ERROR] {ex}", e.Message);
                this._cache.Remove(cacheKey);
                throw;
            }
            finally {
                _ = this._locker.SemaphoreGlobalApp("KUNCI").Release();
            }
        }

        public CIpMacAddress[] GetIpMacAddress() {
            var IpMacAddress = new List<CIpMacAddress>();

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics) {
                if (nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
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

                    PhysicalAddress mac = nic.GetPhysicalAddress();
                    string macAddr = mac?.ToString();

                    IpMacAddress.Add(new CIpMacAddress() {
                        NAME = nic.Name,
                        DESCRIPTION = nic.Description,
                        MAC_ADDRESS = string.IsNullOrEmpty(macAddr) ? null : macAddr,
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
