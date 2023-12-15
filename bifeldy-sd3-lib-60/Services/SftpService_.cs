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

namespace bifeldy_sd3_lib_60.Services {

    public interface ISftpService {
        bool GetFile(string hostname, int port, string username, string password, string remotePath, string localFile);
        bool PutFile(string hostname, int port, string username, string password, string localFile, string remotePath);
        string[] GetDirectoryList(string hostname, int port, string username, string password, string remotePath);
    }

    public sealed class CSftpService : ISftpService {

        private readonly ILogger<CSftpService> _logger;

        public CSftpService(ILogger<CSftpService> logger) {
            _logger = logger;
        }

        public bool GetFile(string hostname, int port, string username, string password, string remotePath, string localFile) {
            try {
                using (SftpClient sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    using (FileStream fs = new FileStream(localFile, FileMode.OpenOrCreate)) {
                        sftp.DownloadFile(remotePath, fs, downloaded => {
                            _logger.LogInformation($"[SFTP_GET_FILE] {localFile} @ {(double) downloaded / fs.Length * 100} %");
                        });
                    }
                    sftp.Disconnect();
                }
                return true;
            }
            catch (Exception ex) {
                _logger.LogError($"[SFTP_GET_FILE] {ex.Message}");
                return false;
            }
        }

        public bool PutFile(string hostname, int port, string username, string password, string localFile, string remotePath) {
            try {
                using (SftpClient sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    using (FileStream fs = new FileStream(localFile, FileMode.Open)) {
                        sftp.UploadFile(fs, remotePath, uploaded => {
                            _logger.LogInformation($"[SFTP_PUT_FILE] {localFile} @ {(double) uploaded / fs.Length * 100} %");
                        });
                    }
                    sftp.Disconnect();
                }
                return true;
            }
            catch (Exception ex) {
                _logger.LogError($"[SFTP_PUT_FILE] {ex.Message}");
                return false;
            }
        }

        public string[] GetDirectoryList(string hostname, int port, string username, string password, string remotePath) {
            List<SftpFile> response = new List<SftpFile>();
            try {
                using (SftpClient sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    response = (List<SftpFile>) sftp.ListDirectory(remotePath);
                    sftp.Disconnect();
                }
            }
            catch (Exception ex) {
                _logger.LogError($"[SFTP_GET_DIRECTORY_LIST] {ex.Message}");
            }
            return response.Select(r => r.FullName).ToArray();
        }

    }

}
