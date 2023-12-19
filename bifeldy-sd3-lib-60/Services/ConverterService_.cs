/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Alat Konversi
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Data;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Services {

    public interface IConverterService {
        string TimeSpanToEta(TimeSpan ts);
        T JsonToObject<T>(string j2o);
        string ObjectToJson(object body);
        string ByteToString(byte[] bytes, bool removeHypens = true);
        byte[] StringToByte(string hex, string separator = null);
        List<T> DataTableToList<T>(DataTable dt);
        DataTable ListToDataTable<T>(List<T> listData, string tableName = null, string arrayListSingleValueColumnName = null);
        T GetDefaultValueT<T>();
    }

    public sealed class CConverterService : IConverterService {

        private readonly ILogger<CConverterService> _logger;

        public CConverterService(ILogger<CConverterService> logger) {
            _logger = logger;
        }

        public string TimeSpanToEta(TimeSpan ts) {
            return ts.ToEta();
        }

        public T JsonToObject<T>(string j2o) {
            return JsonConvert.DeserializeObject<T>(j2o);
        }

        public string ObjectToJson(object o2j) {
            return JsonConvert.SerializeObject(o2j);
        }

        public string ByteToString(byte[] bytes, bool removeHypens = true) {
            return bytes.ToString(removeHypens);
        }

        public byte[] StringToByte(string hex, string separator = null) {
            return hex.ToByte(separator);
        }

        public List<T> DataTableToList<T>(DataTable dt) {
            return dt.ToList<T>();
        }

        public DataTable ListToDataTable<T>(List<T> listData, string tableName = null, string arrayListSingleValueColumnName = null) {
            return listData.ToDataTable(tableName, arrayListSingleValueColumnName);
        }

        public T GetDefaultValueT<T>() {
            dynamic x = null;
            switch (Type.GetTypeCode(typeof(T))) {
                case TypeCode.DateTime:
                    x = DateTime.MinValue;
                    break;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                    x = 0;
                    break;
                case TypeCode.Boolean:
                    x = false;
                    break;
            }
            return (T) Convert.ChangeType(x, typeof(T));
        }

    }

}
