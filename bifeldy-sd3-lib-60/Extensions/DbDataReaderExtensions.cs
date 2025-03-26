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

        public static void ToCsv(this DbDataReader dr, string separator, string outputFilePath = null) {
            using (var streamWriter = new StreamWriter(outputFilePath, true)) {
                string struktur = Enumerable.Range(0, dr.FieldCount).Select(i => dr.GetName(i)).Aggregate((i, j) => $"{i}{separator}{j}");
                streamWriter.WriteLine(struktur.ToUpper());
                streamWriter.Flush();

                while (dr.Read()) {
                    var _colValue = new List<string>();
                    for (int i = 0; i < dr.FieldCount; i++) {
                        string val = string.Empty;
                        if (!dr.IsDBNull(i)) {
                            val = dr.GetValue(i).ToString();
                        }

                        _colValue.Add(val);
                    }

                    string line = _colValue.Aggregate((i, j) => $"{i}{separator}{j}");
                    if (!string.IsNullOrEmpty(line)) {
                        streamWriter.WriteLine(line.ToUpper());
                        streamWriter.Flush();
                    }
                }
            }
        }

    }

}
