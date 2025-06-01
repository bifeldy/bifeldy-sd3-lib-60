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
        DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null);
        string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null);
        List<T> Csv2List<T>(string filePath, string delimiter = ",", List<CCsvColumn> csvColumn = null);
    }

    [SingletonServiceRegistration]
    public sealed class CCsvService : ICsvService {

        public CCsvService() {
            //
        }

        // Posisi Kolom CSV Start Dari 1 Bukan 0
        private ChoCSVReader<dynamic> ChoEtlSetupCsv(string filePath, string delimiter, List<CCsvColumn> csvColumn = null) {
            ChoCSVReader<dynamic> csv = new ChoCSVReader(filePath).WithDelimiter(delimiter);

            if (csvColumn != null) {
                csvColumn = csvColumn.OrderBy(c => c.Position).ToList();

                foreach (CCsvColumn cc in csvColumn) {
                    csv = csv.WithField(cc.ColumnName, cc.Position, cc.DataType);
                }
            }

            return csv.WithFirstLineHeader(true).MayHaveQuotedFields().MayContainEOLInData();
        }

        public DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null) {
            var fi = new FileInfo(filePath);

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(filePath, delimiter, csvColumn)) {
                return csv.AsDataTable(tableName ?? fi.Name);
            }
        }

        public string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null) {
            var sb = new StringBuilder();

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(filePath, delimiter, csvColumn)) {
                using (var w = new ChoJSONWriter(sb)) {
                    w.Write(csv);
                }
            }

            return sb.ToString();
        }

        public List<T> Csv2List<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null) {
            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(filePath, delimiter, csvColumn)) {
                using (IDataReader dr = csv.AsDataReader()) {
                    var ls = new List<T>();
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

                        ls.Add(objT);
                    }

                    return ls;
                }
            }
        }

    }

}
