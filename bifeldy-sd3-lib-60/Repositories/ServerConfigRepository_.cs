/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Configurasi Aplikasi Per User Session
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IServerConfigRepository {
        string CurrentLoadedKodeServerKunciDc(HttpContext httpContext = null);
        Task<bool> AddKodeServerKunciDc(string kodeDc, string kunciGxxx, string serverTarget);
        Task<bool> EditKodeServerKunciDc(string kodeDc, string kunciGxxx, string serverTarget);
        Task<bool> RemoveKodeServerKunciDc(string kodeDc);
        Task<List<ServerConfigKunci>> GetKodeServerKunciDc(string kodeDc = null);
        Task<ServerConfigKunci> UseKodeServerKunciDc(string kodeDc, string kunciGxxx = null, string serverTarget = null);
    }

    [ScopedServiceRegistration]
    public sealed class CServerConfigRepository : IServerConfigRepository {

        private readonly IHttpContextAccessor _hca;
        private readonly ISqlite _sqlite;
        private readonly IGlobalService _gs;

        private string KunciGxxx = null;

        public CServerConfigRepository(
            IHttpContextAccessor hca,
            ISqlite sqlite,
            IGlobalService gs
        ) {
            this._hca = hca;
            this._sqlite = sqlite;
            this._gs = gs;
        }

        public string CurrentLoadedKodeServerKunciDc(HttpContext httpContext = null) {
            if (!string.IsNullOrEmpty(this.KunciGxxx)) {
                return this.KunciGxxx;
            }

            string serverTarget = null;

            HttpRequest request = httpContext?.Request ?? this._hca.HttpContext?.Request;
            HttpResponse response = httpContext?.Response ?? this._hca.HttpContext?.Response;

            if (request != null) {
                RequestJson reqBody = this._gs.GetHttpRequestBody<RequestJson>(request).Result;

                if (!string.IsNullOrEmpty(request.Headers["x-server"])) {
                    serverTarget = request.Headers["x-server"];
                }
                else if (request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
                    serverTarget = pathBase.Last();
                }
                else if (!string.IsNullOrEmpty(request.Headers.Host)) {
                    string host = request.Headers.Host;
                    if (!host.StartsWith("http")) {
                        host = $"http://{host}";
                    }

                    serverTarget = new Uri(host).Host;
                }
                else if (!string.IsNullOrEmpty(request.Query["server"])) {
                    serverTarget = request.Query["server"];
                }
                else if (!string.IsNullOrEmpty(reqBody?.secret)) {
                    serverTarget = reqBody.server;
                }
            }

            if (!string.IsNullOrEmpty(serverTarget)) {
                var rgxLs = new List<string>() {
                    "g[0-9]{3}",
                    "dcho|whho",
                    "kcbn|pgcbn"
                };

                string kodeDc = null;
                foreach (string rgxStr in rgxLs) {
                    var rgx = new Regex($"({rgxStr})", RegexOptions.IgnoreCase);
                    Match match = rgx.Match(serverTarget);
                    if (match.Success) {
                        kodeDc = match.Groups[1].Value.ToLower().Trim();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(kodeDc)) {
                    string errMsg = "Kunci Server Tidak Tersedia, Silahkan Atur & Pilih Terlebih Dahulu!";

                    if (request != null) {
                        if (response != null) {
                            throw new KunciServerTidakTersediaException(errMsg);
                        }
                    }

                    throw new Exception(errMsg);
                }

                if (kodeDc.ToLower() == "pgcbn") {
                    kodeDc = "kcbn";
                }

                _ = this.UseKodeServerKunciDc(kodeDc, null, serverTarget).Result;
            }

            if (string.IsNullOrEmpty(this.KunciGxxx)) {
                throw new Exception("Kunci Server Belum Di Set!");
            }

            return this.KunciGxxx;
        }

        public async Task<bool> AddKodeServerKunciDc(string kodeDc, string kunciGxxx, string serverTarget) {
            if (string.IsNullOrEmpty(kodeDc) || string.IsNullOrEmpty(kunciGxxx) || string.IsNullOrEmpty(serverTarget)) {
                throw new TidakMemenuhiException("Kode DC / Kunci GXXX / Server Target Tidak Boleh Kosong!");
            }

            return await this._sqlite.ExecQueryAsync(
                $@"
                    INSERT INTO server_kunci (kode_dc, kunci_gxxx, server_target)
                    VALUES (:kode_dc, :kunci_gxxx, :server_target)
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "kode_dc", VALUE = kodeDc.ToLower() },
                    new() { NAME = "kunci_gxxx", VALUE = kunciGxxx },
                    new() { NAME = "server_target", VALUE = serverTarget }
                }
            );
        }

        public async Task<bool> EditKodeServerKunciDc(string kodeDc, string kunciGxxx, string serverTarget) {
            if (string.IsNullOrEmpty(kodeDc) || string.IsNullOrEmpty(kunciGxxx) || string.IsNullOrEmpty(serverTarget)) {
                throw new TidakMemenuhiException("Kode DC / Kunci GXXX / Server Target Tidak Boleh Kosong!");
            }

            return await this._sqlite.ExecQueryAsync(
                $@"
                    UPDATE server_kunci
                    SET kunci_gxxx = :kunci_gxxx, server_target = :server_target
                    WHERE LOWER(kode_dc) = :kode_dc
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "kode_dc", VALUE = kodeDc.ToLower() },
                    new() { NAME = "kunci_gxxx", VALUE = kunciGxxx },
                    new() { NAME = "server_target", VALUE = serverTarget }
                }
            );
        }

        public async Task<bool> RemoveKodeServerKunciDc(string kodeDc) {
            if (string.IsNullOrEmpty(kodeDc)) {
                throw new TidakMemenuhiException("Kode DC Tidak Boleh Kosong!");
            }

            return await this._sqlite.ExecQueryAsync(
                $@"
                    DELETE FROM server_kunci
                    WHERE LOWER(kode_dc) = :kode_dc
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "kode_dc", VALUE = kodeDc.ToLower() }
                }
            );
        }

        public async Task<List<ServerConfigKunci>> GetKodeServerKunciDc(string kodeDc = null) {
            string sqlQuery = "SELECT * FROM server_kunci";
            var sqlParams = new List<CDbQueryParamBind>();

            if (!string.IsNullOrEmpty(kodeDc)) {
                sqlQuery += " WHERE LOWER(kode_dc) = :kode_dc";
                sqlParams.Add(new CDbQueryParamBind() { NAME = "kode_dc", VALUE = kodeDc.ToLower() });
            }

            sqlQuery += " ORDER BY kode_dc ASC";
            return await this._sqlite.GetListAsync<ServerConfigKunci>(sqlQuery, sqlParams);
        }

        // Panggil Ini Dulu Sebelum Resolve Menggunakan Service Provider (_sp.GetService / _sp.GetRequiredService)
        public async Task<ServerConfigKunci> UseKodeServerKunciDc(string kodeDc, string kunciGxxx = null, string serverTarget = null) {
            var sc = new ServerConfigKunci() {
                kode_dc = kodeDc,
                kunci_gxxx = kunciGxxx,
                server_target = serverTarget
            };

            if (string.IsNullOrEmpty(sc.kunci_gxxx)) {
                if (string.IsNullOrEmpty(sc.kode_dc)) {
                    throw new Exception("Kode DC Tidak Boleh Kosong!");
                }

                List<ServerConfigKunci> __sc = await this.GetKodeServerKunciDc(sc.kode_dc);
                ServerConfigKunci _sc = __sc.FirstOrDefault();

                if (_sc == null) {
                    _ = await this.AddKodeServerKunciDc(sc.kode_dc, $"kunci{kodeDc}".ToLower(), serverTarget);
                    __sc = await this.GetKodeServerKunciDc(sc.kode_dc);
                    _sc = __sc.First();
                }

                sc = _sc;
            }

            this.KunciGxxx = sc.kunci_gxxx?.Trim();

            return sc;
        }

    }

}
