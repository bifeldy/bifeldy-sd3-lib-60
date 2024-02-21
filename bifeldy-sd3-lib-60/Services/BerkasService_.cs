/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Mengatur Storage File & Folder
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IBerkasService {
        string BackupFolderPath { get; }
        string TempFolderPath { get; }
        string DownloadFolderPath { get; }
        void DeleteSingleFileInFolder(string fileName, string folderPath = null);
        void DeleteOldFilesInFolder(string folderPath, int maxOldDays, bool isInRecursive = false);
        void CleanUp(bool clearPendingFileForZip = true);
        void CopyAllFilesAndDirectories(DirectoryInfo source, DirectoryInfo target, bool isInRecursive = false);
        void BackupAllFilesInFolder(string folderPath);
    }

    public sealed class CBerkasService : IBerkasService {

        private readonly EnvVar _envVar;
        private readonly ILogger<CBerkasService> _logger;

        private readonly IApplicationService _as;
        private readonly ICsvService _csv;
        private readonly IZipService _zip;

        public string BackupFolderPath { get; }
        public string TempFolderPath { get; }
        public string DownloadFolderPath { get; }

        public CBerkasService(
            IOptions<EnvVar> envVar,
            ILogger<CBerkasService> logger,
            IApplicationService @as,
            ICsvService csv,
            IZipService zip
        ) {
            _envVar = envVar.Value;
            _logger = logger;
            _as = @as;
            _csv = csv;
            _zip = zip;

            BackupFolderPath = Path.Combine(_as.AppLocation, _envVar.DEFAULT_DATA_FOLDER, _envVar.BACKUP_FOLDER_PATH);
            if (!Directory.Exists(BackupFolderPath)) {
                Directory.CreateDirectory(BackupFolderPath);
            }

            TempFolderPath = Path.Combine(_as.AppLocation, _envVar.DEFAULT_DATA_FOLDER, _envVar.TEMP_FOLDER_PATH);
            if (!Directory.Exists(TempFolderPath)) {
                Directory.CreateDirectory(TempFolderPath);
            }

            DownloadFolderPath = Path.Combine(_as.AppLocation, _envVar.DEFAULT_DATA_FOLDER, _envVar.DOWNLOAD_FOLDER_PATH);
            if (!Directory.Exists(DownloadFolderPath)) {
                Directory.CreateDirectory(DownloadFolderPath);
            }
        }

        public void DeleteSingleFileInFolder(string fileName, string folderPath = null) {
            string path = folderPath ?? TempFolderPath;
            try {
                FileInfo fi = new FileInfo(Path.Combine(path, fileName));
                if (fi.Exists) {
                    fi.Delete();
                }
            }
            catch (Exception ex) {
                _logger.LogError($"[BERKAS_DELETE_SINGLE_FILE_IN_FOLDER] {ex.Message}");
            }
        }

        public void DeleteOldFilesInFolder(string folderPath, int maxOldDays, bool isInRecursive = false) {
            string path = folderPath ?? TempFolderPath;
            if (!isInRecursive && path == TempFolderPath) {
                BackupAllFilesInFolder(TempFolderPath);
            }
            try {
                if (Directory.Exists(path)) {
                    DirectoryInfo di = new DirectoryInfo(path);
                    FileSystemInfo[] fsis = di.GetFileSystemInfos();
                    foreach (FileSystemInfo fsi in fsis) {
                        if (fsi.Attributes == FileAttributes.Directory) {
                            DeleteOldFilesInFolder(fsi.FullName, maxOldDays, true);
                        }
                        if (fsi.LastWriteTime <= DateTime.Now.AddDays(-maxOldDays)) {
                            _logger.LogInformation($"[BERKAS_DELETE_OLD_FILE_IN_FOLDER] {fsi.FullName}");
                            fsi.Delete();
                        }
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError($"[BERKAS_DELETE_OLD_FILE_IN_FOLDER] {ex.Message}");
            }
        }

        public void CleanUp(bool clearPendingFileForZip = true) {
            DeleteOldFilesInFolder(Path.Combine(_as.AppLocation, "logs"), _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(BackupFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(TempFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(DownloadFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(_csv.CsvFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(_zip.ZipFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            if (clearPendingFileForZip) {
                _zip.ListFileForZip.Clear();
            }
        }

        public void CopyAllFilesAndDirectories(DirectoryInfo source, DirectoryInfo target, bool isInRecursive = false) {
            Directory.CreateDirectory(target.FullName);
            foreach (FileInfo fi in source.GetFiles()) {
                FileInfo res = fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                _logger.LogInformation($"[BERKAS_COPY_ALL_FILES_AND_DIRECTORIES] {fi.FullName} => {res.FullName}");
            }
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAllFilesAndDirectories(diSourceSubDir, nextTargetSubDir, true);
            }
        }

        public void BackupAllFilesInFolder(string folderPath) {
            DirectoryInfo diSource = new DirectoryInfo(folderPath);
            DirectoryInfo diTarget = new DirectoryInfo(BackupFolderPath);
            CopyAllFilesAndDirectories(diSource, diTarget);
        }

    }

}
