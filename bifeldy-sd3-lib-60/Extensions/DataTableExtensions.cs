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
using System.Reflection;
using System.Text;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class DataTableExtensions {

        public static List<T> ToList<T>(this DataTable dt) {
            dt.CaseSensitive = false;
            PropertyInfo[] properties = typeof(T).GetProperties();

            return dt.AsEnumerable().Select(row => {
                T objT = Activator.CreateInstance<T>();
                foreach (PropertyInfo pro in properties) {
                    try {
                        pro.SetValue(objT, row[pro.Name]);
                    }
                    catch {
                        //
                    }
                }

                return objT;
            }).ToList();
        }

        public static void ToCsv(this DataTable dt, string separator, string outputFilePath = null) {
            using (var writer = new StreamWriter(outputFilePath)) {
                string sep = string.Empty;
                var builder = new StringBuilder();
                foreach (DataColumn col in dt.Columns) {
                    _ = builder.Append(sep).Append(col.ColumnName);
                    sep = separator;
                }

                // Untuk Export *.CSV Di Buat NAMA_KOLOM Besar Semua Tanpa Petik "NAMA_KOLOM"
                writer.WriteLine(builder.ToString().ToUpper());
                foreach (DataRow row in dt.Rows) {
                    sep = string.Empty;
                    builder = new StringBuilder();
                    foreach (DataColumn col in dt.Columns) {
                        _ = builder.Append(sep).Append(row[col.ColumnName]);
                        sep = separator;
                    }

                    writer.WriteLine(builder.ToString());
                }
            }
        }

    }

}
