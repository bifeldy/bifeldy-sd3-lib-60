/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Default Service
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IGlobalService {
        List<string> AllowedIpOrigin { get; set; }
        string GetSecretData(HttpRequest request, RequestJson reqBody);
        string GetApiKeyData(HttpRequest request, RequestJson reqBody);
        string GetIpOriginData(ConnectionInfo connection, HttpRequest request);
        string CleanIpOrigin(string ipOrigin);
        string GetTokenData(HttpRequest request, RequestJson reqBody);
        Task<RequestJson> GetRequestBody(HttpRequest request);
    }

    [SingletonServiceRegistration]
    public sealed class CGlobalService : IGlobalService {

        private readonly ILogger<CGlobalService> _logger;
        private readonly IConverterService _cs;

        public List<string> AllowedIpOrigin { get; set; } = new List<string>() {
            "localhost", "127.0.0.1"
        };

        public CGlobalService(
            ILogger<CGlobalService> logger,
            IConverterService cs
        ) {
            this._logger = logger;
            this._cs = cs;
        }

        public string GetSecretData(HttpRequest request, RequestJson reqBody) {
            string secret = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers["x-secret-key"])) {
                secret = request.Headers["x-secret-key"];
            }
            else if (!string.IsNullOrEmpty(request.Query["secret"])) {
                secret = request.Query["secret"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.secret)) {
                secret = reqBody.secret;
            }

            return secret;
        }

        public string GetApiKeyData(HttpRequest request, RequestJson reqBody) {
            string apiKey = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers["x-api-key"])) {
                apiKey = request.Headers["x-api-key"];
            }
            else if (!string.IsNullOrEmpty(request.Query["key"])) {
                apiKey = request.Query["key"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.key)) {
                apiKey = reqBody.key;
            }

            return apiKey;
        }

        public string GetIpOriginData(ConnectionInfo connection, HttpRequest request) {
            string ipOrigin = connection.RemoteIpAddress.ToString();
            if (!string.IsNullOrEmpty(request.Headers["origin"])) {
                ipOrigin = request.Headers["origin"];
            }
            else if (!string.IsNullOrEmpty(request.Headers["referer"])) {
                ipOrigin = request.Headers["referer"];
            }
            else if (!string.IsNullOrEmpty(request.Headers["cf-connecting-ip"])) {
                ipOrigin = request.Headers["cf-connecting-ip"];
            }
            else if (!string.IsNullOrEmpty(request.Headers["x-forwarded-for"])) {
                ipOrigin = request.Headers["x-forwarded-for"];
            }

            return ipOrigin;
        }

        public string CleanIpOrigin(string ipOrigin) {
            ipOrigin ??= "";
            // Remove Prefixes
            if (ipOrigin.StartsWith("::ffff:")) {
                ipOrigin = ipOrigin[7..];
            }

            if (ipOrigin.StartsWith("http://")) {
                ipOrigin = ipOrigin[7..];
            }
            else if (ipOrigin.StartsWith("https://")) {
                ipOrigin = ipOrigin[8..];
            }

            if (ipOrigin.StartsWith("www.")) {
                ipOrigin = ipOrigin[4..];
            }
            // Get Domain Or IP Maybe With Port Included And Remove Folder Path
            ipOrigin = ipOrigin.Split("/")[0];
            // Remove Port
            int totalColon = 0;
            for (int i = 0; i < ipOrigin.Length; i++) {
                if (ipOrigin[i] == ':') {
                    totalColon++;
                }

                if (totalColon > 1) {
                    break;
                }
            }

            if (totalColon == 1) {
                // IPv4
                ipOrigin = ipOrigin.Split(":")[0];
            }
            else {
                // IPv6
                ipOrigin = ipOrigin.Split("]")[0];
                if (ipOrigin.StartsWith("[")) {
                    ipOrigin = ipOrigin[1..];
                }
            }

            return ipOrigin;
        }

        public string GetTokenData(HttpRequest request, RequestJson reqBody) {
            string token = string.Empty;
            if (!string.IsNullOrEmpty(request.Headers.Authorization)) {
                token = request.Headers.Authorization;
            }
            else if (!string.IsNullOrEmpty(request.Headers["x-access-token"])) {
                token = request.Headers["x-access-token"];
            }
            else if (!string.IsNullOrEmpty(request.Query["token"])) {
                token = request.Query["token"];
            }
            else if (!string.IsNullOrEmpty(reqBody?.token)) {
                token = reqBody.token;
            }

            return token;
        }

        public async Task<RequestJson> GetRequestBody(HttpRequest request) {
            RequestJson reqBody = null;
            string contentType = request.Headers["content-type"].ToString();
            if (SwaggerMediaTypesOperationFilter.AcceptedContentType.Contains(contentType)) {
                string rbString = await request.GetRequestBodyStringAsync();
                if (!string.IsNullOrEmpty(rbString)) {
                    try {
                        reqBody = this._cs.XmlJsonToObject<RequestJson>(contentType, rbString);
                    }
                    catch (Exception ex) {
                        this._logger.LogError("[JSON_BODY] 🌸 {ex}", ex.Message);
                    }
                }
            }

            return reqBody;
        }

    }

}
