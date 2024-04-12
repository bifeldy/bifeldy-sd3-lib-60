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

using System.Drawing;
using System.Reflection;
using System.Runtime.Versioning;

using DinkToPdf;
using DinkToPdf.Contracts;

using Newtonsoft.Json;

namespace bifeldy_sd3_lib_60.Services {

    public interface IConverterService {
        byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument);
        byte[] ImageToByte(Image x);
        Image ByteToImage(byte[] byteArray);
        T JsonToObject<T>(string j2o);
        string ObjectToJson(object body);
        T GetDefaultValueT<T>();
        string FormatByteSizeHumanReadable(long bytes, string forceUnit = null);
        Dictionary<string, T> ClassToDictionary<T>(object obj);
    }

    public sealed class CConverterService : IConverterService {

        private readonly IConverter _converter;

        public CConverterService(IConverter converter) {
            _converter = converter;
        }

        public byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument) {
            return _converter.Convert(htmlToPdfDocument);
        }

        [SupportedOSPlatform("windows")]
        public byte[] ImageToByte(Image image) {
            return (byte[]) new ImageConverter().ConvertTo(image, typeof(byte[]));
        }

        [SupportedOSPlatform("windows")]
        public Image ByteToImage(byte[] byteArray) {
            return (Bitmap) new ImageConverter().ConvertFrom(byteArray);
        }

        public T JsonToObject<T>(string j2o) {
            return JsonConvert.DeserializeObject<T>(j2o);
        }

        public string ObjectToJson(object o2j) {
            return JsonConvert.SerializeObject(o2j);
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

        public string FormatByteSizeHumanReadable(long bytes, string forceUnit = null) {
            IDictionary<string, long> dict = new Dictionary<string, long> {
                { "TB", 1000000000000 },
                { "GB", 1000000000 },
                { "MB", 1000000 },
                { "KB", 1000 },
                { "B", 1 }
            };
            long digit = 1;
            string ext = "B";
            if (!string.IsNullOrEmpty(forceUnit)) {
                digit = dict[forceUnit];
                ext = forceUnit;
            }
            else {
                foreach (KeyValuePair<string, long> kvp in dict) {
                    if (bytes > kvp.Value) {
                        digit = kvp.Value;
                        ext = kvp.Key;
                        break;
                    }
                }
            }
            return $"{((decimal)bytes / digit):0.00} {ext}";
        }

        public Dictionary<string, T> ClassToDictionary<T>(object obj) {
            return obj.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(prop => prop.Name, prop => {
                        try {
                            dynamic data = prop.GetValue(obj, null);
                            if (typeof(T) == typeof(string)) {
                                if (data.GetType() == typeof(DateTime)) {
                                    data = ((DateTime) data).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                                }
                                if (typeof(T) == typeof(object)) {
                                    data = ObjectToJson(data);
                                }
                                else {
                                    data = $"{data}";
                                }
                            }
                            return (T) data;
                        }
                        catch {
                            return GetDefaultValueT<T>();
                        }
                    });
        }

    }

}
