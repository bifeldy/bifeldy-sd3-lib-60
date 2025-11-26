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

using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class IEnumerableExtensions {

        public static DataTable ToDataTable<T>(this IEnumerable<T> arrayListData, string tableName = null, string arrayListSingleValueColumnName = null) {
            if (string.IsNullOrEmpty(tableName)) {
                tableName = typeof(T).Name;
            }

            var table = new DataTable(tableName);

            //
            // Special handling for value types and string
            //
            // Tabel hanya punya 1 kolom
            // create table `table_name` ( `column_name` varchar(255) );
            //
            // IEnumerable<string> ls = new List<string>() { "Row1", "Row2", "Row3" };
            // ls.ToDataTable(ls, "table_name", "column_name");
            //

            Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (t.IsValueType || t == typeof(string) || t == typeof(DateTime) || t == typeof(decimal)) {
                if (string.IsNullOrEmpty(arrayListSingleValueColumnName)) {
                    throw new Exception("Nama Kolom Tabel Wajib Diisi");
                }

                var dc = new DataColumn(arrayListSingleValueColumnName, t);
                table.Columns.Add(dc);
                foreach (T item in arrayListData) {
                    DataRow dr = table.NewRow();
                    dr[0] = item;
                    table.Rows.Add(dr);
                }
            }
            else {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(t);
                foreach (PropertyDescriptor prop in properties) {
                    _ = table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }

                foreach (T item in arrayListData) {
                    DataRow row = table.NewRow();
                    foreach (PropertyDescriptor prop in properties) {
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                    }

                    table.Rows.Add(row);
                }
            }

            table.CaseSensitive = false;

            return table;
        }

        public static async Task ToCsv<T>(this IEnumerable<T> arrayListData, string delimiter, string outputFilePath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.UTF8)) {
                PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                if (includeHeader) {
                    string headerLine = string.Join(delimiter, properties.Select(prop => {
                        string name = prop.Name;

                        if (allUppercase) {
                            name = name.ToUpper();
                        }

                        if (useDoubleQuote) {
                            name = $"\"{name.Replace("\"", "\"\"")}\"";
                        }

                        return name;
                    }));

                    await streamWriter.WriteLineAsync(headerLine.AsMemory(), token);
                }

                foreach (T item in arrayListData) {
                    string line = string.Join(delimiter, properties.Select(prop => {
                        object value = prop.GetValue(item);
                        if (value == null) {
                            return "";
                        }

                        string text = value.ToString();
                        if (value is DateTime dt) {
                            text = dt.ToString("O");
                        }

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        bool mustQuote = text.Contains(delimiter) || text.Contains('"') || text.Contains('\n') || text.Contains('\r');
                        if (useDoubleQuote || mustQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    await streamWriter.WriteLineAsync(line.AsMemory(), token);
                }
            }
        }

    }

}
