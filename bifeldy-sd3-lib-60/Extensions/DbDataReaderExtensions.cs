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
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class DbDataReaderExtensions {

        public static IEnumerable<T> FetchRow<T>(this DbDataReader dr, CancellationToken token = default, Action<T> callback = null) {
            PropertyInfo[] properties = typeof(T).GetProperties();

            if (dr.HasRows) {
                while (dr.Read() && !token.IsCancellationRequested) {
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

        public static void ToCsv(this DbDataReader dr, string delimiter, string outputFilePath = null, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null) {
            using (var streamWriter = new StreamWriter(outputFilePath, false, encoding ?? Encoding.Default)) {
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

                    streamWriter.WriteLine(header);
                }

                while (dr.Read()) {
                    string line = string.Join(delimiter, Enumerable.Range(0, dr.FieldCount).Select(i => {
                        if (dr.IsDBNull(i)) {
                            return "";
                        }

                        string text = dr.GetValue(i).ToString();

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
