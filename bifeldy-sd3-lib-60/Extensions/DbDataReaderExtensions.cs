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

        public static IEnumerable<T> ToEnumerable<T>(this DbDataReader dr, CancellationToken token = default, Action<T> callback = null) {
            PropertyInfo[] properties = typeof(T).GetProperties();

            if (dr.HasRows) {
                while (dr.Read() && !token.IsCancellationRequested) {
                    T objT = default;

                    Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                    if (t.IsValueType || t == typeof(string)) {
                        if (!dr.IsDBNull(0)) {
                            dynamic val = dr.GetValue(0);
                            objT = (T)ChangeType(val, typeof(T));
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
                                    val = ChangeType(val, pro.PropertyType);
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

                    await streamWriter.WriteLineAsync(header.AsMemory(), token);
                }

                while (await dr.ReadAsync(token)) {
                    string line = string.Join(delimiter, Enumerable.Range(0, dr.FieldCount).Select(i => {
                        if (dr.IsDBNull(i)) {
                            return "";
                        }

                        object value = dr.GetValue(i);
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

        // Safe conversion handler that prevents IConvertible crashes for array / reference payloads
        private static object ChangeType(object value, Type targetType) {
            if (value == null) {
                return null;
            }

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Direct mapping bypass: If database byte[] type fits perfectly into property array type, pass it raw
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

            // Fallback strategy for specialized mapping scenarios
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(value.GetType())) {
                return converter.ConvertFrom(value);
            }

            if (underlyingType.IsArray) {
                return value;
            }

            return Convert.ChangeType(value, underlyingType);
        }

    }

}
