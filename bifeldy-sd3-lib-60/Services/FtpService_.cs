/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Kurir FTP
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Net;

using Microsoft.Extensions.Logging;

using FluentFTP;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IFtpService {
        Task<FtpClient> CreateFtpConnection(string ipDomainHost, int portNumber, string userName, string password, string remoteWorkDir);
        Task<CFtpResultInfo> SendFtpFiles(FtpClient ftpConnection, string localDirPath, string fileName = null, Action<double> progress = null);
        Task<CFtpResultInfo> GetFtpFileDir(FtpClient ftpConnection, string localDirFilePath, bool isDirectory = false, Action<double> progress = null);
        Task<CFtpResultInfo> CreateFtpConnectionAndSendFtpFiles(string ipDomainHost, int portNumber, string userName, string password, string remoteWorkDir, string localDirPath, string fileName = null, Action<double> progress = null);
        Task<CFtpResultInfo> CreateFtpConnectionAndGetFtpFileDir(string ipDomainHost, int portNumber, string userName, string password, string remoteWorkDir, string localDirFilePath, bool isDirectory = false, Action<double> progress = null);
    }

    [SingletonServiceRegistration]
    public sealed class CFtpService : IFtpService {

        private readonly ILogger<CFtpService> _logger;
        private readonly IApplicationService _as;
        private readonly IGlobalService _gs;

        public CFtpService(ILogger<CFtpService> logger, IApplicationService @as, IGlobalService gs) {
            this._logger = logger;
            this._as = @as;
            this._gs = gs;
        }

        public async Task<FtpClient> CreateFtpConnection(string ipDomainHost, int portNumber, string userName, string password, string remoteWorkDir) {
            FtpClient ftpClient;
            try {
                ftpClient = new FtpClient() {
                    Host = ipDomainHost,
                    Port = portNumber,
                    Credentials = new NetworkCredential(userName, password),
                    DataConnectionType = FtpDataConnectionType.PASV,
                    DownloadDataType = FtpDataType.Binary,
                    UploadDataType = FtpDataType.Binary
                };
                await ftpClient.ConnectAsync();
                await ftpClient.SetWorkingDirectoryAsync(remoteWorkDir);
            }
            catch (Exception ex) {
                this._logger.LogError("[FTP_CREATE_FTP_CONNECTION] {ex}", ex.Message);
                throw;
            }

            return ftpClient;
        }

        public async Task RenameFtpFiles(FtpClient ftpConnection, string originalFileName, string newFileName, bool deleteNewFileIfExists = false) {
            if (this._as.DebugMode) {
                originalFileName = $"_SIMULASI__{originalFileName}";
            }

            if (this._as.DebugMode) {
                newFileName = $"_SIMULASI__{newFileName}";
            }

            if (!ftpConnection.FileExists(originalFileName)) {
                throw new Exception($"File {originalFileName} Target Tidak DItemukan!");
            }

            if (ftpConnection.FileExists(newFileName) && deleteNewFileIfExists) {
                await ftpConnection.DeleteFileAsync(newFileName);
            }

            await ftpConnection.RenameAsync(originalFileName, newFileName);

            this._logger.LogInformation("[FTP_RENAME_FTP_FILES] {originalFileName} => {newFileName}", originalFileName, newFileName);

            if (ftpConnection.IsConnected) {
                await ftpConnection.DisconnectAsync();
            }
        }

        public async Task<CFtpResultInfo> SendFtpFiles(FtpClient ftpConnection, string localDirPath, string fileName = null, Action<double> progress = null) {
            var ftpResultInfo = new CFtpResultInfo();
            var directoryInfo = new DirectoryInfo(localDirPath);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            if (fileName != null) {
                fileInfos = fileInfos.Where(f => f.Name.Contains(fileName)).ToArray();
            }

            string cwd = await ftpConnection.GetWorkingDirectoryAsync();
            foreach (FileInfo fi in fileInfos) {
                string fileSent = "Fail";
                string fn = this._as.DebugMode ? $"_SIMULASI__{fi.Name}" : fi.Name;
                if (ftpConnection.FileExists(fn)) {
                    await ftpConnection.DeleteFileAsync(fn);
                }

                IProgress<FtpProgress> ftpProgress = new Progress<FtpProgress>(data => {
                    progress?.Invoke(data.Progress);
                });
                FtpStatus ftpStatus = await ftpConnection.UploadFileAsync(fi.FullName, fn, progress: ftpProgress);
                var resultSend = new CFtpResultSendGet() {
                    FtpStatusSendGet = ftpStatus == FtpStatus.Success,
                    FileInformation = fi
                };
                if (ftpStatus == FtpStatus.Success) {
                    fileSent = "Ok";
                    ftpResultInfo.Success.Add(resultSend);
                }
                else {
                    ftpResultInfo.Fail.Add(resultSend);
                }

                this._logger.LogInformation("[FTP_SEND_FTP_FILES] {fileSent} @ {cwd}/{fn}", fileSent, cwd, fn);
            }

            if (ftpConnection.IsConnected) {
                await ftpConnection.DisconnectAsync();
            }

            return ftpResultInfo;
        }

        public async Task<CFtpResultInfo> GetFtpFileDir(FtpClient ftpConnection, string localDirFilePath, bool isDirectory = false, Action<double> progress = null) {
            var ftpResultInfo = new CFtpResultInfo();
            string saveDownloadTo = Path.Combine(this._gs.DownloadFolderPath, localDirFilePath);
            IProgress<FtpProgress> ftpProgress = new Progress<FtpProgress>(data => {
                progress?.Invoke(data.Progress);
            });
            if (isDirectory) {
                List<FtpResult> ftpResult = await ftpConnection.DownloadDirectoryAsync(saveDownloadTo, localDirFilePath, FtpFolderSyncMode.Update, FtpLocalExists.Overwrite, progress: ftpProgress);
                foreach (FtpResult fr in ftpResult) {
                    string fileGet = "Fail";
                    var resultGet = new CFtpResultSendGet() {
                        FtpStatusSendGet = fr.IsSuccess,
                        FileInformation = new FileInfo(Path.Combine(saveDownloadTo, fr.Name))
                    };
                    if (fr.IsSuccess) {
                        fileGet = "Ok";
                        ftpResultInfo.Success.Add(resultGet);
                    }
                    else {
                        ftpResultInfo.Fail.Add(resultGet);
                    }

                    this._logger.LogInformation("[FTP_GET_FTP_FILE_DIR] {fileGet} @ {LocalPath}", fileGet, fr.LocalPath);
                }
            }
            else {
                string fileGet = "Fail";
                FtpStatus ftpStatus = await ftpConnection.DownloadFileAsync(this._gs.DownloadFolderPath, localDirFilePath, FtpLocalExists.Overwrite, progress: ftpProgress);
                var resultGet = new CFtpResultSendGet() {
                    FtpStatusSendGet = ftpStatus == FtpStatus.Success,
                    FileInformation = new FileInfo(saveDownloadTo)
                };
                if (ftpStatus == FtpStatus.Success) {
                    fileGet = "Ok";
                    ftpResultInfo.Success.Add(resultGet);
                }
                else {
                    ftpResultInfo.Fail.Add(resultGet);
                }

                this._logger.LogInformation("[FTP_GET_FTP_FILE_DIR] {fileGet} @ {saveDownloadTo}", fileGet, saveDownloadTo);
            }

            if (ftpConnection.IsConnected) {
                await ftpConnection.DisconnectAsync();
            }

            return ftpResultInfo;
        }

        public async Task<CFtpResultInfo> CreateFtpConnectionAndSendFtpFiles(string ipDomainHost, int portNumber, string userName, string password, string remoteWorkDir, string localDirPath, string fileName = null, Action<double> progress = null) {
            FtpClient ftpClient = await this.CreateFtpConnection(ipDomainHost, portNumber, userName, password, remoteWorkDir);
            return await this.SendFtpFiles(ftpClient, localDirPath, fileName, progress);
        }

        public async Task<CFtpResultInfo> CreateFtpConnectionAndGetFtpFileDir(string ipDomainHost, int portNumber, string userName, string password, string remoteWorkDir, string localDirFilePath, bool isDirectory = false, Action<double> progress = null) {
            FtpClient ftpClient = await this.CreateFtpConnection(ipDomainHost, portNumber, userName, password, remoteWorkDir);
            return await this.GetFtpFileDir(ftpClient, localDirFilePath, isDirectory, progress);
        }

    }

}
