/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Default Service
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IGlobalService {
        string BackupFolderPath { get; set; }
        string TempFolderPath { get; set; }
        string DownloadFolderPath { get; set; }
        string CsvFolderPath { get; set; }
        string ZipFolderPath { get; set; }
        SortedDictionary<string, string> SignalrClients { get; }
        List<string> AllowedIpOrigin { get; set; }
        string GetSecretData(HttpRequest request, RequestJson reqBody);
        string GetApiKeyData(HttpRequest request, RequestJson reqBody);
        string GetIpOriginData(ConnectionInfo connection, HttpRequest request, bool ipOnly = false, bool removeReverseProxyRoute = false);
        string CleanIpOrigin(string ipOrigins);
        string GetTokenData(HttpRequest request, RequestJson reqBody);
        Task<(string, string)> ParseHttpRequestBodyJsonString(HttpRequest request);
        Task<T> GetHttpRequestBody<T>(HttpRequest request);
        bool IsAllowedRoutingTarget(Type hideType, string kodeDc, EJenisDc jenisDc, bool isSwaggerApiDocs = false);
        Task CheckDownloadUpdate(string apiUpdaterUrl, Dictionary<string, object> HashFileFromServer);
    }

    [SingletonServiceRegistration]
    public sealed class CGlobalService : IGlobalService {

        private readonly EnvVar _envVar;

        private readonly ILogger<CGlobalService> _logger;
        private readonly IApplicationService _as;
        private readonly IConverterService _cs;
        private readonly IChiperService _chiper;
        private readonly IHostApplicationLifetime _host;
        private readonly IHttpService _http;

        public string BackupFolderPath { get; set; }
        public string TempFolderPath { get; set; }
        public string DownloadFolderPath { get; set; }
        public string CsvFolderPath { get; set; }
        public string ZipFolderPath { get; set; }

        public SortedDictionary<string, string> SignalrClients { get; } = new();

        public List<string> AllowedIpOrigin { get; set; } = new List<string>() {
            "localhost", "127.0.0.1"
        };

        public CGlobalService(
            IOptions<EnvVar> envVar,
            ILogger<CGlobalService> logger,
            IApplicationService @as,
            IConverterService cs,
            IChiperService chiper,
            IHostApplicationLifetime host,
            IHttpService http
        ) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;
            this._cs = cs;
            this._chiper = chiper;
            this._host = host;
            this._http = http;

            // --

            this.BackupFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.BACKUP_FOLDER_PATH);
            _ = Directory.CreateDirectory(this.BackupFolderPath);

            this.TempFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.TEMP_FOLDER_PATH);
            _ = Directory.CreateDirectory(this.TempFolderPath);

            this.DownloadFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.DOWNLOAD_FOLDER_PATH);
            _ = Directory.CreateDirectory(this.DownloadFolderPath);

            this.CsvFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.CSV_FOLDER_PATH);
            _ = Directory.CreateDirectory(this.CsvFolderPath);

            this.ZipFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.ZIP_FOLDER_PATH);
            _ = Directory.CreateDirectory(this.ZipFolderPath);
        }

        public string GetSecretData(HttpRequest request, RequestJson reqBody) {
            string secret = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers["x-secret"])) {
                secret = request.Headers["x-secret"];
            }
            else if (!string.IsNullOrEmpty(request.Query["secret"])) {
                secret = request.Query["secret"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.secret)) {
                secret = reqBody.secret;
            }

            return secret;
        }

        public string GetApiKeyData(HttpRequest request, RequestJson reqBody) {
            string apiKey = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers["x-api-key"])) {
                apiKey = request.Headers["x-api-key"];
            }
            else if (!string.IsNullOrEmpty(request.Query["key"])) {
                apiKey = request.Query["key"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.key)) {
                apiKey = reqBody.key;
            }

            return apiKey;
        }

        public string GetIpOriginData(ConnectionInfo connection, HttpRequest request, bool ipOnly = false, bool removeReverseProxyRoute = false) {
            string ipOrigin = connection?.RemoteIpAddress?.ToString();

            if (request != null) {
                if (!string.IsNullOrEmpty(request.Headers["cf-connecting-ip"])) {
                    ipOrigin = request.Headers["cf-connecting-ip"];
                }
                else if (!string.IsNullOrEmpty(request.Headers["x-forwarded-for"])) {
                    ipOrigin = request.Headers["x-forwarded-for"];
                }
                else if (!string.IsNullOrEmpty(request.Headers["x-real-ip"])) {
                    ipOrigin = request.Headers["x-real-ip"];
                }

                if (!ipOnly) {
                    if (!string.IsNullOrEmpty(request.Headers["origin"])) {
                        ipOrigin = request.Headers["origin"];
                    }
                    else if (!string.IsNullOrEmpty(request.Headers["referer"])) {
                        ipOrigin = request.Headers["referer"];
                    }
                }
            }

            string resultIpOrigin = this.CleanIpOrigin(ipOrigin);
            return removeReverseProxyRoute ? resultIpOrigin.Split(",").Select(rio => rio?.Trim()).FirstOrDefault() : resultIpOrigin;
        }

        public string CleanIpOrigin(string ipOrigins) {
            return string.Join(", ", ipOrigins.Split(",").Select(io => {
                string ipOrigin = io?.Trim() ?? string.Empty;

                // Remove Prefixes
                if (ipOrigin.StartsWith("::ffff:")) {
                    ipOrigin = ipOrigin[7..];
                }

                if (ipOrigin.StartsWith("http://")) {
                    ipOrigin = ipOrigin[7..];
                }
                else if (ipOrigin.StartsWith("https://")) {
                    ipOrigin = ipOrigin[8..];
                }

                if (ipOrigin.StartsWith("www.")) {
                    ipOrigin = ipOrigin[4..];
                }

                // Get Domain Or IP Maybe With Port Included And Remove Folder Path
                ipOrigin = ipOrigin.Split("/")[0];

                // Remove Port
                int totalColon = 0;
                for (int i = 0; i < ipOrigin.Length; i++) {
                    if (ipOrigin[i] == ':') {
                        totalColon++;
                    }

                    if (totalColon > 1) {
                        break;
                    }
                }

                if (totalColon == 1) {
                    // IPv4
                    ipOrigin = ipOrigin.Split(":")[0];
                }
                else {
                    // IPv6
                    ipOrigin = ipOrigin.Split("]")[0];
                    if (ipOrigin.StartsWith("[")) {
                        ipOrigin = ipOrigin[1..];
                    }
                }

                return ipOrigin;
            }));
        }

        public string GetTokenData(HttpRequest request, RequestJson reqBody) {
            string token = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers.Authorization)) {
                token = request.Headers.Authorization;
            }
            else if (!string.IsNullOrEmpty(request.Headers["x-access-token"])) {
                token = request.Headers["x-access-token"];
            }
            else if (!string.IsNullOrEmpty(request.Query["token"])) {
                token = request.Query["token"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.token)) {
                token = reqBody.token;
            }

            return token;
        }

        public async Task<(string, string)> ParseHttpRequestBodyJsonString(HttpRequest request) {
            string contentType = request.ContentType ?? request.Headers["content-type"].ToString();

            string rbString = null;
            if (contentType == "application/grpc" || SwaggerMediaTypesOperationFilter.AcceptedContentType.Contains(contentType)) {
                try {
                    rbString = await request.GetHttpRequestBodyStringAsync();
                }
                catch {
                    // Bukan Text
                }
            }

            return (contentType, rbString);
        }

        public async Task<T> GetHttpRequestBody<T>(HttpRequest request) {
            T reqBody = default;

            if (typeof(RequestJson).IsAssignableFrom(typeof(T))) {
                (string contentType, string rbString) = await this.ParseHttpRequestBodyJsonString(request);

                if (!string.IsNullOrEmpty(rbString)) {
                    try {
                        reqBody = this._cs.XmlJsonToObject<T>(contentType, rbString);
                    }
                    catch (Exception ex) {
                        this._logger.LogError("[JSON_BODY] 🌸 {ex}", ex.Message);
                    }
                }
            }

            return reqBody;
        }

        public bool IsAllowedRoutingTarget(Type hideType, string kodeDc, EJenisDc jenisDc, bool isSwaggerApiDocs = false) {
            bool isVisibleAllowed = true;

            if (isSwaggerApiDocs) {
                if (
                    (hideType == typeof(ApiHideDcHoAttribute) && kodeDc == "DCHO") ||
                    (hideType == typeof(ApiHideKonsolidasiCbnAttribute) && kodeDc == "KCBN") ||
                    (hideType == typeof(ApiHideWhHoAttribute) && kodeDc == "WHHO") ||
                    (hideType == typeof(ApiHideAllDcAttribute) && kodeDc != "DCHO" && kodeDc != "KCBN" && kodeDc != "WHHO")
                ) {
                    isVisibleAllowed = false;
                }
            }

            if (
                (hideType == typeof(DenyAccessIndukAttribute) && jenisDc == EJenisDc.INDUK) ||
                (hideType == typeof(DenyAccessDepoAttribute) && jenisDc == EJenisDc.DEPO) ||
                (hideType == typeof(DenyAccessKonvinienceAttribute) && jenisDc == EJenisDc.KONVINIENCE) ||
                (hideType == typeof(DenyAccessIplazaAttribute) && jenisDc == EJenisDc.IPLAZA) ||
                (hideType == typeof(DenyAccessFrozenAttribute) && jenisDc == EJenisDc.FROZEN) ||
                (hideType == typeof(DenyAccessPerishableAttribute) && jenisDc == EJenisDc.PERISHABLE) ||
                (hideType == typeof(DenyAccessLpgAttribute) && jenisDc == EJenisDc.LPG) ||
                (hideType == typeof(DenyAccessSewaAttribute) && jenisDc == EJenisDc.SEWA)
            ) {
                isVisibleAllowed = false;
            }

            return isVisibleAllowed;
        }



        public async Task CheckDownloadUpdate(string apiUpdaterUrl, Dictionary<string, object> HashFileFromServer) {
            string updaterFolder = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "updater");
            if (Directory.Exists(updaterFolder)) {
                Directory.Delete(updaterFolder, true);
            }

            _ = Directory.CreateDirectory(updaterFolder);

            bool needUpdate = false;
            foreach (KeyValuePair<string, object> hashFile in HashFileFromServer) {
                string remoteFileName = hashFile.Key;
                string remoteFileHash = hashFile.Value.ToString();

                string localFilePath = Path.Combine(this._as.AppLocation, remoteFileName);
                string localFileHash = File.Exists(localFilePath) ? this._chiper.CalculateCRC32File(localFilePath) : null;

                if (localFileHash != remoteFileHash) {
                    var uriBuilder = new UriBuilder(apiUpdaterUrl);
                    NameValueCollection queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
                    queryParams["fileName"] = remoteFileName;
                    uriBuilder.Query = queryParams.ToString();

                    HttpResponseMessage fileResponse = await this._http.GetData(uriBuilder.ToString(), timeoutSeconds: 10, maxRetry: 3);
                    if (!fileResponse.IsSuccessStatusCode) {
                        throw new Exception($"Gagal download {remoteFileName}");
                    }

                    string downloadedFilePath = Path.Combine(updaterFolder, remoteFileName);
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(downloadedFilePath)!);
                    await File.WriteAllBytesAsync(downloadedFilePath, await fileResponse.Content.ReadAsByteArrayAsync());

                    needUpdate = true;
                }
            }

            if (!needUpdate) {
                return;
            }

            string logPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "updater.log");
            int pid = Process.GetCurrentProcess().Id;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                string scriptPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "updater.sh");
                string scriptContent = $@"
