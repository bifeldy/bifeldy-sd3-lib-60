/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Kumpulan Handler Database Bawaan
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Collections.Specialized;
using System.Data;
using System.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Confluent.Kafka;

using Grpc.Core;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IGeneralRepository : IRepository {
        Task<string> GetURLWebService(bool isPg, IDatabase db, string webType);
        Task<bool> SaveKafkaToTable(bool isPg, IDatabase db, string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName);
        Task<KAFKA_SERVER_T> GetKafkaServerInfo(bool isPg, IDatabase db, string topicName);
        Task<List<DC_TABEL_V>> GetListBranchDbInformation(bool isPg, IDatabase db, string kodeDcInduk);
        Task<IDictionary<string, (bool, IDatabase)>> GetListBranchDbConnection(bool isPg, IDatabase db, string kodeDcInduk, IServiceProvider sp);
        Task<(bool, IDatabase, IDatabase)> OpenConnectionToDcFromHo(bool isPg, IDatabase db, string kodeDcTarget, IServiceProvider sp);
        Task GetDcApiPathAppFromHo(bool isPg, IDatabase db, HttpRequest request, string dcKode, Action<string, Uri> Callback);
        Task<string> GetAppHoApiUrlBase(bool isPg, IDatabase db, string apiPath);
        Task CheckKoordinatorHO(bool isPg, IDatabase db, string kodeDc, bool isGrpc = false);
    }

    [ScopedServiceRegistration]
    public class CGeneralRepository : CRepository, IGeneralRepository {

        private readonly EnvVar _envVar;

        private readonly IApplicationService _as;
        private readonly IHttpService _http;
        private readonly IChiperService _chiper;
        private readonly IConverterService _converter;

        private IDictionary<
            string, IDictionary<
                string, (bool, IDatabase)
            >
        > BranchConnectionInfo { get; } = new Dictionary<
            string, IDictionary<
                string, (bool, IDatabase)
            >
        >(StringComparer.InvariantCultureIgnoreCase);

        public CGeneralRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IHttpService http,
            IChiperService chiper,
            IConverterService converter
        ) {
            this._envVar = envVar.Value;
            this._as = @as;
            this._http = http;
            this._chiper = chiper;
            this._converter = converter;
        }

        /** Custom Queries */

        /* ** */

        public async Task<string> GetURLWebService(bool isPg, IDatabase db, string webType) {
            return await db.ExecScalarAsync<string>(
                $@"SELECT web_url FROM dc_webservice_t WHERE UPPER(web_type) = :web_type",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "web_type", VALUE = webType.ToUpper() }
                }
            );
        }

        public async Task<bool> SaveKafkaToTable(bool isPg, IDatabase db, string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName) {
            return await db.ExecQueryAsync($@"
                INSERT INTO {logTableName} (TPC, OFFS, PARTT, KEY, VAL, TMSTAMP)
                VALUES (:tpc, :offs, :partt, :key, :value, :tmstmp)
            ", new List<CDbQueryParamBind>() {
                new() { NAME = "tpc", VALUE = topic },
                new() { NAME = "offs", VALUE = offset },
                new() { NAME = "partt", VALUE = partition },
                new() { NAME = "key", VALUE = msg.Key },
                new() { NAME = "value", VALUE = msg.Value },
                new() { NAME = "tmstmp", VALUE = msg.Timestamp.UtcDateTime }
            });
        }

        public async Task<KAFKA_SERVER_T> GetKafkaServerInfo(bool isPg, IDatabase db, string topicName) {
            return await db.Set<KAFKA_SERVER_T>().Where(k => k.TOPIC.ToUpper() == topicName.ToUpper()).AsNoTracking().FirstOrDefaultAsync();
        }

        /* ** */

        public async Task<List<DC_TABEL_V>> GetListBranchDbInformation(bool isPg, IDatabase db, string kodeDcInduk) {
            string url = await this.GetURLWebService(isPg, db, "SYNCHO") ?? this._envVar.WS_SYNCHO;
            url += kodeDcInduk;

            HttpResponseMessage httpResponse = await this._http.PostData(url, null);
            string httpResString = await httpResponse.Content.ReadAsStringAsync();

            return this._converter.JsonToObject<List<DC_TABEL_V>>(httpResString);
        }

        //
        // Akses Langsung Ke Database Cabang
        // Tembak Ambil Info Dari Service Mas Edwin :) HO
        // Atur URL Di `appsettings.json` -> ws_syncho
        //
        // Item1 => bool :: Apakah Menggunakan Postgre
        // Item2 => IDatabase :: Koneksi Ke Database Oracle / Postgre (Tidak Ada SqlServer)
        //
        // IDictionary<string, (bool, IDatabase)> dbOraPgBranch = await GetListBranchDbConnection(..., "G001", ...);
        // var res = dbOraPgBranch["G055"].Item2.ExecScalarAsync<...>(...);
        //
        public async Task<IDictionary<string, (bool, IDatabase)>> GetListBranchDbConnection(bool isPg, IDatabase db, string kodeDcInduk, IServiceProvider sp) {
            if (!this.BranchConnectionInfo.ContainsKey(kodeDcInduk)) {
                IDictionary<string, (bool, IDatabase)> dbCons = new Dictionary<string, (bool, IDatabase)>(StringComparer.InvariantCultureIgnoreCase);

                List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation(isPg, db, kodeDcInduk);
                foreach (DC_TABEL_V dbi in dbInfo) {
                    IDatabase dbOraPgBranch = null;

                    bool isPostgre = dbi.FLAG_DBPG?.ToUpper() == "Y";
                    if (isPostgre) {
                        IPostgres postgres = sp.GetRequiredService<IPostgres>();
                        dbOraPgBranch = postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                    }
                    else {
                        IOracle oracle = sp.GetRequiredService<IOracle>();
                        dbOraPgBranch = oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT.ToString(), dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                    }

                    dbCons.Add(dbi.TBL_DC_KODE.ToUpper(), (isPostgre, dbOraPgBranch));
                }

                this.BranchConnectionInfo[kodeDcInduk] = dbCons;
            }

            return this.BranchConnectionInfo[kodeDcInduk];
        }

        public async Task<(bool, IDatabase, IDatabase)> OpenConnectionToDcFromHo(bool isPg, IDatabase db, string kodeDcTarget, IServiceProvider sp) {
            IOracle oracle = sp.GetRequiredService<IOracle>();
            IPostgres postgres = sp.GetRequiredService<IPostgres>();
            IMsSQL mssql = sp.GetRequiredService<IMsSQL>();

            IDatabase dbConHo = db;
            bool isHo = await this.IsHo(isPg, db);
            if (!isHo) {
                List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation(isPg, db, "DCHO");

                DC_TABEL_V dcho = dbInfo.FirstOrDefault();
                if (dcho != null) {

                    bool isHoPg = dcho.FLAG_DBPG?.ToUpper() == "Y";
                    if (isHoPg) {
                        dbConHo = postgres.NewExternalConnection(dcho.DBPG_IP, dcho.DBPG_PORT, dcho.DBPG_USER, dcho.DBPG_PASS, dcho.DBPG_NAME);
                    }
                    else {
                        dbConHo = oracle.NewExternalConnection(dcho.IP_DB, dcho.DB_PORT.ToString(), dcho.DB_USER_NAME, dcho.DB_PASSWORD, dcho.DB_SID);
                    }
                }
            }

            bool dbIsUsingPostgre = false;
            IDatabase dbOraPgDc = null;
            IDatabase dbSqlDc = null;

            if (dbConHo != null) {
                DC_TABEL_IP_T dbi = dbConHo.Set<DC_TABEL_IP_T>()
                    .Where(d => d.DC_KODE.ToUpper() == kodeDcTarget.ToUpper())
                    .AsNoTracking()
                    .SingleOrDefault();

                if (dbi != null) {
                    dbIsUsingPostgre = dbi.FLAG_DBPG?.ToUpper() == "Y";

                    if (dbIsUsingPostgre) {
                        dbOraPgDc = postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                    }
                    else {
                        dbOraPgDc = oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT.ToString(), dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                    }

                    if (
                        !string.IsNullOrEmpty(dbi.DB_IP_SQL) &&
                        !string.IsNullOrEmpty(dbi.DB_USER_SQL) &&
                        !string.IsNullOrEmpty(dbi.DB_PWD_SQL) &&
                        !string.IsNullOrEmpty(dbi.SCHEMA_DPD)
                    ) {
                        dbSqlDc = mssql.NewExternalConnection(dbi.DB_IP_SQL, dbi.DB_USER_SQL, dbi.DB_PWD_SQL, dbi.SCHEMA_DPD);
                    }
                }
            }

            return (dbIsUsingPostgre, dbOraPgDc, dbSqlDc);
        }

        public async Task GetDcApiPathAppFromHo(bool isPg, IDatabase db, HttpRequest request, string dcKode, Action<string, Uri> callback) {
            bool isHo = await this.IsHo(isPg, db);
            if (!isHo) {
                throw new TidakMemenuhiException("Khusus HO!");
            }

            List<ListApiDc> listApiDcs = await db.GetListAsync<ListApiDc>($@"
                SELECT
                    a.dc_kode,
                    a.ip_nginx,
                    b.api_host,
                    b.api_path
                FROM
                    dc_tabel_ip_t a
                    LEFT JOIN api_dc_t b ON (
                        a.dc_kode = b.dc_kode
                        AND UPPER(b.app_name) = :app_name
                    )
                WHERE
                    UPPER(a.dc_kode) = :kode_dc
            ", new List<CDbQueryParamBind>() {
                new() { NAME = "app_name", VALUE = this._as.AppName.ToUpper() },
                new() { NAME = "kode_dc", VALUE = dcKode.ToUpper() }
            });

            ListApiDc dbi = listApiDcs.FirstOrDefault();
            string hostApiDc = string.IsNullOrEmpty(dbi?.API_HOST) ? dbi?.IP_NGINX : dbi?.API_HOST;
            if (dbi == null || string.IsNullOrEmpty(hostApiDc)) {
                callback($"Kode DC {dcKode.ToUpper()} tidak tersedia!", null);
            }
            else {
                string separator = $"/{Bifeldy.API_PREFIX}/";

                //
                // dotnet blablabla.dll
                //
                // http://127.x.xx.xxx/blablablaHOSIM/api/bliblibli
                // http://127.x.xx.xxx/blablablaHO/api/bliblibli
                // /blablablaHOSIM/api/bliblibli
                // /blablablaHO/api/bliblibli
                //
                // http://127.x.xx.xxx/blablablaGXXXSIM/api/bliblibli
                // http://127.x.xx.xxx/blablablaGXXX/api/bliblibli
                // /blablablaGXXXSIM/api/bliblibli
                // /blablablaGXXX/api/bliblibli
                //
                string currentPath = request.Path.Value;
                if (!string.IsNullOrEmpty(currentPath)) {
                    string findUrl = $"{this._as.AppName.ToUpper()}HO";
                    if (currentPath.ToUpper().Contains($"/{findUrl}")) {
                        int idx = currentPath.ToUpper().IndexOf(findUrl);
                        if (idx >= 0) {
                            idx += this._as.AppName.Length;
                            currentPath = $"{currentPath[..idx]}{dcKode.ToUpper()}{currentPath[(idx + 2)..]}";
                        }
                    }
                }

                string pathApiDc = string.IsNullOrEmpty(dbi.API_PATH) ? currentPath : $"{dbi.API_PATH}{currentPath?.Split(separator).Last()}";
                var urlApiDc = new Uri($"http://{hostApiDc}{pathApiDc}{request.QueryString.Value}");

                // API Khusus Bypass ~ Case Sensitive
                NameValueCollection queryApiDc = HttpUtility.ParseQueryString(urlApiDc.Query);
                string hashText = this._chiper.HashText(this._as.AppName);

                request.Headers["x-secret"] = hashText;
                queryApiDc.Set("secret", hashText);

                request.Headers["x-api-key"] = hashText;
                queryApiDc.Set("key", hashText);

                if (request.HttpContext.Items["token"] != null) {
                    string token = request.HttpContext.Items["token"].ToString();
                    request.Headers.Authorization = token;
                    request.Headers["x-access-token"] = token;
                    queryApiDc.Set("token", token);
                }

                if (request.HttpContext.Items["address_ip"] != null) {
                    string addrIp = request.HttpContext.Items["address_ip"].ToString();
                    queryApiDc.Set("mask_ip", this._chiper.EncryptText(addrIp));
                }

                var uriBuilder = new UriBuilder(urlApiDc) {
                    Query = queryApiDc.ToString()
                };

                callback(null, uriBuilder.Uri);
            }
        }

        public async Task<string> GetAppHoApiUrlBase(bool isPg, IDatabase db, string apiPath) {
            //
            // http://xxx.xxx.xxx.xxx/{appNameAsPath}/api?secret=*********
            //
            string appNameAsPath = this._as.AppName.ToUpper();
            string apiUrl = await db.ExecScalarAsync<string>($@"
                SELECT web_url
                FROM dc_webservice_t
                WHERE web_type = '{appNameAsPath}_API_URL_BASE'
            ");
            if (string.IsNullOrEmpty(apiUrl)) {
                throw new Exception($"API URL Web Service '{appNameAsPath}_API_URL_BASE' Tidak Tersedia");
            }

            var baseUri = new Uri(apiUrl);
            NameValueCollection baseQuery = HttpUtility.ParseQueryString(baseUri.Query);

            string url = $"{baseUri.Scheme}://";
            if (!string.IsNullOrEmpty(baseUri.UserInfo)) {
                url += $"{baseUri.UserInfo}@";
            }

            url += $"{baseUri.Host}:{baseUri.Port}";

            var apiUri = new Uri(apiPath);
            NameValueCollection apiQuery = HttpUtility.ParseQueryString(apiUri.Query);

            foreach (string aq in baseQuery.AllKeys) {
                apiQuery.Set(aq, baseQuery.Get(aq));
            }

            var uriBuilder = new UriBuilder(url) {
                Path = $"{baseUri.AbsolutePath}{apiUri.AbsolutePath}",
                Query = apiQuery.ToString()
            };
            return uriBuilder.ToString();
        }

        public async Task CheckKoordinatorHO(bool isPg, IDatabase db, string kodeDc, bool isGrpc = false) {
            string targetJenisDc = await this.GetJenisDc(isPg, db, kodeDc.ToUpper());

            if (Enum.TryParse(targetJenisDc.ToUpper(), true, out EJenisDc _eJenisDc)) {
                bool isDcHo = await this.IsDcHo(isPg, db);
                bool isWhHo = await this.IsWhHo(isPg, db);

                string exception = null;
                if (isDcHo && _eJenisDc == EJenisDc.IPLAZA) {
                    exception = "Silahkan Gunakan WH HO Untuk Akses Ke DC IPLAZA WHK";
                }
                else if (isWhHo && _eJenisDc != EJenisDc.IPLAZA) {
                    exception = "Silahkan Gunakan DC HO Untuk Akses Ke DC Selain IPLAZA WHK";
                }

                if (!string.IsNullOrEmpty(exception)) {
                    if (isGrpc) {
                        throw new RpcException(new Status(StatusCode.Unavailable, exception));
                    }

                    throw new TidakMemenuhiException(exception);
                }
            }
        }

    }

}
