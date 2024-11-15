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
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface ISftpService {
        Task ExecuteSshCommand(string ipDomain, string username, string password, string commandLine, CancellationToken cancellationToken, IProgress<CScriptOutputLine> progress = null);
        bool WriteStringToFile(string hostname, int port, string username, string password, string remotePath, string textData);
        bool WriteByteToFile(string hostname, int port, string username, string password, string remotePath, byte[] byteData);
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

        public async Task ExecuteSshCommand(string ipDomain, string username, string password, string commandLine, CancellationToken cancellationToken, IProgress<CScriptOutputLine> progress = null) {
            using (var client = new SshClient(ipDomain, username, password)) {
                client.Connect();

                using (SshCommand command = client.CreateCommand(commandLine)) {
                    await command.ExecuteAsync(cancellationToken, progress);
                }

                client.Disconnect();
            }
        }

        public bool WriteStringToFile(string hostname, int port, string username, string password, string remotePath, string textData) {
            try {
                using (var sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    sftp.WriteAllText(remotePath, textData);
                    sftp.Disconnect();
                }

                return true;
            }
            catch (Exception ex) {
                this._logger.LogError("[SFTP_WRITE_STRING] {ex}", ex.Message);
                return false;
            }
        }

        public bool WriteByteToFile(string hostname, int port, string username, string password, string remotePath, byte[] byteData) {
            try {
                using (var sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    sftp.WriteAllBytes(remotePath, byteData);
                    sftp.Disconnect();
                }

                return true;
            }
            catch (Exception ex) {
                this._logger.LogError("[SFTP_WRITE_BYTE] {ex}", ex.Message);
                return false;
            }
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
            IEnumerable<ISftpFile> response = Enumerable.Empty<ISftpFile>();
            try {
                using (var sftp = new SftpClient(hostname, port, username, password)) {
                    sftp.Connect();
                    response = sftp.ListDirectory(remotePath);
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
