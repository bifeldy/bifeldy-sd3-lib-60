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
* 
*/

namespace bifeldy_sd3_lib_60.Models {

    public class EnvVar {

        private string GetEnvVar(string key) {
            return Environment.GetEnvironmentVariable(key);
        }

        private bool isUsingPostgres = true;
        public bool IS_USING_POSTGRES {
            get {
                string dbPgEnv = GetEnvVar("IS_USING_POSTGRES");
                if (!string.IsNullOrEmpty(dbPgEnv)) {
                    isUsingPostgres = bool.Parse(dbPgEnv);
                }
                return isUsingPostgres;
            }
            set {
                isUsingPostgres = value;
            }
        }

        private string kunciIpDomain = "localhost";
        public string KUNCI_IP_DOMAIN {
            get {
                string kunciIpDomainEnv = GetEnvVar("KUNCI_IP_DOMAIN");
                if (!string.IsNullOrEmpty(kunciIpDomainEnv)) {
                    kunciIpDomain = kunciIpDomainEnv;
                }
                return kunciIpDomain;
            }
            set {
                kunciIpDomain = value;
            }
        }

        private string kunciGxxx = "kuncirest";
        public string KUNCI_GXXX {
            get {
                string kunciGxxxEnv = GetEnvVar("KUNCI_GXXX");
                if (!string.IsNullOrEmpty(kunciGxxxEnv)) {
                    kunciGxxx = kunciGxxxEnv;
                }
                return kunciGxxx;
            }
            set {
                kunciGxxx = value;
            }
        }

        private string apiKeyName = "api_key";
        public string API_KEY_NAME {
            get {
                string apiKeyNameEnv = GetEnvVar("API_KEY_NAME");
                if (!string.IsNullOrEmpty(apiKeyNameEnv)) {
                    apiKeyName = apiKeyNameEnv;
                }
                return apiKeyName;
            }
            set {
                apiKeyName = value;
            }
        }

        private string jwtName = "jwt";
        public string JWT_NAME {
            get {
                string jwtEnv = GetEnvVar("API_KEY_NAME");
                if (!string.IsNullOrEmpty(jwtEnv)) {
                    jwtName = jwtEnv;
                }
                return jwtName;
            }
            set {
                jwtName = value;
            }
        }

        private string jwtSecret = "secret_rahasia";
        public string JWT_SECRET {
            get {
                string jwtSecretEnv = GetEnvVar("JWT_SECRET");
                if (!string.IsNullOrEmpty(jwtSecretEnv)) {
                    jwtSecret = jwtSecretEnv;
                }
                return jwtSecret;
            }
            set {
                jwtSecret = value;
            }
        }

        private string backupFolderPath = "backups";
        public string BACKUP_FOLDER_PATH {
            get {
                string backupFolderPathEnv = GetEnvVar("BACKUP_FOLDER_PATH");
                if (!string.IsNullOrEmpty(backupFolderPathEnv)) {
                    backupFolderPath = backupFolderPathEnv;
                }
                return backupFolderPath;
            }
            set {
                backupFolderPath = value;
            }
        }

        private string tempFolderPath = "temps";
        public string TEMP_FOLDER_PATH {
            get {
                string tempFolderPathEnv = GetEnvVar("TEMP_FOLDER_PATH");
                if (!string.IsNullOrEmpty(tempFolderPathEnv)) {
                    tempFolderPath = tempFolderPathEnv;
                }
                return tempFolderPath;
            }
            set {
                tempFolderPath = value;
            }
        }

        private string zipFolderPath = "zips";
        public string ZIP_FOLDER_PATH {
            get {
                string zipFolderPathEnv = GetEnvVar("ZIP_FOLDER_PATH");
                if (!string.IsNullOrEmpty(zipFolderPathEnv)) {
                    zipFolderPath = zipFolderPathEnv;
                }
                return zipFolderPath;
            }
            set {
                zipFolderPath = value;
            }
        }

        private string downloadFolderPath = "downloads";
        public string DOWNLOAD_FOLDER_PATH {
            get {
                string downloadFolderPathEnv = GetEnvVar("DOWNLOAD_FOLDER_PATH");
                if (!string.IsNullOrEmpty(downloadFolderPathEnv)) {
                    downloadFolderPath = downloadFolderPathEnv;
                }
                return downloadFolderPath;
            }
            set {
                downloadFolderPath = value;
            }
        }

        private int maxRetentionDays = 14;
        public int MAX_RETENTIONS_DAYS {
            get {
                string maxRetentionDaysEnv = GetEnvVar("MAX_RETENTIONS_DAYS");
                if (!string.IsNullOrEmpty(maxRetentionDaysEnv)) {
                    maxRetentionDays = int.Parse(maxRetentionDaysEnv);
                }
                return maxRetentionDays;
            }
            set {
                maxRetentionDays = value;
            }
        }

        private string smtpServerIpDomain = "http://127.0.0.1";
        public string SMTP_SERVER_IP_DOMAIN {
            get {
                string smtpServerIpDomainEnv = GetEnvVar("SMTP_SERVER_IP_DOMAIN");
                if (!string.IsNullOrEmpty(smtpServerIpDomainEnv)) {
                    smtpServerIpDomain = smtpServerIpDomainEnv;
                }
                return smtpServerIpDomain;
            }
            set {
                smtpServerIpDomain = value;
            }
        }

        private int smtpServerPort = 587;
        public int SMTP_SERVER_PORT {
            get {
                string smtpServerPortEnv = GetEnvVar("SMTP_SERVER_PORT");
                if (!string.IsNullOrEmpty(smtpServerPortEnv)) {
                    smtpServerPort = int.Parse(smtpServerPortEnv);
                }
                return smtpServerPort;
            }
            set {
                smtpServerPort = value;
            }
        }

        private string smtpServerUsername = "admin@localhost";
        public string SMTP_SERVER_USERNAME {
            get {
                string smtpServerUsernameEnv = GetEnvVar("SMTP_SERVER_USERNAME");
                if (!string.IsNullOrEmpty(smtpServerUsernameEnv)) {
                    smtpServerUsername = smtpServerUsernameEnv;
                }
                return smtpServerUsername;
            }
            set {
                smtpServerUsername = value;
            }
        }

        private string smtpServerPassword = "root";
        public string SMTP_SERVER_PASSWORD {
            get {
                string smtpServerPasswordEnv = GetEnvVar("SMTP_SERVER_PASSWORD");
                if (!string.IsNullOrEmpty(smtpServerPasswordEnv)) {
                    smtpServerPassword = smtpServerPasswordEnv;
                }
                return smtpServerPassword;
            }
            set {
                smtpServerPassword = value;
            }
        }

        private string wsSyncHo = "http://127.0.0.1";
        public string WS_SYNCHO {
            get {
                string wsSyncHoEnv = GetEnvVar("WS_SYNCHO");
                if (!string.IsNullOrEmpty(wsSyncHoEnv)) {
                    wsSyncHo = wsSyncHoEnv;
                }
                return wsSyncHo;
            }
            set {
                wsSyncHo = value;
            }
        }

        // private string haha;
        // public string HAHA {
        //     get => haha ??= "TEST";
        //     set => haha = value;
        // }

    }

}
