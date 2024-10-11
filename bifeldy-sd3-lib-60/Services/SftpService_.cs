/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Secure SSH
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.Extensions.Logging;

using Renci.SshNet;
using Renci.SshNet.Sftp;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Services {

    public interface ISftpService {
        bool GetFile(string hostname, int port, string username, string password, string remotePath, string localFile, Action<double> progress = null);
        bool PutFile(string hostname, int port, string username, string password, string localFile, string remotePath, Action<double> progress = null);
        string[] GetDirectoryList(string hostname, int port, string username, string password, string remotePath);
    }

    [SingletonServiceRegistration]
    public sealed class CSftpService : ISftpService {

        private readonly ILogger<CSftpService> _logger;

        public CSftpService(ILogger<CSftpService> logger) {
            this._logger = logger;
        }

        public bool GetFile(string hostname, int port, string username, string password, string remotePath, string localFile, Action<double> progress = null) {
            try {
                using (var sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    using (var fs = new FileStream(localFile, FileMode.OpenOrCreate)) {
                        sftp.DownloadFile(remotePath, fs, downloaded => {
                            double percentage = (double) downloaded / fs.Length * 100;
                            progress?.Invoke(percentage);

                            this._logger.LogInformation("[SFTP_GET_FILE] {localFile} @ {percentage} %", localFile, percentage);
                        });
                    }

                    sftp.Disconnect();
                }

                return true;
            }
            catch (Exception ex) {
                this._logger.LogError("[SFTP_GET_FILE] {ex}", ex.Message);
                return false;
            }
        }

        public bool PutFile(string hostname, int port, string username, string password, string localFile, string remotePath, Action<double> progress = null) {
            try {
                using (var sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    using (var fs = new FileStream(localFile, FileMode.Open)) {
                        sftp.UploadFile(fs, remotePath, uploaded => {
                            double percentage = (double) uploaded / fs.Length * 100;
                            progress?.Invoke(percentage);

                            this._logger.LogInformation("[SFTP_PUT_FILE] {localFile} @ {percentage} %", localFile, percentage);
                        });
                    }

                    sftp.Disconnect();
                }

                return true;
            }
            catch (Exception ex) {
                this._logger.LogError("[SFTP_PUT_FILE] {ex}", ex.Message);
                return false;
            }
        }

        public string[] GetDirectoryList(string hostname, int port, string username, string password, string remotePath) {
            var response = new List<SftpFile>();
            try {
                using (var sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    response = (List<SftpFile>) sftp.ListDirectory(remotePath);
                    sftp.Disconnect();
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[SFTP_GET_DIRECTORY_LIST] {ex}", ex.Message);
            }

            return response.Select(r => r.FullName).ToArray();
        }

    }

}
