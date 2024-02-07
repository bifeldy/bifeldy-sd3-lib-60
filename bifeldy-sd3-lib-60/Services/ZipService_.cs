/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Zip Files Manager
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Ionic.Zip;
using Ionic.Zlib;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IZipService {
        string ZipFolderPath { get; }
        List<string> ListFileForZip { get; }
        int ZipListFileInFolder(string zipFileName, string folderPath, List<string> listFileName = null, string password = null, string outputPath = null);
        int ZipAllFileInFolder(string zipFileName, string folderPath, string password = null, string outputPath = null);
    }

    public sealed class CZipService : IZipService {

        private readonly EnvVar _envVar;
        private readonly ILogger<CZipService> _logger;

        private readonly IApplicationService _as;

        public string ZipFolderPath { get; }

        public List<string> ListFileForZip { get; }

        public CZipService(IOptions<EnvVar> envVar, ILogger<CZipService> logger, IApplicationService @as) {
            _envVar = envVar.Value;
            _logger = logger;
            _as = @as;

            ListFileForZip = new List<string>();

            ZipFolderPath = Path.Combine(_as.AppLocation, "_data", _envVar.ZIP_FOLDER_PATH);
            if (!Directory.Exists(ZipFolderPath)) {
                Directory.CreateDirectory(ZipFolderPath);
            }
        }

        public int ZipListFileInFolder(string zipFileName, string folderPath, List<string> listFileName = null, string password = null, string outputPath = null) {
            int totalFileInZip = 0;
            List<string> ls = listFileName ?? ListFileForZip;
            string path = Path.Combine(outputPath ?? ZipFolderPath, zipFileName);
            try {
                ZipFile zip = new ZipFile();
                if (!string.IsNullOrEmpty(password)) {
                    zip.Password = password;
                    zip.CompressionLevel = CompressionLevel.BestCompression;
                }
                foreach (string targetFileName in ls) {
                    string filePath = Path.Combine(folderPath, targetFileName);
                    ZipEntry zipEntry = zip.AddFile(filePath, "");
                    if (zipEntry != null) {
                        totalFileInZip++;
                    }
                    _logger.LogInformation($"[ZIP_LIST_FILE_IN_FOLDER] {(zipEntry == null ? "Fail" : "Ok")} @ {filePath}");
                }
                zip.Save(path);
                _logger.LogInformation($"[ZIP_LIST_FILE_IN_FOLDER] {path}");
            }
            catch (Exception ex) {
                _logger.LogError($"[ZIP_LIST_FILE_IN_FOLDER] {ex.Message}");
            }
            finally {
                if (listFileName == null) {
                    ls.Clear();
                }
            }
            return totalFileInZip;
        }

        public int ZipAllFileInFolder(string zipFileName, string folderPath, string password = null, string outputPath = null) {
            int totalFileInZip = 0;
            string path = Path.Combine(outputPath ?? ZipFolderPath, zipFileName);
            try {
                ZipFile zip = new ZipFile();
                if (!string.IsNullOrEmpty(password)) {
                    zip.Password = password;
                    zip.CompressionLevel = CompressionLevel.BestCompression;
                }
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
                foreach (FileInfo fileInfo in fileInfos) {
                    ZipEntry zipEntry = zip.AddFile(fileInfo.FullName, "");
                    if (zipEntry != null) {
                        totalFileInZip++;
                    }
                    _logger.LogInformation($"[ZIP_ALL_FILE_IN_FOLDER] {(zipEntry == null ? "Fail" : "Ok")} @ {fileInfo.FullName}");
                }
                zip.Save(path);
                _logger.LogInformation($"[ZIP_ALL_FILE_IN_FOLDER] {path}");
            }
            catch (Exception ex) {
                _logger.LogError($"[ZIP_ALL_FILE_IN_FOLDER] {ex.Message}");
            }
            return totalFileInZip;
        }

    }

}
