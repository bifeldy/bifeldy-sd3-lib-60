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

using ChoETL;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Services {

    public interface ICsvService {
        List<CCsvColumn> GetColumnFromClassType(Type tableClass);
        DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null, Encoding encoding = null);
        string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null);
        IDataReader Csv2DataReader(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null);
        IEnumerable<T> Csv2Enumerable<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null);
        List<T> Csv2List<T>(string filePath, string delimiter = ",", List<CCsvColumn> csvColumn = null, Encoding encoding = null);
    }

    [SingletonServiceRegistration]
    public sealed class CCsvService : ICsvService {

        public CCsvService() {
            //
        }

        public List<CCsvColumn> GetColumnFromClassType(Type tableClass) {
            PropertyInfo[] properties = tableClass.GetProperties();

            var csvColumn = new List<CCsvColumn>();
            foreach (PropertyInfo prop in properties) {
                csvColumn.Add(new() { ColumnName = prop.Name, Position = 0, FieldType = prop.PropertyType, FieldName = prop.Name });
            }

            return csvColumn;
        }

        // Posisi Kolom CSV Start Dari 1 Bukan 0
        private ChoCSVReader<dynamic> ChoEtlSetupCsv(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null) {
            var cfg = new ChoCSVRecordConfiguration {
                Delimiter = delimiter,
                MayHaveQuotedFields = false,
                Encoding = encoding ?? Encoding.Default,
                // MaxLineSize = 1_000_000_000
            };

            ChoCSVReader<dynamic> csv = new ChoCSVReader(filePath, cfg).WithFirstLineHeader(false);

            if (csvColumn != null) {
                csvColumn = csvColumn.OrderBy(c => c.Position).ToList();

                foreach (CCsvColumn cc in csvColumn) {
                    csv = csv.WithField(cc.ColumnName, cc.Position, cc.FieldType, fieldName: cc.ColumnName);
                }
            }

            return csv;
        }

        public DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null, Encoding encoding = null) {
            var fi = new FileInfo(filePath);

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(fi.FullName, delimiter, csvColumn, encoding)) {
                return csv.AsDataTable(tableName ?? fi.Name);
            }
        }

        public string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null) {
            var sb = new StringBuilder();

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(new FileInfo(filePath).FullName, delimiter, csvColumn, encoding)) {
                using (var w = new ChoJSONWriter(sb)) {
                    w.Write(csv);
                }
            }

            return sb.ToString();
        }

        public IDataReader Csv2DataReader(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null) {
            return this.ChoEtlSetupCsv(new FileInfo(filePath).FullName, delimiter, csvColumn, encoding).AsDataReader();
        }

        public IEnumerable<T> Csv2Enumerable<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null) {
            using (IDataReader dr = this.Csv2DataReader(filePath, delimiter, csvColumn, encoding ?? Encoding.Default)) {
                try {
                    PropertyInfo[] properties = typeof(T).GetProperties();

                    while (dr.Read()) {
                        var cols = new Dictionary<string, dynamic>(StringComparer.InvariantCultureIgnoreCase);
                        for (int i = 0; i < dr.FieldCount; i++) {
                            if (!dr.IsDBNull(i)) {
                                cols[dr.GetName(i).ToUpper()] = dr.GetValue(i);
                            }
                        }

                        T objT = Activator.CreateInstance<T>();
                        foreach (PropertyInfo pro in properties) {
                            string key = pro.Name.ToUpper();
                            if (cols.ContainsKey(key)) {
                                dynamic val = cols[key];
                                if (val != null) {
                                    pro.SetValue(objT, val);
                                }
                            }
                        }

                        yield return objT;
                    }
                }
                finally {
                    dr.Close();
                }
            }
        }

        public List<T> Csv2List<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, Encoding encoding = null) {
            return this.Csv2Enumerable<T>(filePath, delimiter, csvColumn, encoding ?? Encoding.Default).ToList();
        }

    }

}
