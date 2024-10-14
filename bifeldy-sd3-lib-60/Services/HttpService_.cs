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
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;
using System.Net.Mime;

namespace bifeldy_sd3_lib_60.Services {

    public interface IHttpService {
        Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false);
        Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null);
        Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null);
        Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null);
        Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null);
        Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null);
    }

    [SingletonServiceRegistration]
    public sealed class CHttpService : IHttpService {

        private readonly IConverterService _cs;

        private string[] ProhibitedHeaders { get; } = new string[] {
            "accept-charset", "accept-encoding", "access-control-request-headers", "access-control-request-method",
            "connection", "content-length", "cookie", "date", "dnt", "expect", "feature-policy", "host", "via",
            "keep-alive", "origin", "proxy-*", "sec-*", "referer", "te", "trailer", "transfer-encoding", "upgrade"
        };

        private string[] RequestHeadersToRemove { get; } = new string [] {
            "host", "user-agent", "accept", "accept-encoding", "content-length", "x-real-ip",
            "cf-connecting-ip", "forwarded", "x-forwarded-proto", "x-forwarded-for", "x-cloud-trace-context"
        };

        private string[] ResponseHeadersToRemove { get; } = new string [] {
            "accept-ranges", "content-length", "keep-alive", "connection",
            "content-encoding", "set-cookie"
        };

        public CHttpService(IConverterService cs) {
            this._cs = cs;
        }

        private async Task<HttpContent> GetHttpContent(dynamic httpContent, string contentType) {
            HttpContent content = null;

            if (httpContent.GetType() == typeof(string)) {
                content = new StringContent(httpContent, Encoding.UTF8, contentType);
            }
            else if (typeof(HttpRequest).IsAssignableFrom(httpContent.GetType())) {
                using (var ms = new MemoryStream()) {
                    await httpContent.Body.CopyToAsync(ms);
                    await ms.FlushAsync();
                    byte[] arr = ms.ToArray();
                    content = new ByteArrayContent(arr);
                }
            }
            else if (typeof(Stream).IsAssignableFrom(httpContent.GetType())) {
                content = new StreamContent(httpContent);
            }
            else if (httpContent.GetType() == typeof(byte[])) {
                content = new ByteArrayContent(httpContent);
            }
            else {
                content = new StringContent(this._cs.ObjectToJson(httpContent), Encoding.UTF8, contentType);
            }

            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            return content;
        }

        private async Task<HttpRequestMessage> FetchApi(
            string httpUri, HttpMethod httpMethod,
            dynamic httpContent = null, bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null
        ) {
            var httpRequestMessage = new HttpRequestMessage() {
                Method = httpMethod,
                RequestUri = new Uri(httpUri)
            };

            if (httpContent != null) {
                if (multipart) {
                    // Send binary form data with key value
                    // file=...binary...;
                    var lsContent = new List<HttpContent>();

                    if (httpContent.GetType().IsArray) {
                        for (int i = 0; i < httpContent.Length; i++) {
                            lsContent.Add(
                                await GetHttpContent(
                                    httpContent[i],
                                    contentType?.Length > 0 ? contentType[i] : "application/octet-stream"
                                )
                            );
                        }
                    }
                    else {
                        lsContent.Add(await GetHttpContent(httpContent, "application/octet-stream"));
                    }

                    httpContent = new MultipartFormDataContent();
                    for (int i = 0; i < lsContent.Count; i++) {
                        httpContent.Add(lsContent[i], contentKeyName?.Length > 0 ? contentKeyName[i] : "file");
                    }
                }
                else {
                    httpContent = await GetHttpContent(
                        httpContent,
                        contentType?.Length > 0 ? contentType[0] : "application/json"
                    );
                }

                httpRequestMessage.Content = httpContent;
            }

            if (httpHeaders != null) {
                foreach (Tuple<string, string> hdr in httpHeaders) {
                    try {
                        httpRequestMessage.Headers.Add(hdr.Item1, hdr.Item2);
                    }
                    catch {
                        // Skip Invalid Header ~
                    }
                }
            }

            return httpRequestMessage;
        }

        public async Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false) {
            string[] hdrListReq = this.ProhibitedHeaders.Union(this.RequestHeadersToRemove).ToArray();
            var lsHeader = new List<Tuple<string, string>>();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers) {
                bool isOk = true;
                foreach (string hl in hdrListReq) {
                    string h = hl.ToLower();
                    string hdrKey = header.Key.ToLower();
                    if (h.EndsWith("*")) {
                        if (hdrKey.StartsWith(h.Split("*")[0])) {
                            isOk = false;
                        }
                    }
                    else if (hdrKey == h) {
                        isOk = false;
                    }
                }

                if (isOk) {
                    lsHeader.Add(new Tuple<string, string>(header.Key, header.Value));
                }
            }

            HttpResponseMessage res = await new HttpClient().SendAsync(
                await this.FetchApi(
                    urlTarget,
                    new HttpMethod(request.Method),
                    request,
                    httpHeaders: lsHeader,
                    contentType: new string[] {
                        request.ContentType
                    }
                )
            );

            KeyValuePair<string, IEnumerable<string>>[] hdrContentListRes = res.Headers.Union(res.Content.Headers).ToArray();
            string[] hdrListRes = this.ProhibitedHeaders.Union(this.ResponseHeadersToRemove).ToArray();
            foreach (KeyValuePair<string, IEnumerable<string>> header in hdrContentListRes) {
                bool isOk = true;
                foreach (string hl in hdrListRes) {
                    string h = hl.ToLower();
                    string hdrKey = header.Key.ToLower();
                    if (h.EndsWith("*")) {
                        if (hdrKey.StartsWith(h.Split("*")[0])) {
                            isOk = false;
                        }
                    }
                    else if (hdrKey == h) {
                        isOk = false;
                    }
                }

                if (isOk) {
                    response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            int statusCode = (int) res.StatusCode;

            response.Clear();
            response.StatusCode = statusCode;

            if (statusCode == 404 && (isApiEndpoint || urlTarget.Contains("/api/"))) {
                await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonError>() {
                    info = "404 - Whoops :: Alamat Server Tujuan Tidak Ditemukan",
                    result = new ResponseJsonError() {
                        message = $"Silahkan Periksa Kembali Dokumentasi API"
                    }
                });
            }
            else if (statusCode == 502) {
                await response.WriteAsJsonAsync(new ResponseJsonSingle<ResponseJsonError>() {
                    info = "502 - Whoops :: Alamat Server Tujuan Tidak Tersedia",
                    result = new ResponseJsonError() {
                        message = $"Silahkan Hubungi S/SD 3 Untuk informasi Lebih Lanjut"
                    }
                });
            }
            else {
                await res.Content.CopyToAsync(response.Body);
            }

            // Soalnya Stream Body ~
            return null;
        }

        public async Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null) => await new HttpClient().SendAsync(await this.FetchApi(urlPath, HttpMethod.Head, httpHeaders: headerOpts));

        public async Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null) => await new HttpClient().SendAsync(await this.FetchApi(urlPath, HttpMethod.Get, httpHeaders: headerOpts));

        public async Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null) => await new HttpClient().SendAsync(await this.FetchApi(urlPath, HttpMethod.Delete, httpHeaders: headerOpts));

        public async Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null) => await new HttpClient().SendAsync(await FetchApi(urlPath, HttpMethod.Post, objBody, multipart, headerOpts, contentKeyName, contentType));

        public async Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null) => await new HttpClient().SendAsync(await FetchApi(urlPath, HttpMethod.Put, objBody, multipart, headerOpts, contentKeyName, contentType));

        public async Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null) => await new HttpClient().SendAsync(await this.FetchApi(urlPath, new HttpMethod("CONNECT"), httpHeaders: headerOpts));

        public async Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null) => await new HttpClient().SendAsync(await this.FetchApi(urlPath, new HttpMethod("OPTIONS"), httpHeaders: headerOpts));

        public async Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null) => await new HttpClient().SendAsync(await FetchApi(urlPath, new HttpMethod("PATCH"), objBody, multipart, headerOpts, contentKeyName, contentType));

        public async Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null) => await new HttpClient().SendAsync(await this.FetchApi(urlPath, HttpMethod.Trace, httpHeaders: headerOpts));

    }
}
