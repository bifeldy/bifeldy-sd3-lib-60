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
using System.Reflection;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface ICsvService {
        string CsvFolderPath { get; }
        bool DataTable2CSV(DataTable table, string filename, string separator, string outputPath = null);
        List<T> CsvToList<T>(Stream stream, string delimiter = ",", bool skipHeader = false, List<string> csvColumn = null, List<string> requiredColumn = null);
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

        public List<T> CsvToList<T>(Stream stream, string delimiter = ",", bool skipHeader = false, List<string> csvColumn = null, List<string> requiredColumn = null) {
            using (var reader = new StreamReader(stream)) {
                int i = 0;
                List<string> col = csvColumn ?? new();
                var row = new List<T>();

                if (skipHeader && csvColumn != null) {
                    i++;
                    _ = reader.ReadLine();
                }

                while (!reader.EndOfStream) {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line)) {
                        string[] values = line.Split(delimiter).Select(v => v.StartsWith("\"") && v.EndsWith("\"") ? v[1..^1] : v).ToArray();

                        if (i == 0) {
                            if (csvColumn == null) {
                                col.AddRange(values);
                            }
                            else {
                                var temp = new List<string>();
                                for (int j = 0; j < values.Length; j++) {
                                    if (csvColumn.Select(ac => ac.ToUpper()).Contains(values[j].ToUpper())) {
                                        temp.Add(values[j]);
                                    }
                                }

                                if (temp.Count != col.Count) {
                                    throw new Exception("Data kolom yang tersedia tidak lengkap");
                                }

                                col = temp;
                            }
                        }
                        else if (values.Length != col.Count) {
                            throw new Exception("Jumlah kolom data tidak sesuai dengan kolom header");
                        }
                        else {
                            PropertyInfo[] properties = typeof(T).GetProperties();
                            T objT = Activator.CreateInstance<T>();

                            for (int j = 0; j < col.Count; j++) {
                                string colName = col[j].ToUpper();
                                string rowVal = values[j].ToUpper();

                                if (csvColumn != null && requiredColumn != null) {
                                    if (requiredColumn.Select(rc => rc.ToUpper()).Contains(colName)) {
                                        if (string.IsNullOrEmpty(rowVal)) {
                                            throw new Exception($"Baris {i + 1} kolom {j} :: {colName} tidak boleh kosong");
                                        }
                                    }
                                }

                                foreach (PropertyInfo pro in properties) {
                                    if (pro.Name.ToUpper() == colName) {
                                        try {
                                            pro.SetValue(objT, rowVal);
                                        }
                                        catch {
                                            //
                                        }
                                    }
                                }
                            }

                            row.Add(objT);
                        }
                    }

                    i++;
                }

                return row;
            }
        }

    }

}
