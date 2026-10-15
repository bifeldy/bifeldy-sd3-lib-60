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

    public static class DataTableExtensions {

        public static IEnumerable<T> ToEnumerable<T>(this DataTable dt, Action<T> callback = null) {
            dt.CaseSensitive = false;

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
                            // Menggunakan helper ChangeType yang aman dari crash IConvertible (array/byte[])
                            val = ChangeType(val, pro.PropertyType);
                            pro.SetValue(objT, val);
                        }
                    }
                }

                callback?.Invoke(objT);
                yield return objT;
            }
        }

        public static async Task ToCsv(this DataTable dt, string delimiter, string outputFilePath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.UTF8)) {
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

                    await streamWriter.WriteLineAsync(header.AsMemory(), token);
                }

                foreach (DataRow row in dt.Rows) {
                    string line = string.Join(delimiter, dt.Columns.Cast<DataColumn>().Select(col => {
                        object value = row[col];

                        if (value == DBNull.Value) {
                            return "";
                        }

                        string text = value.ToString();
                        if (value is DateTime dt) {
                            text = dt.ToString("O");
                        }

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        bool mustQuote = text.Contains(delimiter) || text.Contains('"') || text.Contains((char)10) || text.Contains((char)13);
                        if (useDoubleQuote || mustQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    await streamWriter.WriteLineAsync(line.AsMemory(), token);
                }
            }
        }

        // Helper untuk konversi tipe data yang aman dari error IConvertible (Array / byte[])
        private static object ChangeType(object value, Type targetType) {
            if (value == null) {
                return null;
            }

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Jika tipe data asal (dari DataRow) sudah cocok atau bisa langsung dimasukkan ke target tipe properti, langsung return
            if (targetType.IsAssignableFrom(value.GetType()) || underlyingType.IsAssignableFrom(value.GetType())) {
                return value;
            }

            // Larik biner byte[] <-> byte?[]
            if (value is byte[] rawBytes && targetType == typeof(byte?[])) {
                return rawBytes.Select(b => (byte?)b).ToArray();
            }
            if (value is byte?[] nullableBytes && targetType == typeof(byte[])) {
                return nullableBytes.Select(b => b ?? (byte)0).ToArray();
            }

            // Coba konversi menggunakan TypeConverter bawaan .NET
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(value.GetType())) {
                return converter.ConvertFrom(value);
            }

            if (underlyingType.IsArray) {
                return value;
            }

            // Fallback terakhir untuk tipe primitif murni
            return Convert.ChangeType(value, underlyingType);
        }

    }

}
