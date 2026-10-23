/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: CSV Files Manager
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Libraries;
using bifeldy_sd3_lib_60.Models;
using ChoETL;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text;

namespace bifeldy_sd3_lib_60.Services {

    public interface ICsvService {
        List<CCsvColumn> GetColumnFromClassType(Type tableClass);
        DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
        string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
        IDataReader Csv2DataReader(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
        IEnumerable<T> Csv2Enumerable<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null);
    }

    [SingletonServiceRegistration]
    public sealed class CCsvService : ICsvService {

        public CCsvService() {
            //
        }

        public List<CCsvColumn> GetColumnFromClassType(Type tableClass) {
            PropertyInfo[] properties = tableClass.GetProperties();

            var csvColumn = new List<CCsvColumn>();
            foreach (PropertyInfo prop in properties) {
                csvColumn.Add(new() { ColumnName = prop.Name, Position = 0, FieldType = prop.PropertyType, FieldName = prop.Name });
            }

            return csvColumn;
        }

        // Posisi Kolom CSV Start Dari 1 Bukan 0
        private ChoCSVReader<dynamic> ChoEtlSetupCsv(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            if (string.IsNullOrEmpty(eolDelimiter)) {
                LineEndingType lineEnding = CsvLineEndingChecker.DetectLineEndings(filePath);
                switch (lineEnding) {
                    case LineEndingType.CRLF:
                        eolDelimiter = "\r\n";
                        break;
                    case LineEndingType.LF:
                        eolDelimiter = "\n";
                        break;
                    case LineEndingType.Mixed:
                        eolDelimiter = Environment.NewLine;
                        break;
                    default:
                        throw new Exception("Tidak dapat mendeteksi jenis line ending pada file '" + filePath + "'.");
                }
            }

            var cfg = new ChoCSVRecordConfiguration() {
                Delimiter = delimiter,
                MayHaveQuotedFields = true,
                MayContainEOLInData = true,
                EOLDelimiter = eolDelimiter,
                NullValue = nullValue,
                QuoteAllFields = true,
                Encoding = encoding ?? Encoding.UTF8,
                DetectEncodingFromByteOrderMarks = encoding == null,
                MaxLineSize = 1_000_000
            };

            ChoCSVReader<dynamic> csv = new ChoCSVReader(filePath, cfg);

            if (csvColumn != null) {
                csv = csv.WithFirstLineHeader(false);
                csvColumn = csvColumn.OrderBy(c => c.Position).ToList();

                foreach (CCsvColumn cc in csvColumn) {
                    Type targetFieldType = cc.FieldType;
                    // Amankan tipe field mentah agar ChoETL membaca payload biner sebagai string kasaran terlebih dahulu
                    if (targetFieldType.IsArray && (targetFieldType.GetElementType() == typeof(byte) || Nullable.GetUnderlyingType(targetFieldType.GetElementType()) == typeof(byte))) {
                        targetFieldType = typeof(string);
                    }
                    csv = csv.WithField(cc.ColumnName, cc.Position, targetFieldType, fieldName: cc.ColumnName);
                }
            }
            else {
                csv = csv.WithFirstLineHeader();
            }

            return csv;
        }

        public DataTable Csv2DataTable(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string tableName = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            var fi = new FileInfo(filePath);

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(fi.FullName, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8)) {
                DataTable dt = csv.AsDataTable(tableName ?? fi.Name);

                foreach (DataRow row in dt.Rows) {
                    foreach (DataColumn col in dt.Columns) {
                        if (row[col] is string s) {
                            if (s.Contains("\"\"")) {
                                row[col] = s.Replace("\"\"", "\"");
                            }
                        }
                    }
                }

                return dt;
            }
        }

        public string Csv2Json(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            var sb = new StringBuilder();

            using (ChoCSVReader<dynamic> csv = this.ChoEtlSetupCsv(new FileInfo(filePath).FullName, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8)) {
                IEnumerable<Dictionary<string, object>> cleaned = csv.Select(record => {
                    var dict = new Dictionary<string, object>();
                    foreach (dynamic kvp in record) {
                        dict[kvp.Key] = kvp.Value;
                        if (dict[kvp.Key] is string s) {
                            if (s.Contains("\"\"")) {
                                dict[kvp.Key] = s.Replace("\"\"", "\"");
                            }
                        }
                    }

                    return dict;
                });

                using (var w = new ChoJSONWriter(sb)) {
                    w.Write(cleaned);
                }
            }

            return sb.ToString();
        }

        public IDataReader Csv2DataReader(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            return this.ChoEtlSetupCsv(new FileInfo(filePath).FullName, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8).AsDataReader();
        }

        public IEnumerable<T> Csv2Enumerable<T>(string filePath, string delimiter, List<CCsvColumn> csvColumn = null, string nullValue = "", string eolDelimiter = null, Encoding encoding = null) {
            using (IDataReader dr = this.Csv2DataReader(filePath, delimiter, csvColumn, nullValue, eolDelimiter, encoding ?? Encoding.UTF8)) {
                PropertyInfo[] properties = typeof(T).GetProperties();

                while (dr.Read()) {
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
                                if (val is string s) {
                                    if (s.Contains("\"\"")) {
                                        val = s.Replace("\"\"", "\"");
                                    }
                                }

                                // MENGGUNAKAN HELPER BARU: Menghindari crash IConvertible larik biner
                                val = ChangeType(val, pro.PropertyType);
                                pro.SetValue(objT, val);
                            }
                        }
                    }

                    yield return objT;
                }
            }
        }

        private static object ChangeType(object value, Type targetType) {
            if (value == null) {
                return null;
            }

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Jika target bertipe array byte (byte[] atau byte?[]), lakukan dekode otomatis dari string Base64 CSV
            bool isTargetByteArray = underlyingType.IsArray &&
                                     (underlyingType.GetElementType() == typeof(byte) || Nullable.GetUnderlyingType(underlyingType.GetElementType()) == typeof(byte));

            if (isTargetByteArray && value is string base64Str) {
                if (string.IsNullOrEmpty(base64Str)) return null;
                try {
                    byte[] rawBytes = Convert.FromBase64String(base64Str);
                    if (targetType == typeof(byte?[])) {
                        return rawBytes.Select(b => (byte?)b).ToArray();
                    }
                    return rawBytes;
                }
                catch {
                    byte[] textBytes = Encoding.UTF8.GetBytes(base64Str);
                    if (targetType == typeof(byte?[])) {
                        return textBytes.Select(b => (byte?)b).ToArray();
                    }
                    return textBytes;
                }
            }

            if (targetType.IsAssignableFrom(value.GetType()) || underlyingType.IsAssignableFrom(value.GetType())) {
                return value;
            }

            // Larik biner byte[] <-> byte?[]
            if (value is byte[] rawBytes2 && targetType == typeof(byte?[])) {
                return rawBytes2.Select(b => (byte?)b).ToArray();
            }
            if (value is byte?[] nullableBytes && targetType == typeof(byte[])) {
                return nullableBytes.Select(b => b ?? (byte)0).ToArray();
            }

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
