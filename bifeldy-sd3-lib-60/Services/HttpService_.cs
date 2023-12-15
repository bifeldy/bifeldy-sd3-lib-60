/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: External API Call
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Net.Http.Headers;

namespace bifeldy_sd3_lib_60.Services {

    public interface IHttpService {
        Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null);
    }

    public sealed class CHttpService : IHttpService {

        private readonly IConverterService _cs;

        public CHttpService(IConverterService cs) {
            _cs = cs;
        }

        private HttpRequestMessage FetchApi(
            string httpUri, HttpMethod httpMethod,
            dynamic httpContent = null, bool multipart = false, List<Tuple<string, string>> httpHeaders = null
        ) {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage {
                Method = httpMethod,
                RequestUri = new Uri(httpUri)
            };
            if (httpContent != null) {
                if (multipart) {
                    // Send binary form data with key value
                    // file=...binary...;
                    ByteArrayContent byteArrayContent = new ByteArrayContent(httpContent);
                    byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    httpContent = new MultipartFormDataContent();
                    httpContent.Add(byteArrayContent, "file");
                }
                else {
                    // Send normal json key value
                    if (httpContent.GetType() != typeof(string)) {
                        httpContent = _cs.ObjectToJson(httpContent);
                    }
                    httpContent = new StringContent(httpContent, System.Text.Encoding.UTF8, "application/json");
                }
                httpRequestMessage.Content = httpContent;
            }
            if (httpHeaders != null) {
                foreach (Tuple<string, string> hdr in httpHeaders) {
                    httpRequestMessage.Headers.Add(hdr.Item1, hdr.Item2);
                }
            }
            return httpRequestMessage;
        }

        public async Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, HttpMethod.Head, httpHeaders: headerOpts));
        }

        public async Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, HttpMethod.Get, httpHeaders: headerOpts));
        }

        public async Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, HttpMethod.Delete, httpHeaders: headerOpts));
        }

        public async Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, HttpMethod.Post, objBody, multipart, headerOpts));
        }

        public async Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, HttpMethod.Put, objBody, multipart, headerOpts));
        }

        public async Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, new HttpMethod("CONNECT"), httpHeaders: headerOpts));
        }

        public async Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, new HttpMethod("OPTIONS"), httpHeaders: headerOpts));
        }

        public async Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, new HttpMethod("PATCH"), objBody, multipart, headerOpts));
        }

        public async Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null) {
            return await new HttpClient().SendAsync(FetchApi(urlPath, HttpMethod.Trace, httpHeaders: headerOpts));
        }

    }
}
