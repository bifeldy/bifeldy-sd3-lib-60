/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class DataTableExtensions {

        public static List<T> ToList<T>(this DataTable dt) {
            dt.CaseSensitive = false;

            var ls = new List<T>();
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (DataRow row in dt.Rows) {
                var cols = new Dictionary<string, dynamic>(StringComparer.InvariantCultureIgnoreCase);
                foreach (DataColumn col in dt.Columns) {
                    string colName = col.ColumnName.ToUpper();
                    if (row[colName] != DBNull.Value) {
                        cols[colName] = row[colName];
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

        public static void ToCsv(this DataTable dt, string delimiter, string outputFilePath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null) {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.Default)) {
                if (includeHeader) {
                    string header = string.Join(delimiter, dt.Columns.Cast<DataColumn>().Select(col => {
                        string text = col.ColumnName;

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        if (useDoubleQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    streamWriter.WriteLine(header);
                }

                foreach (DataRow row in dt.Rows) {
                    string line = string.Join(delimiter, dt.Columns.Cast<DataColumn>().Select(col => {
                        object value = row[col];

                        if (value == DBNull.Value) {
                            return "";
                        }

                        string text = value.ToString();

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        if (useDoubleQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    streamWriter.WriteLine(line);
                }
            }
        }


    }

}
