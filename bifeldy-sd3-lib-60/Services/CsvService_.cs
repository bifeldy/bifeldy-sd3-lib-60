/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: CSV Files Manager
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Data;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface ICsvService {
        string CsvFolderPath { get; }
        bool DataTable2CSV(DataTable table, string filename, string separator, string outputPath = null);
    }

    public sealed class CCsvService : ICsvService {

        private readonly EnvVar _envVar;
        private readonly ILogger<CCsvService> _logger;

        private readonly IApplicationService _as;

        public string CsvFolderPath { get; }

        public CCsvService(IOptions<EnvVar> envVar, ILogger<CCsvService> logger, IApplicationService @as) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;

            this.CsvFolderPath = Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, this._envVar.CSV_FOLDER_PATH);
            if (!Directory.Exists(this.CsvFolderPath)) {
                _ = Directory.CreateDirectory(this.CsvFolderPath);
            }
        }

        public bool DataTable2CSV(DataTable table, string filename, string separator, string outputPath = null) {
            bool res = false;
            StreamWriter writer = null;
            string path = Path.Combine(outputPath ?? this.CsvFolderPath, filename);
            try {
                writer = new StreamWriter(path);
                string sep = string.Empty;
                var builder = new StringBuilder();
                foreach (DataColumn col in table.Columns) {
                    _ = builder.Append(sep).Append(col.ColumnName);
                    sep = separator;
                }
                // Untuk Export *.CSV Di Buat NAMA_KOLOM Besar Semua Tanpa Petik "NAMA_KOLOM"
                writer.WriteLine(builder.ToString().ToUpper());
                foreach (DataRow row in table.Rows) {
                    sep = string.Empty;
                    builder = new StringBuilder();
                    foreach (DataColumn col in table.Columns) {
                        _ = builder.Append(sep).Append(row[col.ColumnName]);
                        sep = separator;
                    }

                    writer.WriteLine(builder.ToString());
                }

                this._logger.LogInformation("[DATA_TABLE_2_CSV] {path}", path);
                res = true;
            }
            catch (Exception ex) {
                this._logger.LogError("[DATA_TABLE_2_CSV] {ex}", ex.Message);
            }
            finally {
                writer?.Close();
            }

            return res;
        }

    }

}
