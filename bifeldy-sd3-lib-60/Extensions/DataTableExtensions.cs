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

namespace bifeldy_sd3_lib_60.Extensions {

    public static class DataTableExtensions {

        public static List<T> ToList<T>(this DataTable dt) {
            var columnNames = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToUpper()).ToList();
            PropertyInfo[] properties = typeof(T).GetProperties();

            return dt.AsEnumerable().Select(row => {
                T objT = Activator.CreateInstance<T>();
                foreach (PropertyInfo pro in properties) {
                    if (columnNames.Contains(pro.Name.ToUpper())) {
                        try {
                            pro.SetValue(objT, row[pro.Name]);
                        }
                        catch {
                            // null / default
                        }
                    }
                }

                return objT;
            }).ToList();
        }

    }

}
