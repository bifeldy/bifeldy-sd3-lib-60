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

        public static void ToCsv(this DataTable dt, string delimiter, string outputFilePath = null, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null) {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.Default)) {
                string sep = string.Empty;
                var builder = new StringBuilder();

                foreach (DataColumn col in dt.Columns) {
                    string text = col.ColumnName;

                    if (useDoubleQuote) {
                        text = "\"" + text.Replace("\"", "\"\"") + "\"";
                    }

                    if (allUppercase) {
                        text = text.ToUpper();
                    }

                    _ = builder.Append(sep).Append(text);

                    sep = delimiter;
                }

                streamWriter.WriteLine(builder.ToString());
                streamWriter.Flush();

                foreach (DataRow row in dt.Rows) {
                    sep = string.Empty;
                    builder = builder.Clear();

                    foreach (DataColumn col in dt.Columns) {
                        string text = row[col.ColumnName].ToString();

                        if (useDoubleQuote) {
                            text = "\"" + text.Replace("\"", "\"\"") + "\"";
                        }

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        _ = builder.Append(sep).Append(text);

                        sep = delimiter;
                    }

                    streamWriter.WriteLine(builder.ToString());
                    streamWriter.Flush();
                }
            }
        }

    }

}
