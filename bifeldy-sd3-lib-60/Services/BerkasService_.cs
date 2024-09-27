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

using System.Globalization;

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
        void CleanUp();
        void CopyAllFilesAndDirectories(DirectoryInfo source, DirectoryInfo target, bool isInRecursive = false);
        void BackupAllFilesInFolder(string folderPath);
        bool CheckSign(FileInfo fileInfo, string signFull, bool isRequired = true);
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
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;
            this._csv = csv;
            this._zip = zip;

            this.BackupFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.BACKUP_FOLDER_PATH);
            if (!Directory.Exists(this.BackupFolderPath)) {
                _ = Directory.CreateDirectory(this.BackupFolderPath);
            }

            this.TempFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.TEMP_FOLDER_PATH);
            if (!Directory.Exists(this.TempFolderPath)) {
                _ = Directory.CreateDirectory(this.TempFolderPath);
            }

            this.DownloadFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.DOWNLOAD_FOLDER_PATH);
            if (!Directory.Exists(this.DownloadFolderPath)) {
                _ = Directory.CreateDirectory(this.DownloadFolderPath);
            }
        }

        public void DeleteSingleFileInFolder(string fileName, string folderPath = null) {
            string path = folderPath ?? this.TempFolderPath;
            try {
                var fi = new FileInfo(Path.Combine(path, fileName));
                if (fi.Exists) {
                    fi.Delete();
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[BERKAS_DELETE_SINGLE_FILE_IN_FOLDER] {ex.Message}", ex.Message);
            }
        }

        public void DeleteOldFilesInFolder(string folderPath, int maxOldDays, bool isInRecursive = false) {
            string path = folderPath ?? this.TempFolderPath;
            if (!isInRecursive && path == this.TempFolderPath) {
                this.BackupAllFilesInFolder(this.TempFolderPath);
            }

            try {
                if (Directory.Exists(path)) {
                    var di = new DirectoryInfo(path);
                    FileSystemInfo[] fsis = di.GetFileSystemInfos();
                    foreach (FileSystemInfo fsi in fsis) {
                        if (fsi.Attributes == FileAttributes.Directory) {
                            this.DeleteOldFilesInFolder(fsi.FullName, maxOldDays, true);
                        }

                        if (fsi.LastWriteTime <= DateTime.Now.AddDays(-maxOldDays)) {
                            this._logger.LogInformation("[BERKAS_DELETE_OLD_FILE_IN_FOLDER] {FullName}", fsi.FullName);
                            fsi.Delete();
                        }
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[BERKAS_DELETE_OLD_FILE_IN_FOLDER] {ex}", ex.Message);
            }
        }

        public void CleanUp() {
            this.DeleteOldFilesInFolder(Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "logs"), this._envVar.MAX_RETENTIONS_DAYS);
            this.DeleteOldFilesInFolder(this.BackupFolderPath, this._envVar.MAX_RETENTIONS_DAYS);
            this.DeleteOldFilesInFolder(this.TempFolderPath, this._envVar.MAX_RETENTIONS_DAYS);
            this.DeleteOldFilesInFolder(this.DownloadFolderPath, this._envVar.MAX_RETENTIONS_DAYS);
            this.DeleteOldFilesInFolder(this._csv.CsvFolderPath, this._envVar.MAX_RETENTIONS_DAYS);
            this.DeleteOldFilesInFolder(this._zip.ZipFolderPath, this._envVar.MAX_RETENTIONS_DAYS);
        }

        public void CopyAllFilesAndDirectories(DirectoryInfo source, DirectoryInfo target, bool isInRecursive = false) {
            _ = Directory.CreateDirectory(target.FullName);
            foreach (FileInfo fi in source.GetFiles()) {
                FileInfo res = fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                this._logger.LogInformation("[BERKAS_COPY_ALL_FILES_AND_DIRECTORIES] {FullName} => {FullName}", fi.FullName, res.FullName);
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                this.CopyAllFilesAndDirectories(diSourceSubDir, nextTargetSubDir, true);
            }
        }

        public void BackupAllFilesInFolder(string folderPath) {
            var diSource = new DirectoryInfo(folderPath);
            var diTarget = new DirectoryInfo(this.BackupFolderPath);
            this.CopyAllFilesAndDirectories(diSource, diTarget);
        }

        public bool CheckSign(FileInfo fileInfo, string signFull, bool isRequired = true) {
            if (isRequired && string.IsNullOrEmpty(signFull)) {
                throw new Exception("Tidak ada tanda tangan file");
            }
            else if (!isRequired && string.IsNullOrEmpty(signFull)) {
                return true;
            }

            string[] signSplit = signFull.Split(' ');
            int minFileSize = signSplit.Length;
            if (fileInfo.Length < minFileSize) {
                throw new Exception("Isi konten file tidak sesuai");
            }

            int[] intList = new int[minFileSize];
            for (int i = 0; i < intList.Length; i++) {
                if (signSplit[i] == "??") {
                    intList[i] = -1;
                }
                else {
                    intList[i] = int.Parse(signSplit[i], NumberStyles.HexNumber);
                }
            }

            using (var reader = new BinaryReader(new FileStream(fileInfo.FullName, FileMode.Open))) {
                byte[] buff = new byte[minFileSize];
                _ = reader.BaseStream.Seek(0, SeekOrigin.Begin);
                _ = reader.Read(buff, 0, buff.Length);
                for (int i = 0; i < intList.Length; i++) {
                    if (intList[i] == -1 || buff[i] == intList[i]) {
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

    }

}
