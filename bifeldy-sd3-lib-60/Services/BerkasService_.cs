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

using System.Data;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Ionic.Zip;
using Ionic.Zlib;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IBerkasService {
        string BackupFolderPath { get; }
        string TempFolderPath { get; }
        string ZipFolderPath { get; }
        string DownloadFolderPath { get; }
        List<string> ListFileForZip { get; }
        void CleanUp();
        void DeleteSingleFileInFolder(string fileName, string folderPath = null);
        void DeleteOldFilesInFolder(string folderPath, int maxOldDays, bool isInRecursive = false);
        bool DataTable2CSV(DataTable table, string filename, string separator, string outputFolderPath = null);
        int ZipListFileInFolder(string zipFileName, List<string> listFileName = null, string folderPath = null, string password = null);
        int ZipAllFileInFolder(string zipFileName, string folderPath = null, string password = null);
        void BackupAllFilesInTempFolder();
        void CopyAllFilesAndDirectories(DirectoryInfo source, DirectoryInfo target, bool isInRecursive = false);
        string GZipDecompressString(byte[] byteData, int maxChunk = 2048);
        byte[] GZipCompressString(string text);
        MemoryStream ReadFileAsBinaryByte(string filePath, int maxChunk = 2048);
    }

    public sealed class CBerkasService : IBerkasService {

        private readonly EnvVar _envVar;
        private readonly ILogger<CBerkasService> _logger;

        private readonly IApplicationService _as;

        public string BackupFolderPath { get; }
        public string TempFolderPath { get; }
        public string ZipFolderPath { get; }
        public string DownloadFolderPath { get; }

        public List<string> ListFileForZip { get; }

        public CBerkasService(IOptions<EnvVar> envVar, ILogger<CBerkasService> logger, IApplicationService @as) {
            _envVar = envVar.Value;
            _logger = logger;
            _as = @as;

            ListFileForZip = new List<string>();

            BackupFolderPath = Path.Combine(_as.AppLocation, "_data", _envVar.BACKUP_FOLDER_PATH);
            if (!Directory.Exists(BackupFolderPath)) {
                Directory.CreateDirectory(BackupFolderPath);
            }

            TempFolderPath = Path.Combine(_as.AppLocation, "_data", _envVar.TEMP_FOLDER_PATH);
            if (!Directory.Exists(TempFolderPath)) {
                Directory.CreateDirectory(TempFolderPath);
            }

            ZipFolderPath = Path.Combine(_as.AppLocation, "_data", _envVar.ZIP_FOLDER_PATH);
            if (!Directory.Exists(ZipFolderPath)) {
                Directory.CreateDirectory(ZipFolderPath);
            }

            DownloadFolderPath = Path.Combine(_as.AppLocation, "_data", _envVar.DOWNLOAD_FOLDER_PATH);
            if (!Directory.Exists(DownloadFolderPath)) {
                Directory.CreateDirectory(DownloadFolderPath);
            }
        }

        public void CleanUp() {
            DeleteOldFilesInFolder(Path.Combine(_as.AppLocation, "_data", "logs"), _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(Path.Combine(_as.AppLocation, "_data", "logs"), _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(BackupFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(TempFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            DeleteOldFilesInFolder(ZipFolderPath, _envVar.MAX_RETENTIONS_DAYS);
            ListFileForZip.Clear();
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
                BackupAllFilesInTempFolder();
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

        public bool DataTable2CSV(DataTable table, string filename, string separator, string outputFolderPath = null) {
            bool res = false;
            string path = Path.Combine(outputFolderPath ?? TempFolderPath, filename);
            StreamWriter writer = null;
            try {
                writer = new StreamWriter(path);
                string sep = string.Empty;
                StringBuilder builder = new StringBuilder();
                foreach (DataColumn col in table.Columns) {
                    builder.Append(sep).Append(col.ColumnName);
                    sep = separator;
                }
                // Untuk Export *.CSV Di Buat NAMA_KOLOM Besar Semua Tanpa Petik "NAMA_KOLOM"
                writer.WriteLine(builder.ToString().ToUpper());
                foreach (DataRow row in table.Rows) {
                    sep = string.Empty;
                    builder = new StringBuilder();
                    foreach (DataColumn col in table.Columns) {
                        builder.Append(sep).Append(row[col.ColumnName]);
                        sep = separator;
                    }
                    writer.WriteLine(builder.ToString());
                }
                _logger.LogInformation($"[BERKAS_DATA_TABLE_2_CSV] {path}");
                res = true;
            }
            catch (Exception ex) {
                _logger.LogError($"[BERKAS_DATA_TABLE_2_CSV] {ex.Message}");
            }
            finally {
                if (writer != null) {
                    writer.Close();
                }
            }
            return res;
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

        public void BackupAllFilesInTempFolder() {
            DirectoryInfo diSource = new DirectoryInfo(TempFolderPath);
            DirectoryInfo diTarget = new DirectoryInfo(BackupFolderPath);
            CopyAllFilesAndDirectories(diSource, diTarget);
        }

        public int ZipListFileInFolder(string zipFileName, List<string> listFileName = null, string folderPath = null, string password = null) {
            int totalFileInZip = 0;
            List<string> ls = listFileName ?? ListFileForZip;
            try {
                ZipFile zip = new ZipFile();
                if (!string.IsNullOrEmpty(password)) {
                    zip.Password = password;
                    zip.CompressionLevel = CompressionLevel.BestCompression;
                }
                foreach (string targetFileName in ls) {
                    string filePath = Path.Combine(folderPath ?? TempFolderPath, targetFileName);
                    ZipEntry zipEntry = zip.AddFile(filePath, "");
                    if (zipEntry != null) {
                        totalFileInZip++;
                    }
                    _logger.LogInformation($"[BERKAS_ZIP_LIST_FILE_IN_FOLDER] {(zipEntry == null ? "Fail" : "Ok")} @ {filePath}");
                }
                string outputPath = Path.Combine(ZipFolderPath, zipFileName);
                zip.Save(outputPath);
                _logger.LogInformation($"[BERKAS_ZIP_LIST_FILE_IN_FOLDER] {outputPath}");
            }
            catch (Exception ex) {
                _logger.LogError($"[BERKAS_ZIP_LIST_FILE_IN_FOLDER] {ex.Message}");
            }
            finally {
                if (listFileName == null) {
                    ls.Clear();
                }
            }
            return totalFileInZip;
        }

        public int ZipAllFileInFolder(string zipFileName, string folderPath = null, string password = null) {
            int totalFileInZip = 0;
            try {
                ZipFile zip = new ZipFile();
                if (!string.IsNullOrEmpty(password)) {
                    zip.Password = password;
                    zip.CompressionLevel = CompressionLevel.BestCompression;
                }
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath ?? TempFolderPath);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
                foreach (FileInfo fileInfo in fileInfos) {
                    ZipEntry zipEntry = zip.AddFile(fileInfo.FullName, "");
                    if (zipEntry != null) {
                        totalFileInZip++;
                    }
                    _logger.LogInformation($"[BERKAS_ZIP_ALL_FILE_IN_FOLDER] {(zipEntry == null ? "Fail" : "Ok")} @ {fileInfo.FullName}");
                }
                string outputPath = Path.Combine(ZipFolderPath, zipFileName);
                zip.Save(outputPath);
                _logger.LogInformation($"[BERKAS_ZIP_ALL_FILE_IN_FOLDER] {outputPath}");
            }
            catch (Exception ex) {
                _logger.LogError($"[BERKAS_ZIP_ALL_FILE_IN_FOLDER] {ex.Message}");
            }
            return totalFileInZip;
        }

        public string GZipDecompressString(byte[] byteData, int maxChunk = 2048) {
            byte[] buffer = new byte[maxChunk - 1];
            GZipStream stream = new GZipStream(new MemoryStream(byteData), CompressionMode.Decompress);
            MemoryStream memoryStream = new MemoryStream();
            int count = 0;
            do {
                count = stream.Read(buffer, 0, count);
                if (count > 0) {
                    memoryStream.Write(buffer, 0, count);
                }
            }
            while (count > 0);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        public byte[] GZipCompressString(string text) {
            byte[] tempByte = Encoding.UTF8.GetBytes(text);
            MemoryStream memory = new MemoryStream();
            GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true);
            gzip.Write(tempByte, 0, tempByte.Length);
            return memory.ToArray();
        }

        public MemoryStream ReadFileAsBinaryByte(string filePath, int maxChunk = 2048) {
            MemoryStream dest = new MemoryStream();
            using (Stream source = File.OpenRead(filePath)) {
                byte[] buffer = new byte[maxChunk];
                int bytesRead = 0;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0) {
                    dest.Write(buffer, 0, bytesRead);
                }
            }
            return dest;
        }

    }

}
