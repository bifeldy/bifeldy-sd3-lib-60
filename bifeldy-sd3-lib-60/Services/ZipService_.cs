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
        int ZipListFileInFolder(string zipFileName, string folderPath, List<string> listFileName = null, string password = null, string outputPath = null);
        int ZipAllFileInFolder(string zipFileName, string folderPath, string password = null, string outputPath = null);
    }

    public sealed class CZipService : IZipService {

        private readonly EnvVar _envVar;
        private readonly ILogger<CZipService> _logger;

        private readonly IApplicationService _as;

        public string ZipFolderPath { get; }

        public CZipService(IOptions<EnvVar> envVar, ILogger<CZipService> logger, IApplicationService @as) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;

            this.ZipFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.ZIP_FOLDER_PATH);
            if (!Directory.Exists(this.ZipFolderPath)) {
                _ = Directory.CreateDirectory(this.ZipFolderPath);
            }
        }

        public int ZipListFileInFolder(string zipFileName, string folderPath, List<string> listFileForZip = null, string password = null, string outputPath = null) {
            int totalFileInZip = 0;
            string path = Path.Combine(outputPath ?? this.ZipFolderPath, zipFileName);
            try {
                var zip = new ZipFile();
                if (!string.IsNullOrEmpty(password)) {
                    zip.Password = password;
                    zip.CompressionLevel = CompressionLevel.BestCompression;
                }

                foreach (string targetFileName in listFileForZip) {
                    string filePath = Path.Combine(folderPath, targetFileName);
                    ZipEntry zipEntry = zip.AddFile(filePath, "");
                    if (zipEntry != null) {
                        totalFileInZip++;
                    }

                    this._logger.LogInformation("[ZIP_LIST_FILE_IN_FOLDER] {status} @ {filePath}", zipEntry == null ? "Fail" : "Ok", filePath);
                }

                zip.Save(path);
                this._logger.LogInformation("[ZIP_LIST_FILE_IN_FOLDER] {path}", path);
            }
            catch (Exception ex) {
                this._logger.LogError("[ZIP_LIST_FILE_IN_FOLDER] {ex}", ex.Message);
            }

            return totalFileInZip;
        }

        public int ZipAllFileInFolder(string zipFileName, string folderPath, string password = null, string outputPath = null) {
            int totalFileInZip = 0;
            string path = Path.Combine(outputPath ?? this.ZipFolderPath, zipFileName);
            try {
                var zip = new ZipFile {
                    CompressionLevel = CompressionLevel.BestCompression
                };
                if (!string.IsNullOrEmpty(password)) {
                    zip.Password = password;
                    zip.Encryption = EncryptionAlgorithm.PkzipWeak;
                }

                var directoryInfo = new DirectoryInfo(folderPath);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
                foreach (FileInfo fileInfo in fileInfos) {
                    ZipEntry zipEntry = zip.AddFile(fileInfo.FullName, "");
                    if (zipEntry != null) {
                        totalFileInZip++;
                    }

                    this._logger.LogInformation("[ZIP_ALL_FILE_IN_FOLDER] {status} @ {FullName}", zipEntry == null ? "Fail" : "Ok", fileInfo.FullName);
                }

                zip.Save(path);
                this._logger.LogInformation("[ZIP_ALL_FILE_IN_FOLDER] {path}", path);
            }
            catch (Exception ex) {
                this._logger.LogError("[ZIP_ALL_FILE_IN_FOLDER] {ex}", ex.Message);
            }

            return totalFileInZip;
        }

    }

}
