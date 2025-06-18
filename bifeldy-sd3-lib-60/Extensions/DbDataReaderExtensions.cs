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
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class DbDataReaderExtensions {

        public static IEnumerable<T> FetchRow<T>(this DbDataReader dr, CancellationToken token = default, Action<T> callback = null) {
            PropertyInfo[] properties = typeof(T).GetProperties();

            if (dr.HasRows) {
                while (dr.Read() && !token.IsCancellationRequested) {
                    T objT = default;

                    Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                    if (t.IsPrimitive || t == typeof(string) || t == typeof(DateTime) || t == typeof(decimal)) {
                        if (!dr.IsDBNull(0)) {
                            dynamic val = dr.GetValue(0);

                            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                            if (converter.CanConvertFrom(val.GetType())) {
                                val = converter.ConvertFrom(val);
                            }
                            else {
                                val = Convert.ChangeType(val, typeof(T));
                            }

                            objT = val;
                        }
                    }
                    else {
                        objT = Activator.CreateInstance<T>();

                        var cols = new Dictionary<string, dynamic>(StringComparer.InvariantCultureIgnoreCase);
                        for (int i = 0; i < dr.FieldCount; i++) {
                            if (!dr.IsDBNull(i)) {
                                cols[dr.GetName(i).ToUpper()] = dr.GetValue(i);
                            }
                        }

                        foreach (PropertyInfo pro in properties) {
                            string key = pro.Name.ToUpper();
                            if (cols.ContainsKey(key)) {
                                dynamic val = cols[key];

                                if (val != null) {
                                    TypeConverter converter = TypeDescriptor.GetConverter(pro.PropertyType);
                                    if (converter.CanConvertFrom(val.GetType())) {
                                        val = converter.ConvertFrom(val);
                                    }
                                    else {
                                        val = Convert.ChangeType(val, pro.PropertyType);
                                    }

                                    pro.SetValue(objT, val);
                                }
                            }
                        }
                    }

                    callback?.Invoke(objT);
                    yield return objT;
                }
            }
        }

        public static List<T> ToList<T>(this DbDataReader dr, CancellationToken token = default, Action<T> callback = null) {
            var ls = new List<T>();

            foreach (T objT in dr.FetchRow(token, callback)) {
                ls.Add(objT);
            }

            return ls;
        }

        public static async Task ToCsv(this DbDataReader dr, string delimiter, string outputFilePath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, CancellationToken token = default) {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.UTF8)) {
                if (includeHeader) {
                    string header = string.Join(delimiter, Enumerable.Range(0, dr.FieldCount).Select(i => {
                        string text = dr.GetName(i);

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        if (useDoubleQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    await streamWriter.WriteAsync(header.AsMemory(), token);
                }

                while (await dr.ReadAsync(token)) {
                    string line = string.Join(delimiter, Enumerable.Range(0, dr.FieldCount).Select(i => {
                        if (dr.IsDBNull(i)) {
                            return "";
                        }

                        string text = dr.GetValue(i).ToString();

                        if (allUppercase) {
                            text = text.ToUpper();
                        }

                        bool mustQuote = text.Contains(delimiter) || text.Contains('"') || text.Contains('\n') || text.Contains('\r');
                        if (useDoubleQuote || mustQuote) {
                            text = $"\"{text.Replace("\"", "\"\"")}\"";
                        }

                        return text;
                    }));

                    await streamWriter.WriteAsync(line.AsMemory(), token);
                }
            }
        }

    }

}
