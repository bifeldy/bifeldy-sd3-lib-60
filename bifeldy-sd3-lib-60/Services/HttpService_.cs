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

using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Libraries;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IHttpService {
        List<Tuple<string, string>> CleanHeader(IHeaderDictionary httpHeader);
        HttpClient CreateHttpClient(uint timeoutSeconds = 60, string publicKeysBase64HashJsonFilePath = null);
        Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300, string publicKeysBase64HashJsonFilePath = null);
        IAsyncEnumerable<T> ReadStreamingJsonAsync<T>(HttpResponseMessage response, JsonSerializerOptions options = null, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
        Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null);
    }

    [SingletonServiceRegistration]
    public sealed class CHttpService : IHttpService {

        private readonly ILogger<CHttpService> _logger;
        private readonly IConverterService _cs;

        private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase) {
            // RFC 7230 / RFC 9110
            "Connection",
            "Proxy-Connection",
            "Keep-Alive",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
    
            // Wildcards (never forward)
            "Proxy-*",
            "Sec-*"
        };


        private static readonly HashSet<string> RequestHeadersToRemove = new(StringComparer.OrdinalIgnoreCase) {
            "Host",               // replaced by HttpClient
            "Content-Length",     // recalculated by HttpClient
            "Content-Encoding",   // ASP.NET may already decompress
            "Transfer-Encoding",  // HttpClient will decide
            "Expect"              // avoid 100-continue problems

            /* "x-real-ip", "cf-connecting-ip", */

            // Optional: depends on your proxy policy
            // "Accept-Encoding", // do not forward if proxy wants to decompress
            // "User-Agent",
            // "Forwarded", "X-Forwarded-*"
        };

        private static readonly HashSet<string> ResponseHeadersToRemove = new(StringComparer.OrdinalIgnoreCase) {
            "Transfer-Encoding",
            "Content-Length",
            "Connection",
            "Keep-Alive",
            "Proxy-Connection",
            "Trailer"
        };

        public CHttpService(ILogger<CHttpService> logger, IConverterService cs) {
            this._logger = logger;
            this._cs = cs;
        }

        public List<Tuple<string, string>> CleanHeader(IHeaderDictionary headers) {
            var list = new List<Tuple<string, string>>();

            foreach (KeyValuePair<string, StringValues> header in headers) {
                if (RequestHeadersToRemove.Contains(header.Key)) {
                    continue;
                }

                if (HopByHopHeaders.Any(p => HeaderMatches(p, header.Key))) {
                    continue;
                }

                list.Add(Tuple.Create(header.Key, header.Value.ToString()));
            }

            return list;
        }

        public HttpContent CreateStreamingJsonContent(IAsyncEnumerable<object> stream, JsonSerializerOptions jsonOpt = null) {
            jsonOpt ??= new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return new StreamContent(new StreamingJson(stream, jsonOpt)) {
                Headers = {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            };
        }

        public HttpContent CreateNdjsonContent(IAsyncEnumerable<object> stream, JsonSerializerOptions jsonOpt = null) {
            jsonOpt ??= new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return new StreamContent(new StreamingNdjson(stream, jsonOpt)) {
                Headers = {
                    ContentType = new MediaTypeHeaderValue("application/x-ndjson")
                }
            };
        }

        private HttpContent GetHttpContent(dynamic httpContent, string contentType, Encoding encoding = null) {
            encoding ??= Encoding.UTF8;

            Type type = httpContent.GetType();

            if (type == typeof(string)) {
                return new StringContent(httpContent, encoding, contentType);
            }
            else if (type == typeof(byte[])) {
                return new ByteArrayContent(httpContent);
            }
            else if (type == typeof(Stream) || type == typeof(HttpRequest)) {
                HttpContent streamContent;

                if (type == typeof(HttpRequest)) {
                    var req = (HttpRequest)httpContent;
                    streamContent = new StreamContent(req.Body);

                    if (!string.IsNullOrEmpty(req.ContentType)) {
                        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(req.ContentType);
                    }

                    streamContent.Headers.ContentLength = null;
                    streamContent.Headers.ContentEncoding.Clear();
                }
                else {
                    streamContent = new StreamContent((Stream)httpContent);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }

                return streamContent;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)) {
                string ct = contentType?.ToLowerInvariant();

                if (ct == "application/json") {
                    return this.CreateStreamingJsonContent(httpContent);
                }

                if (ct == "application/x-ndjson") {
                    return this.CreateNdjsonContent(httpContent);
                }

                throw new PluginGagalProsesException($"Streaming Untuk Content-Type '{contentType}' Tidak Tersedia");
            }
            else {
                dynamic json = JsonSerializer.Serialize(httpContent);
                return new StringContent(json, encoding, contentType);
            }
        }

        private HttpRequestMessage ParseApiData(
            string httpUri, HttpMethod httpMethod, dynamic httpContent = null,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null
        ) {
            encoding ??= Encoding.UTF8;

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
                                GetHttpContent(
                                    httpContent[i],
                                    contentType?.Length > 0 ? contentType[i] : "application/octet-stream",
                                    encoding
                                )
                            );
                        }
                    }
                    else {
                        lsContent.Add(
                            GetHttpContent(
                                httpContent,
                                contentType?.Length > 0 ? contentType[0] : "application/octet-stream",
                                encoding
                            )
                        );
                    }

                    httpContent = new MultipartFormDataContent();
                    for (int i = 0; i < lsContent.Count; i++) {
                        httpContent.Add(lsContent[i], contentKeyName?.Length > 0 ? contentKeyName[i] : "file");
                    }
                }
                else {
                    httpContent = GetHttpContent(
                        httpContent,
                        contentType?.Length > 0 ? contentType[0] : "application/json",
                        encoding
                    );
                }

                httpRequestMessage.Content = httpContent;
            }

            if (httpHeaders != null) {
                foreach (Tuple<string, string> hdr in httpHeaders) {
                    try {
                        if (!httpRequestMessage.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2)) {
                            _ = httpRequestMessage.Content?.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2);
                        }
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
            HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead,
            string publicKeysBase64HashJsonFilePath = null
        ) {
            HttpClient httpClient = this.CreateHttpClient(timeoutSeconds, publicKeysBase64HashJsonFilePath);

            HttpResponseMessage httpResponseMessage = null;
            HttpRequestMessage httpRequestMessage = null;

            for (int retry = 0; retry < maxRetry; retry++) {
                httpRequestMessage = this.ParseApiData(
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

        public HttpClient CreateHttpClient(uint timeoutSeconds = 60, string publicKeysBase64HashJsonFilePath = null) {
            var httpMessageHandler = new HttpClientHandler() {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            if (!string.IsNullOrEmpty(publicKeysBase64HashJsonFilePath)) {
                string json = File.ReadAllText(publicKeysBase64HashJsonFilePath);

                List<string> lsJson = this._cs.JsonToObject<List<string>>(json);
                var pinnedPublicKeys = new HashSet<string>(lsJson, StringComparer.OrdinalIgnoreCase);

                httpMessageHandler.ServerCertificateCustomValidationCallback = (httpRequestMessage, x509Certificate2, x509Chain, sslPolicyErrors) => {
                    if (sslPolicyErrors == SslPolicyErrors.None) {
                        byte[] serverPublicKey = x509Certificate2.GetPublicKey();

                        using (var sha256 = SHA256.Create()) {
                            byte[] hash = sha256.ComputeHash(serverPublicKey);
                            string base64Hash = Convert.ToBase64String(hash);

                            if (pinnedPublicKeys.Contains(base64Hash)) {
                                return true;
                            }
                        }

                        foreach (X509ChainElement element in x509Chain.ChainElements) {
                            byte[] chainServerPublicKey = element.Certificate.GetPublicKey();

                            using (var sha256 = SHA256.Create()) {
                                byte[] hash = sha256.ComputeHash(chainServerPublicKey);
                                string base64Hash = Convert.ToBase64String(hash);

                                if (pinnedPublicKeys.Contains(base64Hash)) {
                                    return true;
                                }
                            }
                        }
                    }

                    return false;
                };
            }

            return new HttpClient(httpMessageHandler) {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
        }

        private static bool HeaderMatches(string pattern, string header) {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(header)) {
                return false;
            }

            pattern = pattern.ToLowerInvariant();
            header = header.ToLowerInvariant();

            if (pattern.EndsWith("*")) {
                string prefix = pattern[..^1];
                return header.StartsWith(prefix);
            }

            return header == pattern;
        }

        public async Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300, string publicKeysBase64HashJsonFilePath = null) {
            List<Tuple<string, string>> lsHeader = this.CleanHeader(request.Headers);

            HttpRequestMessage forwardMsg = this.ParseApiData(
                urlTarget,
                new HttpMethod(request.Method),
                request,
                httpHeaders: lsHeader,
                contentType: request.ContentType != null ? new[] { request.ContentType } : null
            );

            HttpResponseMessage res = await this.CreateHttpClient(timeoutSeconds, publicKeysBase64HashJsonFilePath)
                .SendAsync(forwardMsg, HttpCompletionOption.ResponseHeadersRead);

            int statusCode = (int)res.StatusCode;

            response.Clear();
            response.StatusCode = statusCode;

            if (statusCode == 404 && (isApiEndpoint || urlTarget.Contains($"/{Bifeldy.API_PREFIX}/"))) {
                return new NotFoundObjectResult(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "404 - Whoops :: Alamat Server Tujuan Tidak Ditemukan",
                    result = new ResponseJsonMessage() {
                        message = "Silahkan Periksa Kembali Dokumentasi API"
                    }
                });
            }
            else if (statusCode == 502 && (isApiEndpoint || urlTarget.Contains($"/{Bifeldy.API_PREFIX}/"))) {
                return new BadRequestObjectResult(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = "502 - Whoops :: Alamat Server Tujuan Tidak Tersedia",
                    result = new ResponseJsonMessage() {
                        message = "Silahkan Hubungi S/SD 3 Untuk informasi Lebih Lanjut"
                    }
                });
            }

            KeyValuePair<string, IEnumerable<string>>[] allHeaders = res.Headers
                .Concat(res.Content.Headers)
                .ToArray();

            string[] blockedHeaders = HopByHopHeaders
                .Union(ResponseHeadersToRemove)
                .ToArray();

            foreach (KeyValuePair<string, IEnumerable<string>> header in allHeaders) {
                string hdrKey = header.Key;

                // Skip prohibited OR hop-by-hop headers
                if (blockedHeaders.Any(b => HeaderMatches(b, hdrKey))) {
                    continue;
                }

                response.Headers[hdrKey] = header.Value.ToArray();
            }

            Stream stream = await res.Content.ReadAsStreamAsync();
            string mediaType = res.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            return new FileStreamResult(stream, mediaType);
        }

        public async IAsyncEnumerable<T> ReadStreamingJsonAsync<T>(HttpResponseMessage response, JsonSerializerOptions options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            if (response == null) {
                throw new PluginGagalProsesException("Response Tidak Ada Isinya");
            }

            if (!response.IsSuccessStatusCode) {
                throw new PluginGagalProsesException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            options ??= new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            };

            Stream stream = await response.Content.ReadAsStreamAsync();

            if (response.Content.Headers.ContentType?.MediaType == "application/json") {
                await foreach (T item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, options, cancellationToken)) {
                    if (item != null) {
                        yield return item;
                    }
                }

                yield break;
            }

            if (response.Content.Headers.ContentType?.MediaType == "application/x-ndjson") {
                using (var reader = new StreamReader(stream)) {
                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested) {
                        string line = await reader.ReadLineAsync();

                        if (!string.IsNullOrWhiteSpace(line)) {
                            T item = default;

                            try {
                                item = JsonSerializer.Deserialize<T>(line, options);
                            }
                            catch {
                                throw new PluginGagalProsesException("Format X-(ND)JSON Harus Per Baris 1 Object Lengkap");
                            }

                            if (item != null) {
                                yield return item;
                            }
                        }
                    }
                }

                yield break;
            }

            throw new PluginGagalProsesException($"Streaming Untuk Content-Type '{response.Content.Headers.ContentType?.MediaType}' Tidak Tersedia");
        }

        public async Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Head, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Get, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Delete, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> PostData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Post, objBody, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> PutData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Put, objBody, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("CONNECT"), httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("OPTIONS"), httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> PatchData(string urlPath, dynamic objBody, bool multipart = false, List<Tuple<string, string>> headerOpts = null, string[] contentKeyName = null, string[] contentType = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, new HttpMethod("PATCH"), objBody, multipart, headerOpts, contentKeyName, contentType, encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

        public async Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null) {
            return await this.SendWithRetry(urlPath, HttpMethod.Trace, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath);
        }

    }

}
