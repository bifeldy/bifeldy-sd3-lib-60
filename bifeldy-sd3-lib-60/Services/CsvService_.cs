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
using System.Data.Common;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ChoETL;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Services {

    public interface ICsvService {
        string CsvFolderPath { get; }
        DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null);
        string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null);
        List<T> Csv2List<T>(string filePath, string delimiter = ",", List<CCsvColumn> csvColumn = null);
    }

    [SingletonServiceRegistration]
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

        private ChoCSVReader<dynamic> ChoEtlSetupCsv(string filePath, string delimiter, List<CCsvColumn> csvColumn) {
            if (csvColumn == null || csvColumn?.Count <= 0) {
                throw new Exception("Daftar Kolom Harus Di Isi");
            }

            ChoCSVReader<dynamic> csv = new ChoCSVReader(filePath).WithDelimiter(delimiter);
            foreach (CCsvColumn cc in csvColumn) {
                csv = csv.WithField(cc.ColumnName, cc.Position, cc.DataType);
            }

            csv = csv.WithFirstLineHeader(true).MayHaveQuotedFields().MayContainEOLInData();
            return csv;
        }

        public DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn, string tableName = null) {
            csvColumn = csvColumn.OrderBy(c => c.Position).ToList();
            var fi = new FileInfo(filePath);

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(filePath, delimiter, csvColumn)) {
                return csv.AsDataTable(tableName ?? fi.Name);
            }
        }

        public string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn) {
            csvColumn = csvColumn.OrderBy(c => c.Position).ToList();
            var sb = new StringBuilder();

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(filePath, delimiter, csvColumn)) {
                using (var w = new ChoJSONWriter(sb)) {
                    w.Write(csv);
                }
            }

            return sb.ToString();
        }

        public List<T> Csv2List<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn) {
            csvColumn = csvColumn.OrderBy(c => c.Position).ToList();
            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(filePath, delimiter, csvColumn)) {
                using (var rdr = (DbDataReader) csv.AsDataReader()) {
                    return rdr.ToList<T>();
                }
            }
        }

    }

}
