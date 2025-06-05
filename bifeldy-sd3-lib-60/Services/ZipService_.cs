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

using Ionic.Zip;
using Ionic.Zlib;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Services {

    public interface IZipService {
        int ZipListFileInFolder(string zipFileName, string folderPath, List<string> listFileName = null, string password = null, string outputFolderPath = null);
        int ZipAllFileInFolder(string zipFileName, string folderPath, string password = null, string outputFolderPath = null);
    }

    [SingletonServiceRegistration]
    public sealed class CZipService : IZipService {

        private readonly ILogger<CZipService> _logger;
        private readonly IGlobalService _gs;

        public CZipService(ILogger<CZipService> logger, IGlobalService gs) {
            this._logger = logger;
            this._gs = gs;
        }

        public int ZipListFileInFolder(string zipFileName, string folderPath, List<string> listFileForZip = null, string password = null, string outputFolderPath = null) {
            int totalFileInZip = 0;

            try {
                string tempPath = Path.Combine(outputFolderPath ?? this._gs.TempFolderPath, zipFileName);
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }

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

                zip.Save(tempPath);

                string realPath = Path.Combine(outputFolderPath ?? this._gs.ZipFolderPath, zipFileName);
                if (File.Exists(realPath)) {
                    File.Delete(realPath);
                }

                File.Move(tempPath, $"{realPath}.tmp", true);
                File.Move($"{realPath}.tmp", realPath, true);

                this._logger.LogInformation("[ZIP_LIST_FILE_IN_FOLDER] {path}", realPath);
            }
            catch (Exception ex) {
                this._logger.LogError("[ZIP_LIST_FILE_IN_FOLDER] {ex}", ex.Message);
            }

            return totalFileInZip;
        }

        public int ZipAllFileInFolder(string zipFileName, string folderPath, string password = null, string outputFolderPath = null) {
            int totalFileInZip = 0;

            try {
                string tempPath = Path.Combine(outputFolderPath ?? this._gs.TempFolderPath, zipFileName);
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }

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

                zip.Save(tempPath);

                string realPath = Path.Combine(outputFolderPath ?? this._gs.ZipFolderPath, zipFileName);
                if (File.Exists(realPath)) {
                    File.Delete(realPath);
                }

                File.Move(tempPath, $"{realPath}.tmp", true);
                File.Move($"{realPath}.tmp", realPath, true);

                this._logger.LogInformation("[ZIP_ALL_FILE_IN_FOLDER] {path}", realPath);
            }
            catch (Exception ex) {
                this._logger.LogError("[ZIP_ALL_FILE_IN_FOLDER] {ex}", ex.Message);
            }

            return totalFileInZip;
        }

    }

}
