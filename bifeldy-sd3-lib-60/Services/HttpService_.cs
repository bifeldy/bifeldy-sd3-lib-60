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
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IHttpService {
        HttpClient CreateHttpClient(uint timeoutSeconds = 60);
        Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300);
        Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
        Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null);
        Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
        Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
        Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
        Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
        Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
        Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
        Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null);
    }

    [SingletonServiceRegistration]
    public sealed class CHttpService : IHttpService {

        private readonly ILogger<CHttpService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
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

        public CHttpService(ILogger<CHttpService> logger, IHttpClientFactory httpClientFactory, IConverterService cs) {
            this._logger = logger;
            this._httpClientFactory = httpClientFactory;
            this._cs = cs;
        }

        private async Task<HttpContent> GetHttpContent(dynamic httpContent, string contentType, Encoding encoding = null) {
            HttpContent content = null;

            encoding ??= Encoding.UTF8;

            if (httpContent.GetType() == typeof(string)) {
                content = new StringContent(httpContent, encoding, contentType);
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
                content = new StringContent(this._cs.ObjectToJson(httpContent), encoding, contentType);
            }

            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            return content;
        }

        private async Task<HttpRequestMessage> ParseApiData(
            string httpUri, HttpMethod httpMethod, dynamic httpContent = null,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null
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
                                    contentType?.Length > 0 ? contentType[i] : "application/octet-stream",
                                    encoding ?? Encoding.UTF8
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

        private async Task<HttpResponseMessage> SendWithRetry(
            string httpUri, HttpMethod httpMethod, dynamic httpContent = null,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null,
            uint timeoutSeconds = 180, uint maxRetry = 3,
            HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead
        ) {
            HttpClient httpClient = this.CreateHttpClient(timeoutSeconds);

            HttpResponseMessage httpResponseMessage = null;
            HttpRequestMessage httpRequestMessage = null;

            for (int retry = 0; retry < maxRetry; retry++) {
                httpRequestMessage = await this.ParseApiData(
                    httpUri, httpMethod, httpContent,
                    multipart, httpHeaders,
                    contentKeyName, contentType,
                    encoding ?? Encoding.UTF8
                );

                httpRequestMessage.Headers.Add("x-retry-number", $"{retry}");

                try {
                    httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, readOpt);

                    if (((int)httpResponseMessage.StatusCode) < 500) {
                        break;
                    }
                }
                catch (Exception ex) {
                    this._logger.LogError("[HTTP_REQUEST_{method}] {ex}", httpRequestMessage.Method.Method, ex.Message);
                }
                finally {
                    await Task.Delay(Math.Min((int)timeoutSeconds / (int)maxRetry * retry, 5 * retry) * 1000);
                }
            }

            return httpResponseMessage;
        }

        public HttpClient CreateHttpClient(uint timeoutSeconds = 60) {
            HttpClient httpClient = this._httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            return httpClient;
        }

        public async Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300) {
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

            HttpResponseMessage res = await this.CreateHttpClient(timeoutSeconds).SendAsync(
                await this.ParseApiData(
                    urlTarget,
                    new HttpMethod(request.Method),
                    request,
                    httpHeaders: lsHeader,
                    contentType: new string[] {
                        request.ContentType
                    }
                ),
                HttpCompletionOption.ResponseHeadersRead
            );

            int statusCode = (int) res.StatusCode;

            response.Clear();
            response.StatusCode = statusCode;

            if (statusCode == 404 && (isApiEndpoint || urlTarget.Contains("/api/"))) {
                return new NotFoundObjectResult(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "404 - Whoops :: Alamat Server Tujuan Tidak Ditemukan",
                    result = new ResponseJsonMessage() {
                        message = $"Silahkan Periksa Kembali Dokumentasi API"
                    }
                });
            }
            else if (statusCode == 502 && (isApiEndpoint || urlTarget.Contains("/api/"))) {
                return new BadRequestObjectResult(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "502 - Whoops :: Alamat Server Tujuan Tidak Tersedia",
                    result = new ResponseJsonMessage() {
                        message = $"Silahkan Hubungi S/SD 3 Untuk informasi Lebih Lanjut"
                    }
                });
            }
            else {
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

                Stream stream = await res.Content.ReadAsStreamAsync();
                return new FileStreamResult(stream, res.Content.Headers.ContentType.MediaType);
            }
        }

        public async Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Head, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

        public async Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Get, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt);
        }

        public async Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Delete, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

        public async Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Post, objBody, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

        public async Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Put, objBody, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

        public async Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("CONNECT"), httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

        public async Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("OPTIONS"), httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

        public async Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("PATCH"), objBody, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

        public async Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Trace, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry);
        }

    }

}
