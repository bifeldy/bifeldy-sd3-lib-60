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

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Libraries;
using bifeldy_sd3_lib_60.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace bifeldy_sd3_lib_60.Services {

    public interface IHttpService {
        List<Tuple<string, string>> CleanHeader(IHeaderDictionary httpHeader);
        HttpClient CreateHttpClient(uint timeoutSeconds = 60, string publicKeysBase64HashJsonFilePath = null);
        Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300, string publicKeysBase64HashJsonFilePath = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<T> ReadStreamingJsonAsync<T>(HttpResponseMessage response, int bufferSize = 32768, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> PostData(string urlPath, object objBody, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> PutData(string urlPath, object objBody, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> PatchData(string urlPath, object objBody, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
        Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default);
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
            "Content-Type",       // recreated by HttpClient
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
                        byte[] hash = SHA256.HashData(serverPublicKey);
                        string base64Hash = Convert.ToBase64String(hash);

                        if (pinnedPublicKeys.Contains(base64Hash)) {
                            return true;
                        }

                        foreach (X509ChainElement element in x509Chain.ChainElements) {
                            byte[] chainServerPublicKey = element.Certificate.GetPublicKey();
                            hash = SHA256.HashData(chainServerPublicKey);
                            base64Hash = Convert.ToBase64String(hash);

                            if (pinnedPublicKeys.Contains(base64Hash)) {
                                return true;
                            }
                        }
                    }

                    return false;
                };
            }

            return new HttpClient(httpMessageHandler) {
                Timeout = timeoutSeconds <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(timeoutSeconds)
            };
        }

        private HttpContent GetHttpContent(object payload, string contentType, Encoding encoding = null) {
            encoding ??= Encoding.UTF8;

            if (payload == null) {
                return null;
            }

            if (payload is byte[] b) {
                return new ByteArrayContent(b);
            }

            if (payload is HttpRequestMessage reqMsg) {
                return reqMsg.Content;
            }

            if (payload is HttpRequest req) {
                var streamContent = new StreamContent(req.Body);

                if (!string.IsNullOrEmpty(req.ContentType)) {
                    streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(req.ContentType);
                }

                streamContent.Headers.ContentLength = null;
                streamContent.Headers.ContentEncoding.Clear();

                return streamContent;
            }

            if (payload is Stream stream) {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                return streamContent;
            }

            if (payload is not string) {
                string json = this._cs.ObjectToJson(payload);
                return new StringContent(json, encoding, MediaTypeNames.Application.Json);
            }

            return new StringContent(payload.ToString(), encoding, contentType);
        }

        private HttpRequestMessage ParseApiData(
            string httpUri, HttpMethod httpMethod, object payload,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null
        ) {
            var request = new HttpRequestMessage() {
                Method = httpMethod,
                RequestUri = new Uri(httpUri)
            };

            if (payload != null) {
                HttpContent finalContent = null;

                if (multipart) {
                    var form = new MultipartFormDataContent();

                    if (payload is object[] arr) {
                        for (int i = 0; i < arr.Length; i++) {
                            HttpContent part = this.GetHttpContent(
                                arr.GetValue(i),
                                contentType?.ElementAtOrDefault(i) ?? MediaTypeNames.Application.Octet,
                                encoding
                            );

                            form.Add(
                                part,
                                contentKeyName?.ElementAtOrDefault(i) ?? "file"
                            );
                        }
                    }
                    else {
                        form.Add(
                            this.GetHttpContent(
                                payload,
                                contentType?.FirstOrDefault() ?? MediaTypeNames.Application.Octet,
                                encoding
                            ),
                            contentKeyName?.FirstOrDefault() ?? "file"
                        );
                    }

                    finalContent = form;
                }
                else {
                    finalContent = this.GetHttpContent(
                        payload,
                        contentType?.FirstOrDefault() ?? MediaTypeNames.Application.Json,
                        encoding
                    );
                }

                request.Content = finalContent;
            }

            if (httpHeaders != null) {
                foreach (Tuple<string, string> hdr in httpHeaders) {
                    if (!request.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2)) {
                        _ = (request.Content?.Headers.TryAddWithoutValidation(hdr.Item1, hdr.Item2));
                    }
                }
            }

            return request;
        }

        private async Task<HttpResponseMessage> SendWithRetry(
            string httpUri, HttpMethod httpMethod, object httpContent,
            bool multipart = false, List<Tuple<string, string>> httpHeaders = null,
            string[] contentKeyName = null, string[] contentType = null,
            Encoding encoding = null,
            uint timeoutSeconds = 180, uint maxRetry = 3,
            HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead,
            string publicKeysBase64HashJsonFilePath = null,
            CancellationToken token = default
        ) {
            HttpClient httpClient = this.CreateHttpClient(timeoutSeconds, publicKeysBase64HashJsonFilePath);

            HttpResponseMessage httpResponseMessage = null;

            if (httpContent is Stream or HttpRequest or HttpRequestMessage) {
                maxRetry = 1;
            }

            for (int retry = 0; retry < maxRetry; retry++) {
                using (HttpRequestMessage httpRequestMessage = this.ParseApiData(
                    httpUri, httpMethod, httpContent,
                    multipart, httpHeaders,
                    contentKeyName, contentType,
                    encoding ?? Encoding.UTF8
                )) {
                    httpRequestMessage.Headers.Add("x-retry-number", $"{retry}");

                    try {
                        httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, readOpt, token);

                        if (((int)httpResponseMessage.StatusCode) < 500) {
                            break;
                        }
                    }
                    catch (Exception ex) {
                        this._logger.LogError("[HTTP_REQUEST_{method}] {ex}", httpRequestMessage.Method.Method, ex.Message);
                    }
                    finally {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Min(5 * retry, timeoutSeconds / maxRetry)), token);
                    }
                }
            }

            return httpResponseMessage;
        }

        private static bool HeaderMatches(string pattern, string header) {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(header)) {
                return false;
            }

            pattern = pattern.ToLowerInvariant();
            header = header.ToLowerInvariant();

            if (pattern.EndsWith('*')) {
                string prefix = pattern[..^1];
                return header.StartsWith(prefix);
            }

            return header == pattern;
        }

        public async Task<IActionResult> ForwardRequest(string urlTarget, HttpRequest request, HttpResponse response, bool isApiEndpoint = false, uint timeoutSeconds = 300, string publicKeysBase64HashJsonFilePath = null, CancellationToken cancellationToken = default) {
            List<Tuple<string, string>> lsHeader = this.CleanHeader(request.Headers);

            HttpRequestMessage forwardMsg = this.ParseApiData(
                urlTarget,
                new HttpMethod(request.Method),
                request,
                httpHeaders: lsHeader,
                contentType: request.ContentType != null ? new string[] { request.ContentType } : null
            );

            HttpResponseMessage res = await this.CreateHttpClient(timeoutSeconds, publicKeysBase64HashJsonFilePath)
                .SendAsync(forwardMsg, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            int statusCode = (int)res.StatusCode;

            response.Clear();
            response.StatusCode = statusCode;

            if (statusCode == 404 && (isApiEndpoint || urlTarget.Contains($"/{Bifeldy.API_PREFIX}/"))) {
                var r1 = new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"{statusCode} - Whoops :: Alamat Server Tujuan Tidak Ditemukan",
                    result = new ResponseJsonMessage() {
                        message = "Silahkan Periksa Kembali Dokumentasi API"
                    }
                };

                return new NotFoundObjectResult(r1);
            }

            if (statusCode == 502 && (isApiEndpoint || urlTarget.Contains($"/{Bifeldy.API_PREFIX}/"))) {
                var jsonSerializerOptions = new JsonSerializerOptions();
                jsonSerializerOptions.Converters.Add(new DecimalSystemTextJsonConverter());
                jsonSerializerOptions.Converters.Add(new NullableDecimalSystemTextJsonConverter());

                var r2 = new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"{statusCode} - Whoops :: Alamat Server Tujuan Tidak Tersedia",
                    result = new ResponseJsonMessage() {
                        message = "Silahkan Hubungi S/SD 3 Untuk informasi Lebih Lanjut"
                    }
                };

                return new JsonResult(r2) {
                    SerializerSettings = jsonSerializerOptions,
                    ContentType = MediaTypeNames.Application.Json,
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }

            KeyValuePair<string, IEnumerable<string>>[] allHeaders = res.Headers.Concat(res.Content.Headers).ToArray();
            string[] blockedHeaders = HopByHopHeaders.Union(ResponseHeadersToRemove).ToArray();

            foreach (KeyValuePair<string, IEnumerable<string>> header in allHeaders) {
                if (!blockedHeaders.Any(b => HeaderMatches(b, header.Key))) {
                    response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            await response.StartAsync(cancellationToken);

            await using (Stream upstream = await res.Content.ReadAsStreamAsync(cancellationToken)) {
                byte[] buffer = new byte[8192];
                int bytesRead = 0;

                while ((bytesRead = await upstream.ReadAsync(buffer, cancellationToken)) > 0) {
                    await response.Body.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
            }

            return new EmptyResult();
        }

        private static async IAsyncEnumerable<T> ReadStreamingJsonAsync<T>(
            HttpResponseMessage response,
            Func<Stream, IAsyncEnumerable<T>> callbackStream,
            Func<string, T> callbackLine,
            int bufferSize = 32768,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            if (response == null) {
                throw new Exception("Response Tidak Ada Isinya");
            }

            if (!response.IsSuccessStatusCode) {
                throw new Exception($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                MediaTypeNames.Application.Json,
                StringComparison.OrdinalIgnoreCase
            )) {
                IAsyncEnumerable<T> iae = callbackStream(stream);
                await foreach (T item in iae.WithCancellation(cancellationToken)) {
                    if (item != null) {
                        yield return item;
                    }
                }

                yield break;
            }

            if (string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                "application/x-ndjson",
                StringComparison.OrdinalIgnoreCase
            )) {
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, bufferSize, true)) {
                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested) {
                        string line = await reader.ReadLineAsync();

                        if (!string.IsNullOrEmpty(line)) {
                            T item = default;

                            try {
                                item = callbackLine(line);
                            }
                            catch {
                                throw new Exception("Format X-(ND)JSON Harus Per Baris 1 Object Lengkap");
                            }

                            if (item != null) {
                                yield return item;
                            }
                        }
                    }
                }

                yield break;
            }

            throw new Exception($"Streaming Untuk Content-Type '{response.Content.Headers.ContentType?.MediaType}' Tidak Tersedia");
        }

        public async IAsyncEnumerable<T> ReadStreamingJsonAsync<T>(HttpResponseMessage response, int bufferSize = 32768, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new DecimalSystemTextJsonConverter());
            jsonSerializerOptions.Converters.Add(new NullableDecimalSystemTextJsonConverter());

            IAsyncEnumerable<T> iae = ReadStreamingJsonAsync(
                response,
                (stream) => JsonSerializer.DeserializeAsyncEnumerable<T>(stream, jsonSerializerOptions, cancellationToken),
                (line) => JsonSerializer.Deserialize<T>(line, jsonSerializerOptions),
                bufferSize,
                cancellationToken
            );

            await foreach (T ie in iae.WithCancellation(cancellationToken)) {
                yield return ie;
            }
        }

        public Task<HttpResponseMessage> HeadData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, HttpMethod.Head, null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> GetData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, HttpMethod.Get, null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> DeleteData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, HttpMethod.Delete, null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> PostData(string urlPath, object objBody, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, HttpMethod.Post, objBody, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> PutData(string urlPath, object objBody, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, HttpMethod.Put, objBody, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> ConnectData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, new HttpMethod("CONNECT"), null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> OptionsData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, new HttpMethod("OPTIONS"), null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> PatchData(string urlPath, object objBody, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, new HttpMethod("PATCH"), objBody, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

        public Task<HttpResponseMessage> TraceData(string urlPath, List<Tuple<string, string>> headerOpts = null, uint timeoutSeconds = 180, uint maxRetry = 3, HttpCompletionOption readOpt = HttpCompletionOption.ResponseContentRead, Encoding encoding = null, string publicKeysBase64HashJsonFilePath = null, CancellationToken token = default) {
            return this.SendWithRetry(urlPath, HttpMethod.Trace, null, httpHeaders: headerOpts, encoding: encoding ?? Encoding.UTF8, timeoutSeconds: timeoutSeconds, maxRetry: maxRetry, readOpt: readOpt, publicKeysBase64HashJsonFilePath: publicKeysBase64HashJsonFilePath, token: token);
        }

    }

}