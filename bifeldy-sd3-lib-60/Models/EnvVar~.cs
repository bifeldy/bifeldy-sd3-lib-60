/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Environment Variable Model
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 *              :: Bisa Dipakai Untuk Inherit
 * 
 */

namespace bifeldy_sd3_lib_60.Models {

    public class EnvVar {

        protected static string GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);

        private bool isUsingPostgres = true;
        public bool IS_USING_POSTGRES {
            get {
                string dbPgEnv = GetEnvVar("IS_USING_POSTGRES");
                if (!string.IsNullOrEmpty(dbPgEnv)) {
                    this.isUsingPostgres = bool.Parse(dbPgEnv);
                }

                return this.isUsingPostgres;
            }
            set => this.isUsingPostgres = value;
        }

        private string kunciIpDomain = "localhost";
        public string KUNCI_IP_DOMAIN {
            get {
                string kunciIpDomainEnv = GetEnvVar("KUNCI_IP_DOMAIN");
                if (!string.IsNullOrEmpty(kunciIpDomainEnv)) {
                    this.kunciIpDomain = kunciIpDomainEnv;
                }

                return this.kunciIpDomain;
            }
            set => this.kunciIpDomain = value;
        }

        private string kunciGxxx = "kuncirest";
        public string KUNCI_GXXX {
            get {
                string kunciGxxxEnv = GetEnvVar("KUNCI_GXXX");
                if (!string.IsNullOrEmpty(kunciGxxxEnv)) {
                    this.kunciGxxx = kunciGxxxEnv;
                }

                return this.kunciGxxx;
            }
            set => this.kunciGxxx = value;
        }

        private string jwtAudience = "jwt_audience";
        public string JWT_AUDIENCE {
            get {
                string jwtAudienceEnv = GetEnvVar("JWT_AUDIENCE");
                if (!string.IsNullOrEmpty(jwtAudienceEnv)) {
                    this.jwtAudience = jwtAudienceEnv;
                }

                return this.jwtAudience;
            }
            set => this.jwtAudience = value;
        }

        private string jwtSecret = "jwt_secret";
        public string JWT_SECRET {
            get {
                string jwtSecretEnv = GetEnvVar("JWT_SECRET");
                if (!string.IsNullOrEmpty(jwtSecretEnv)) {
                    this.jwtSecret = jwtSecretEnv;
                }

                return this.jwtSecret;
            }
            set => this.jwtSecret = value;
        }

        private string backupFolderPath = "backups";
        public string BACKUP_FOLDER_PATH {
            get {
                string backupFolderPathEnv = GetEnvVar("BACKUP_FOLDER_PATH");
                if (!string.IsNullOrEmpty(backupFolderPathEnv)) {
                    this.backupFolderPath = backupFolderPathEnv;
                }

                return this.backupFolderPath;
            }
            set => this.backupFolderPath = value;
        }

        private string tempFolderPath = "temps";
        public string TEMP_FOLDER_PATH {
            get {
                string tempFolderPathEnv = GetEnvVar("TEMP_FOLDER_PATH");
                if (!string.IsNullOrEmpty(tempFolderPathEnv)) {
                    this.tempFolderPath = tempFolderPathEnv;
                }

                return this.tempFolderPath;
            }
            set => this.tempFolderPath = value;
        }

        private string csvFolderPath = "csvs";
        public string CSV_FOLDER_PATH {
            get {
                string csvFolderPathEnv = GetEnvVar("CSV_FOLDER_PATH");
                if (!string.IsNullOrEmpty(csvFolderPathEnv)) {
                    this.csvFolderPath = csvFolderPathEnv;
                }

                return this.csvFolderPath;
            }
            set => this.csvFolderPath = value;
        }

        private string zipFolderPath = "zips";
        public string ZIP_FOLDER_PATH {
            get {
                string zipFolderPathEnv = GetEnvVar("ZIP_FOLDER_PATH");
                if (!string.IsNullOrEmpty(zipFolderPathEnv)) {
                    this.zipFolderPath = zipFolderPathEnv;
                }

                return this.zipFolderPath;
            }
            set => this.zipFolderPath = value;
        }

