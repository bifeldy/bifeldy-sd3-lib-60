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
using System.Net.Mime;
using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;

using DinkToPdf;
using DinkToPdf.Contracts;

using Newtonsoft.Json;

namespace bifeldy_sd3_lib_60.Services {

    public interface IConverterService {
        byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument);
        byte[] ImageToByte(Image x);
        Image ByteToImage(byte[] byteArray);
        T JsonToObject<T>(string j2o);
        string JsonToXml(string json);
        string ObjectToJson(object body);
        string XmlToJson(string xml);
        T XmlJsonToObject<T>(string type, string text);
        string FormatByteSizeHumanReadable(long bytes, string forceUnit = null);
        Dictionary<string, T> ClassToDictionary<T>(object obj);
        List<T> Csv2List<T>(Stream stream, string delimiter = ",", bool skipHeader = false, List<string> csvColumn = null, List<string> requiredColumn = null);
    }

    public sealed class CConverterService : IConverterService {

        private readonly IConverter _converter;

        public CConverterService(IConverter converter) {
            this._converter = converter;
        }

        public byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument) => this._converter.Convert(htmlToPdfDocument);

        [SupportedOSPlatform("windows")]
        public byte[] ImageToByte(Image image) => (byte[]) new ImageConverter().ConvertTo(image, typeof(byte[]));

        [SupportedOSPlatform("windows")]
        public Image ByteToImage(byte[] byteArray) => (Bitmap) new ImageConverter().ConvertFrom(byteArray);

        public T JsonToObject<T>(string j2o) => JsonConvert.DeserializeObject<T>(j2o);

        public string ObjectToJson(object o2j) => JsonConvert.SerializeObject(o2j);

        public string XmlToJson(string xml) {
            var xdoc = XDocument.Parse(xml);
            xdoc.Declaration = null;
            return JsonConvert.SerializeXNode(xdoc, Formatting.None, true);
        }

        public string JsonToXml(string json) => JsonConvert.DeserializeXmlNode(json, "root").ToString();

        public T XmlJsonToObject<T>(string type, string text) {
            switch (type) {
                case MediaTypeNames.Application.Xml:
                    text = this.XmlToJson(text);
                    goto case MediaTypeNames.Application.Json;
                case MediaTypeNames.Application.Json:
                    return this.JsonToObject<T>(text);
                default:
                    throw new NotImplementedException("No Type Available!");
            }
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

            return $"{(decimal) bytes / digit:0.00} {ext}";
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
                            return default;
                        }
                    });
        }

        public List<T> Csv2List<T>(Stream stream, string delimiter = ",", bool skipHeader = false, List<string> csvColumn = null, List<string> requiredColumn = null) {
            using (var reader = new StreamReader(stream)) {
                int i = 0;
                List<string> col = csvColumn ?? new();
                var row = new List<T>();

                if (skipHeader && csvColumn != null) {
                    i++;
                    _ = reader.ReadLine();
                }

                while (!reader.EndOfStream) {
                    string? line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line)) {
                        string[] values = line.Split(delimiter).Select(v => v.StartsWith("\"") && v.EndsWith("\"") ? v[1..^1] : v).ToArray();

                        if (i == 0) {
                            if (csvColumn == null) {
                                col.AddRange(values);
                            }
                            else {
                                var temp = new List<string>();
                                for (int j = 0; j < values.Length; j++) {
                                    if (csvColumn.Select(ac => ac.ToUpper()).Contains(values[j].ToUpper())) {
                                        temp.Add(values[j]);
                                    }
                                }

                                if (temp.Count != col.Count) {
                                    throw new Exception("Data kolom yang tersedia tidak lengkap");
                                }

                                col = temp;
                            }
                        }
                        else if (values.Length != col.Count) {
                            throw new Exception("Jumlah kolom data tidak sesuai dengan kolom header");
                        }
                        else {
                            PropertyInfo[] properties = typeof(T).GetProperties();
                            T objT = Activator.CreateInstance<T>();

                            for (int j = 0; j < col.Count; j++) {
                                string colName = col[j].ToUpper();
                                string rowVal = values[j].ToUpper();

                                if (csvColumn != null && requiredColumn != null) {
                                    if (requiredColumn.Select(rc => rc.ToUpper()).Contains(colName)) {
                                        if (string.IsNullOrEmpty(rowVal)) {
                                            throw new Exception($"Baris {i + 1} kolom {j} :: {colName} tidak boleh kosong");
                                        }
                                    }
                                }

                                foreach (PropertyInfo pro in properties) {
                                    if (pro.Name.ToUpper() == colName) {
                                        try {
                                            pro.SetValue(objT, rowVal);
                                        }
                                        catch {
                                            //
                                        }
                                    }
                                }
                            }

                            row.Add(objT);
                        }
                    }

                    i++;
                }

                return row;
            }
        }

    }

}
