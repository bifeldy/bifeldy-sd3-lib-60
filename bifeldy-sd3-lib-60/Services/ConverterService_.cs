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

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Libraries;
using DinkToPdf;
using DinkToPdf.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace bifeldy_sd3_lib_60.Services {

    public interface IConverterService {
        byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument);
        byte[] ImageToByte(Image x);
        Image ByteToImage(byte[] byteArray);
        T JsonToObject<T>(string j2o, JsonSerializerSettings settings = null);
        string JsonToXml(string json);
        string ObjectToJson(object body, JsonSerializerSettings settings = null);
        T ObjectToT<T>(object o2t);
        string XmlToJson(string xml);
        T XmlJsonToObject<T>(string type, string text, JsonSerializerSettings settings = null);
        string FormatByteSizeHumanReadable(long bytes, string forceUnit = null);
        List<CDynamicClassProperty> GetTableClassStructureModel<T>();
        List<CDynamicClassPropertyV2> GetPocoStructureModel<T>();
    }

    [SingletonServiceRegistration]
    public sealed class CConverterService : IConverterService {

        private readonly IConverter _converter;

        public CConverterService(IConverter converter) {
            this._converter = converter;
        }

        public byte[] HtmlToPdf(HtmlToPdfDocument htmlToPdfDocument) => this._converter.Convert(htmlToPdfDocument);

        public byte[] ImageToByte(Image image) {
            using (var ms = new MemoryStream()) {
                image.SaveAsPng(ms);
                return ms.ToArray();
            }
        }

        public Image ByteToImage(byte[] byteArray) {
            return Image.Load(byteArray);
        }

        private List<object> JArrayToList(JArray jsonArray) {
            var result = new List<object>();

            foreach (JToken item in jsonArray) {
                switch (item.Type) {
                    case JTokenType.Object:
                        result.Add(this.JObjectToDictionary((JObject)item));
                        break;
                    case JTokenType.Array:
                        result.Add(this.JArrayToList((JArray)item));
                        break;
                    default:
                        result.Add(item.ToObject<object>());
                        break;
                }
            }

            return result;
        }

        private Dictionary<string, object> JObjectToDictionary(JObject jsonObject) {
            var result = new Dictionary<string, object>();

            foreach (JProperty property in jsonObject.Properties()) {
                string key = property.Name;
                JToken value = property.Value;

                switch (value.Type) {
                    case JTokenType.Object:
                        result[key] = this.JObjectToDictionary((JObject)value);
                        break;
                    case JTokenType.Array:
                        result[key] = this.JArrayToList((JArray)value);
                        break;
                    default:
                        result[key] = value.ToObject<object>();
                        break;
                }
            }

            return result;
        }

        public T JsonToObject<T>(string j2o, JsonSerializerSettings settings = null) {
            settings ??= new JsonSerializerSettings {
                Converters = new JsonConverter[] {
                    new DecimalNewtonsoftJsonConverter(),
                    new NullableDecimalNewtonsoftJsonConverter()
                }
            };

            if (typeof(IDictionary<string, object>).IsAssignableFrom(typeof(T))) {
                var jObject = JObject.Parse(j2o);
                return (dynamic)this.JObjectToDictionary(jObject);
            }

            return JsonConvert.DeserializeObject<T>(j2o, settings);
        }

        public string ObjectToJson(object o2j, JsonSerializerSettings settings = null) {
            settings ??= new JsonSerializerSettings {
                Converters = new JsonConverter[] {
                    new DecimalNewtonsoftJsonConverter(),
                    new NullableDecimalNewtonsoftJsonConverter()
                }
            };
            return JsonConvert.SerializeObject(o2j, settings);
        }

        public T ObjectToT<T>(object o2t) {
            if (o2t == null || o2t == DBNull.Value) {
                return default;
            }

            Type targetType = typeof(T);
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Jika tipe data asal sudah cocok atau bisa langsung di-assign, kembalikan langsung datanya
            if (targetType.IsAssignableFrom(o2t.GetType()) || underlyingType.IsAssignableFrom(o2t.GetType())) {
                return (T)o2t;
            }

            // Larik biner byte[] <-> byte?[]
            if (o2t is byte[] rawBytes && targetType == typeof(byte?[])) {
                return (T)(object)rawBytes.Select(b => (byte?)b).ToArray();
            }
            if (o2t is byte?[] nullableBytes && targetType == typeof(byte[])) {
                return (T)(object)nullableBytes.Select(b => b ?? (byte)0).ToArray();
            }

            // Jika target bertipe array byte (byte[] atau byte?[]), lakukan dekode otomatis dari string Base64
            bool isTargetByteArray = underlyingType.IsArray &&
                                     (underlyingType.GetElementType() == typeof(byte) || Nullable.GetUnderlyingType(underlyingType.GetElementType()) == typeof(byte));
            if (isTargetByteArray && o2t is string base64Str) {
                if (string.IsNullOrEmpty(base64Str)) return default;
                try {
                    byte[] bytes = Convert.FromBase64String(base64Str);
                    if (targetType == typeof(byte?[])) {
                        return (T)(object)bytes.Select(b => (byte?)b).ToArray();
                    }
                    return (T)(object)bytes;
                }
                catch {
                    byte[] bytes = Encoding.UTF8.GetBytes(base64Str);
                    if (targetType == typeof(byte?[])) {
                        return (T)(object)bytes.Select(b => (byte?)b).ToArray();
                    }
                    return (T)(object)bytes;
                }
            }

            // Gunakan TypeConverter bawaan .NET jika didukung komponen tipe tujuannya
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(o2t.GetType())) {
                return (T)converter.ConvertFrom(o2t);
            }

            if (underlyingType.IsArray) {
                return (T)o2t;
            }

            // Fallback terakhir untuk tipe primitif murni C#
            return (T)Convert.ChangeType(o2t, underlyingType);
        }

        public string XmlToJson(string xml) {
            var xdoc = XDocument.Parse(xml);
            xdoc.Declaration = null;
            return JsonConvert.SerializeXNode(xdoc, Formatting.None, true);
        }

        public string JsonToXml(string json) => JsonConvert.DeserializeXmlNode(json, "root").ToString();

        public T XmlJsonToObject<T>(string type, string text, JsonSerializerSettings settings = null) {
            switch (type) {
                case MediaTypeNames.Application.Xml:
                    text = this.XmlToJson(text);
                    goto case MediaTypeNames.Application.Json;
                case MediaTypeNames.Application.Json:
                    return this.JsonToObject<T>(text, settings);
                default:
                    throw new NotImplementedException("No Media Type / MiMe Available!");
            }
        }

        public string FormatByteSizeHumanReadable(long bytes, string forceUnit = null) {
            IDictionary<string, long> dict = new Dictionary<string, long>(StringComparer.InvariantCultureIgnoreCase) {
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

            return $"{(decimal)bytes / digit:0.00} {ext}";
        }

        public List<CDynamicClassProperty> GetTableClassStructureModel<T>() {
            var ls = new List<CDynamicClassProperty>();

            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties()) {
                Type type = Nullable.GetUnderlyingType(propertyInfo.PropertyType);

                Type dataType = type ?? propertyInfo.PropertyType;
                bool isNullable = type != null;

                if (type == null && dataType == typeof(string)) {
                    KeyAttribute primaryKey = propertyInfo.GetCustomAttribute<KeyAttribute>();
                    isNullable = primaryKey == null;
                }

                var item = new CDynamicClassProperty() {
                    ColumnName = propertyInfo.Name,
                    DataType = dataType.FullName,
                    IsNullable = isNullable
                };

                ls.Add(item);
            }

            return ls;
        }

        public List<CDynamicClassPropertyV2> GetPocoStructureModel<T>() {
            var list = new List<CDynamicClassPropertyV2>();

            foreach (PropertyInfo prop in typeof(T).GetProperties()) {
                Type type = prop.PropertyType;

                // Nullable<T> detection
                bool isNullable = Nullable.GetUnderlyingType(type) != null;
                Type coreType = isNullable ? Nullable.GetUnderlyingType(type) : type;

                bool isList =
                    coreType.IsGenericType &&
                    coreType.GetGenericTypeDefinition() == typeof(List<>);

                bool isDictionary =
                    coreType.IsGenericType &&
                    coreType.GetGenericTypeDefinition() == typeof(Dictionary<,>);

                bool isArray = coreType.IsArray;
                bool isEnum = coreType.IsEnum;

                bool isClass =
                    coreType.IsClass &&
                    coreType != typeof(string) &&
                    !isDictionary &&
                    !isList &&
                    !isArray &&
                    !isEnum;

                var item = new CDynamicClassPropertyV2() {
                    ColumnName = prop.Name,
                    TypeName = coreType.FullName,
                    IsNullable = isNullable,
                    IsArray = isArray,
                    IsList = isList,
                    IsDictionary = isDictionary,
                    IsClass = isClass
                };

                list.Add(item);
            }

            return list;
        }

    }

}