#!/bin/bash
updaterFolder='{updaterFolder}'
appLocation='{this._as.AppLocation.TrimEnd('\\', '/')}'

copied=false
tries=0
while [ $tries -lt 10 ]; do
    cp -r ""$updaterFolder""/* ""$appLocation"" 2>""{logPath}"" && copied=true && break
    tries=$((tries+1))
    sleep 2
done

if [ ""$copied"" = true ]; then
    rm -rf ""$updaterFolder""
else
    echo ""Failed to copy after $tries attempts"" >> ""{logPath}""
fi

# self-delete
rm -- ""$0""
                                ";

                File.WriteAllText(scriptPath, scriptContent.Replace("\r\n", "\n"), new UTF8Encoding(false));

                var chmod = Process.Start(new ProcessStartInfo("chmod", $"+x \"{scriptPath}\"") {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                await chmod.WaitForExitAsync();

                var proc = Process.Start(new ProcessStartInfo("/bin/bash", $"-c \"nohup '{scriptPath}' >/dev/null 2>&1 & disown\"") {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                await proc.WaitForExitAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                bool isIIS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPAUTH")) ||
                             !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_PORT"));

                string scriptPath = Path.Combine(Path.GetTempPath(), $"updater_{pid}.cmd");
                string appOffline = Path.Combine(this._as.AppLocation, "app_offline.htm");

                string scriptContent;
                if (isIIS) {
                    scriptContent = $@"
@echo off
echo Updating... > ""{appOffline}""

:waitproc
tasklist /fi ""PID eq {pid}"" | findstr {pid} >nul
if %errorlevel%==0 (timeout /t 1 /nobreak >nul & goto waitproc)

set copied=false
set /a tries=0

:retrycopy
set /a tries+=1
xcopy ""{updaterFolder}\*"" ""{this._as.AppLocation}"" /Y /E /I > ""{logPath}"" 2>&1
if %errorlevel% LEQ 1 (set copied=true & goto copydone)

if %tries% GEQ 10 goto copyfail
timeout /t 2 /nobreak >nul
goto retrycopy

:copydone
rmdir /s /q ""{updaterFolder}""
del /f /q ""{appOffline}""
del /f /q ""%~f0""
exit /b 0

:copyfail
echo Failed to copy after %tries% attempts >> ""{logPath}""
del /f /q ""{appOffline}""
del /f /q ""%~f0""
exit /b 1
                                    ";
                }
                else {
                    scriptContent = $@"
@echo off
:waitproc
tasklist /fi ""PID eq {pid}"" | findstr {pid} >nul
if %errorlevel%==0 (timeout /t 1 /nobreak >nul & goto waitproc)

xcopy ""{updaterFolder}\*"" ""{this._as.AppLocation}"" /Y /E /I > ""{logPath}"" 2>&1
rmdir /s /q ""{updaterFolder}""
del /f /q ""%~f0""
exit /b 0
";
                }

                File.WriteAllText(scriptPath, scriptContent, new UTF8Encoding(false));

                var proc = Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{scriptPath}\"") {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                proc.EnableRaisingEvents = true;
                proc.Exited += (s, e) => {
                    File.Delete(scriptPath);
                };
            }
            else {
                throw new Exception("Platform not supported for auto update");
            }

            this._host.StopApplication();
        }

    }

}
