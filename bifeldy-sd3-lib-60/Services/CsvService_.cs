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

using ChoETL;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Services {

    public interface ICsvService {
        string CsvFolderPath { get; }
        void DataTable2CSV(DataTable dt, string filename, string separator, string outputPath = null);
        List<T> Csv2List<T>(Stream stream, char delimiter = ',', bool skipHeader = false, List<string> csvColumn = null, List<string> requiredColumn = null);
        string Csv2Json(string filePath, string delimiter, List<CCsv2Json> csvColumn = null);
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

        public string WriteCsv(TextReader textReader, string filename, string outputPath = null) {
            if (string.IsNullOrEmpty(filename)) {
                throw new Exception("Nama File + Extensi Harus Di Isi");
            }

            string path = Path.Combine(outputPath ?? this.CsvFolderPath, filename);
            using (var streamWriter = new StreamWriter(path, true)) {
                string line = null;
                do {
                    line = textReader.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(line)) {
                        streamWriter.WriteLine(line.ToUpper());
                        streamWriter.Flush();
                    }
                }
                while (!string.IsNullOrEmpty(line));
            }

            return path;
        }

        public void DataTable2CSV(DataTable dt, string filename, string separator, string outputPath = null) {
            try {
                string path = Path.Combine(outputPath ?? this.CsvFolderPath, filename);
                dt.ToCsv(separator, path);
                this._logger.LogInformation("[DATA_TABLE_2_CSV] {path}", path);
            }
            catch (Exception ex) {
                this._logger.LogError("[DATA_TABLE_2_CSV] {ex}", ex.Message);
                throw;
            }
        }

        public List<T> Csv2List<T>(Stream stream, char delimiter = ',', bool skipHeader = false, List<string> csvColumn = null, List<string> requiredColumn = null) {
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

        public string Csv2Json(string filePath, string delimiter, List<CCsv2Json> csvColumn = null) {
            if (csvColumn == null || csvColumn?.Count <= 0) {
                throw new Exception("Daftar Kolom Harus Di Isi");
            }

            var sb = new StringBuilder();
            using (ChoCSVReader<dynamic> csv = new ChoCSVReader(filePath).WithDelimiter(delimiter)) {
                ChoCSVReader<dynamic> _data = csv;

                if (csvColumn != null) {
                    for (int i = 0; i < csvColumn.Count; i++) {
                        int pos = csvColumn[i].Position;
                        _data = _data.WithField(csvColumn[i].ColumnName, (pos > 0) ? pos : (i + 1), csvColumn[i].DataType);
                    }
                    _data = _data.WithFirstLineHeader(true);
                }

                using (var w = new ChoJSONWriter(sb)) {
                    w.Write(csv);
                }
            }

            return sb.ToString();
        }

    }

}
