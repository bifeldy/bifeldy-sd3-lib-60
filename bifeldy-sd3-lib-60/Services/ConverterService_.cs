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

    }

}