        private string downloadFolderPath = "downloads";
        public string DOWNLOAD_FOLDER_PATH {
            get {
                string downloadFolderPathEnv = GetEnvVar("DOWNLOAD_FOLDER_PATH");
                if (!string.IsNullOrEmpty(downloadFolderPathEnv)) {
                    this.downloadFolderPath = downloadFolderPathEnv;
                }

                return this.downloadFolderPath;
            }
            set => this.downloadFolderPath = value;
        }

        private string imageFolderPath = "images";
        public string IMAGE_FOLDER_PATH {
            get {
                string imageFolderPathEnv = GetEnvVar("IMAGE_FOLDER_PATH");
                if (!string.IsNullOrEmpty(imageFolderPathEnv)) {
                    this.imageFolderPath = imageFolderPathEnv;
                }

                return this.imageFolderPath;
            }
            set => this.imageFolderPath = value;
        }

        private int maxRetentionDays = 14;
        public int MAX_RETENTIONS_DAYS {
            get {
                string maxRetentionDaysEnv = GetEnvVar("MAX_RETENTIONS_DAYS");
                if (!string.IsNullOrEmpty(maxRetentionDaysEnv)) {
                    this.maxRetentionDays = int.Parse(maxRetentionDaysEnv);
                }

                return this.maxRetentionDays;
            }
            set => this.maxRetentionDays = value;
        }

        private string smtpServerIpDomain = "http://127.0.0.1";
        public string SMTP_SERVER_IP_DOMAIN {
            get {
                string smtpServerIpDomainEnv = GetEnvVar("SMTP_SERVER_IP_DOMAIN");
                if (!string.IsNullOrEmpty(smtpServerIpDomainEnv)) {
                    this.smtpServerIpDomain = smtpServerIpDomainEnv;
                }

                return this.smtpServerIpDomain;
            }
            set => this.smtpServerIpDomain = value;
        }

        private int smtpServerPort = 587;
        public int SMTP_SERVER_PORT {
            get {
                string smtpServerPortEnv = GetEnvVar("SMTP_SERVER_PORT");
                if (!string.IsNullOrEmpty(smtpServerPortEnv)) {
                    this.smtpServerPort = int.Parse(smtpServerPortEnv);
                }

                return this.smtpServerPort;
            }
            set => this.smtpServerPort = value;
        }

        private string smtpServerUsername = "admin@localhost";
        public string SMTP_SERVER_USERNAME {
            get {
                string smtpServerUsernameEnv = GetEnvVar("SMTP_SERVER_USERNAME");
                if (!string.IsNullOrEmpty(smtpServerUsernameEnv)) {
                    this.smtpServerUsername = smtpServerUsernameEnv;
                }

                return this.smtpServerUsername;
            }
            set => this.smtpServerUsername = value;
        }

        private string smtpServerPassword = "root";
        public string SMTP_SERVER_PASSWORD {
            get {
                string smtpServerPasswordEnv = GetEnvVar("SMTP_SERVER_PASSWORD");
                if (!string.IsNullOrEmpty(smtpServerPasswordEnv)) {
                    this.smtpServerPassword = smtpServerPasswordEnv;
                }

                return this.smtpServerPassword;
            }
            set => this.smtpServerPassword = value;
        }

        private string wsSyncHo = "http://127.0.0.1";
        public string WS_SYNCHO {
            get {
                string wsSyncHoEnv = GetEnvVar("WS_SYNCHO");
                if (!string.IsNullOrEmpty(wsSyncHoEnv)) {
                    this.wsSyncHo = wsSyncHoEnv;
                }

                return this.wsSyncHo;
            }
            set => this.wsSyncHo = value;
        }

        // private string haha;
        // public string HAHA {
        //     get => haha ??= "TEST";
        //     set => haha = value;
        // }

    }

}
