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
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IBerkasService {
        void DeleteSingleFileInFolder(string fileName, string folderPath = null);
        void DeleteOldFilesInFolder(string folderPath, int maxOldHours, bool isInRecursive = false);
        void CleanUp(bool clearWorkingFileDirectories = true, int? maxOldHours = null);
        void CopyAllFilesAndDirectories(DirectoryInfo source, DirectoryInfo target, bool isInRecursive = false);
        void BackupAllFilesInFolder(string folderPath);
        bool CheckSign(FileInfo fileInfo, string signFull, bool isRequired = true, Encoding encoding = null);
        List<string> ReadFileTextGetLastLines(string filePath, int numberOfLines);
    }

    [SingletonServiceRegistration]
    public sealed class CBerkasService : IBerkasService {

        private readonly EnvVar _envVar;
        private readonly ILogger<CBerkasService> _logger;

        private readonly IApplicationService _as;
        private readonly IGlobalService _gs;

        public CBerkasService(
            IOptions<EnvVar> envVar,
            ILogger<CBerkasService> logger,
            IApplicationService @as,
            IGlobalService gs
        ) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;
            this._gs = gs;
        }

        public void DeleteSingleFileInFolder(string fileName, string folderPath = null) {
            string path = folderPath ?? this._gs.TempFolderPath;
            try {
                var fi = new FileInfo(Path.Combine(path, fileName));
                if (fi.Exists) {
                    fi.Delete();
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[BERKAS_DELETE_SINGLE_FILE_IN_FOLDER] {ex}", ex.Message);
            }
        }

        public void DeleteOldFilesInFolder(string folderPath, int maxOldHours, bool isInRecursive = false) {
            string path = folderPath ?? this._gs.TempFolderPath;
            if (!isInRecursive && path == this._gs.TempFolderPath) {
                this.BackupAllFilesInFolder(this._gs.TempFolderPath);
            }

            if (Directory.Exists(path)) {
                var di = new DirectoryInfo(path);
                FileSystemInfo[] fsis = di.GetFileSystemInfos();
                foreach (FileSystemInfo fsi in fsis) {
                    if (fsi.Attributes == FileAttributes.Directory) {
                        this.DeleteOldFilesInFolder(fsi.FullName, maxOldHours, true);
                    }

                    if (fsi.LastWriteTime <= DateTime.Now.AddHours(-maxOldHours)) {
                        try {
                            fsi.Delete();
                            this._logger.LogInformation("[BERKAS_DELETE_OLD_FILE_IN_FOLDER] {FullName}", fsi.FullName);
                        }
                        catch (Exception ex) {
                            this._logger.LogError("[BERKAS_DELETE_OLD_FILE_IN_FOLDER] {ex}", ex.Message);
                        }
                    }
                }
            }
        }

        public void CleanUp(bool clearWorkingFileDirectories = true, int? maxOldHours = null) {
            int daysFromHours = this._envVar.MAX_RETENTIONS_DAYS * 24; // Pakainya Jam
            this.DeleteOldFilesInFolder(Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "logs"), daysFromHours);
            this.DeleteOldFilesInFolder(this._gs.BackupFolderPath, daysFromHours);
            if (clearWorkingFileDirectories) {
                this.DeleteOldFilesInFolder(this._gs.CsvFolderPath, maxOldHours ?? daysFromHours);
                this.DeleteOldFilesInFolder(this._gs.ZipFolderPath, maxOldHours ?? daysFromHours);
                this.DeleteOldFilesInFolder(this._gs.DownloadFolderPath, maxOldHours ?? daysFromHours);
                this.DeleteOldFilesInFolder(this._gs.TempFolderPath, maxOldHours ?? daysFromHours);
            }
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
            var diTarget = new DirectoryInfo(this._gs.BackupFolderPath);
            this.CopyAllFilesAndDirectories(diSource, diTarget);
        }

        public bool CheckSign(FileInfo fileInfo, string signFull, bool isRequired = true, Encoding encoding = null) {
            if (isRequired && string.IsNullOrEmpty(signFull)) {
                throw new Exception("Tidak Ada Tanda Tangan File");
            }
            else if (!isRequired && string.IsNullOrEmpty(signFull)) {
                return true;
            }

            string[] signSplit = signFull.Split(' ');
            int minFileSize = signSplit.Length;
            if (fileInfo.Length < minFileSize) {
                throw new Exception("Isi Konten File Tidak Sesuai");
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

            using (var reader = new BinaryReader(new FileStream(fileInfo.FullName, FileMode.Open), encoding ?? Encoding.UTF8)) {
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

        public List<string> ReadFileTextGetLastLines(string filePath, int numberOfLines) {
            var lastLines = new List<string>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                long position = fs.Length;
                var currentLine = new StringBuilder();

                while (position > 0 && lastLines.Count < numberOfLines) {
                    position--;

                    _ = fs.Seek(position, SeekOrigin.Begin);

                    int byteRead = fs.ReadByte();

                    if (byteRead == -1) {
                        break;
                    }

                    char character = (char)byteRead;

                    if (character == '\n') {
                        if (currentLine.Length > 0 && currentLine[^1] == '\r') {
                            _ = currentLine.Remove(currentLine.Length - 1, 1);
                        }

                        lastLines.Insert(0, currentLine.ToString());

                        _ = currentLine.Clear();
                    }
                    else {
                        _ = currentLine.Insert(0, character);
                    }
                }

                if (currentLine.Length > 0 && lastLines.Count < numberOfLines) {
                    lastLines.Insert(0, currentLine.ToString());
                }
            }

            return lastLines;
        }

    }

}
