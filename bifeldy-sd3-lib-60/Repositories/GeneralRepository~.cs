﻿/**
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
using Microsoft.Extensions.Options;

using Confluent.Kafka;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IGeneralRepository : IRepository {
        string DbName { get; }
        Task<string> GetURLWebService(string webType);
        Task<bool> SaveKafkaToTable(string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName);
        Task<KAFKA_SERVER_T> GetKafkaServerInfo(string topicName);
        Task<List<DC_TABEL_V>> GetListBranchDbInformation(string kodeDcInduk);
        Task<IDictionary<string, (bool, CDatabase)>> GetListBranchDbConnection(string kodeDcInduk);
        Task<(bool, CDatabase, CDatabase)> OpenConnectionToDcFromHo(string kodeDcTarget);
        Task GetDcApiPathAppFromHo(HttpRequest request, string dcKode, Action<string, Uri> Callback);
        Task<string> GetAppHoApiUrlBase(string apiPath);
    }

    [TransientServiceRegistration]
    public class CGeneralRepository : CRepository, IGeneralRepository {

        private readonly EnvVar _envVar;

        private readonly IApplicationService _as;
        private readonly IGlobalService _gs;
        private readonly IHttpService _http;
        private readonly IChiperService _chiper;
        private readonly IConverterService _converter;

        private readonly IOracle _oracle;
        private readonly IPostgres _postgres;
        private readonly IOraPg _orapg;
        private readonly IMsSQL _mssql;

        private IDictionary<
            string, IDictionary<
                string, (bool, CDatabase)
            >
        > BranchConnectionInfo { get; } = new Dictionary<
            string, IDictionary<
                string, (bool, CDatabase)
            >
        >(StringComparer.InvariantCultureIgnoreCase);

        public CGeneralRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IGlobalService gs,
            IHttpService http,
            IChiperService chiper,
            IConverterService converter,
            IOracle oracle,
            IPostgres postgres,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            this._envVar = envVar.Value;
            this._as = @as;
            this._gs = gs;
            this._http = http;
            this._chiper = chiper;
            this._converter = converter;
            this._oracle = oracle;
            this._postgres = postgres;
            this._orapg = orapg;
            this._mssql = mssql;
        }

        /** Custom Queries */

        public string DbName {
            get {
                string FullDbName = string.Empty;
                try {
                    FullDbName += this._orapg.DbName;
                }
                catch {
                    FullDbName += "-";
                }

                FullDbName += " / ";
                try {
                    FullDbName += this._mssql.DbName;
                }
                catch {
                    FullDbName += "-";
                }

                return FullDbName;
            }
        }

        /* ** */

        public async Task<string> CekVersi() {
            if (this._as.DebugMode) {
                return "OKE";
            }
            else {
                try {
                    string res = await this._orapg.ExecScalarAsync<string>(
                        $@"
                            SELECT
                                CASE
                                    WHEN COALESCE(aprove, 'N') = 'Y' AND {(
                                            this._envVar.IS_USING_POSTGRES
                                            ? "COALESCE(tgl_berlaku, NOW())::DATE <= CURRENT_DATE"
                                            : "TRUNC(COALESCE(tgl_berlaku, SYSDATE)) <= TRUNC(SYSDATE)"
                                        )} 
                                        THEN COALESCE(VERSI_BARU, '0')
                                    WHEN COALESCE(aprove, 'N') = 'N'
                                        THEN COALESCE(versi_lama, '0')
                                    ELSE
                                        COALESCE(versi_lama, '0')
                                END AS VERSI
                            FROM
                                dc_program_vbdtl_t
                            WHERE
                                UPPER(dc_kode) = :dc_kode
                                AND UPPER(nama_prog) LIKE :nama_prog
                        ",
                        new List<CDbQueryParamBind>() {
                            new() { NAME = "dc_kode", VALUE = await this.GetKodeDc() },
                            new() { NAME = "nama_prog", VALUE = $"%{this._as.AppName}%".ToUpper() }
                        }
                    );
                    if (string.IsNullOrEmpty(res)) {
                        res = $"Program :: {this._as.AppName}" + Environment.NewLine + "Belum Terdaftar Di Master Program DC";
                    }
                    else if (res == this._as.AppVersion) {
                        res = "OKE";
                    }
                    else {
                        res = $"Versi Program :: {this._as.AppName}" + Environment.NewLine + $"Tidak Sama Dengan Master Program = v{res}";
                    }

                    return res;
                }
                catch (Exception ex) {
                    return ex.Message;
                }
            }
        }

        public async Task<bool> LoginUser(string userNameNik, string password) {
            string loggedInUsername = await this._orapg.ExecScalarAsync<string>(
                $@"
                    SELECT
                        user_name
                    FROM
                        dc_user_t
                    WHERE
                        (UPPER(user_name) = :user_name OR UPPER(user_nik) = :user_nik)
                        AND UPPER(user_password) = :password
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "user_name", VALUE = userNameNik.ToUpper() },
                    new() { NAME = "user_nik", VALUE = userNameNik.ToUpper() },
                    new() { NAME = "password", VALUE = password.ToUpper() }
                }
            );
            return !string.IsNullOrEmpty(loggedInUsername);
        }

        public async Task<string> GetURLWebService(string webType) {
            return await this._orapg.ExecScalarAsync<string>(
                $@"SELECT web_url FROM dc_webservice_t WHERE UPPER(web_type) = :web_type",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "web_type", VALUE = webType.ToUpper() }
                }
            );
        }

        public async Task<bool> SaveKafkaToTable(string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName) {
            return await this._orapg.ExecQueryAsync($@"
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

        public async Task<KAFKA_SERVER_T> GetKafkaServerInfo(string topicName) {
            return await this._orapg.Set<KAFKA_SERVER_T>().Where(k => k.TOPIC.ToUpper() == topicName.ToUpper()).FirstOrDefaultAsync();
        }

        /* ** */

        public async Task<List<DC_TABEL_V>> GetListBranchDbInformation(string kodeDcInduk) {
            string url = await this.GetURLWebService("SYNCHO") ?? this._envVar.WS_SYNCHO;
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
        // Item2 => CDatabase :: Koneksi Ke Database Oracle / Postgre (Tidak Ada SqlServer)
        //
        // IDictionary<string, (bool, CDatabase)> dbCon = await GetListBranchDbConnection("G001");
        // var res = dbCon["G055"].Item2.ExecScalarAsync<...>(...);
        //
        public async Task<IDictionary<string, (bool, CDatabase)>> GetListBranchDbConnection(string kodeDcInduk) {
            if (!this.BranchConnectionInfo.ContainsKey(kodeDcInduk)) {
                IDictionary<string, (bool, CDatabase)> dbCons = new Dictionary<string, (bool, CDatabase)>(StringComparer.InvariantCultureIgnoreCase);

                List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation(kodeDcInduk);
                foreach (DC_TABEL_V dbi in dbInfo) {
                    CDatabase dbCon;
                    bool isPostgre = dbi.FLAG_DBPG?.ToUpper() == "Y";
                    if (isPostgre) {
                        dbCon = this._postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                    }
                    else {
                        dbCon = this._oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT, dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                    }

                    dbCons.Add(dbi.TBL_DC_KODE.ToUpper(), (isPostgre, dbCon));
                }

                this.BranchConnectionInfo[kodeDcInduk] = dbCons;
            }

            return this.BranchConnectionInfo[kodeDcInduk];
        }

        public async Task<(bool, CDatabase, CDatabase)> OpenConnectionToDcFromHo(string kodeDcTarget) {
            CDatabase dbConHo = null;

            string kodeDcSekarang = await this.GetKodeDc();
            bool isHo = await this.IsHo();
            if (!isHo) {
                List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation("DCHO");
                DC_TABEL_V dcho = dbInfo.FirstOrDefault();
                if (dcho != null) {
                    dbConHo = this._oracle.NewExternalConnection(dcho.IP_DB, dcho.DB_PORT.ToString(), dcho.DB_USER_NAME, dcho.DB_PASSWORD, dcho.DB_SID);
                }
            }
            else {
                dbConHo = (CDatabase) this._orapg;
            }

            bool dbIsUsingPostgre = false;
            CDatabase dbOraPgDc = null;
            CDatabase dbSqlDc = null;

            if (dbConHo != null) {
                DC_TABEL_IP_T dbi = dbConHo.Set<DC_TABEL_IP_T>().Where(d => d.DC_KODE.ToUpper() == kodeDcTarget.ToUpper()).SingleOrDefault();
                if (dbi != null) {
                    dbIsUsingPostgre = dbi.FLAG_DBPG?.ToUpper() == "Y";
                    if (dbIsUsingPostgre) {
                        dbOraPgDc = this._postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                    }
                    else {
                        dbOraPgDc = this._oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT.ToString(), dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                    }

                    dbSqlDc = this._mssql.NewExternalConnection(dbi.DB_IP_SQL, dbi.DB_USER_SQL, dbi.DB_PWD_SQL, dbi.SCHEMA_DPD);
                }
            }

            return (dbIsUsingPostgre, dbOraPgDc, dbSqlDc);
        }

        public async Task GetDcApiPathAppFromHo(HttpRequest request, string dcKode, Action<string, Uri> Callback) {
            bool isHo = await this.IsHo();
            if (!isHo) {
                throw new TidakMemenuhiException("Khusus HO!");
            }

            List<ListApiDc> listApiDcs = await this._orapg.GetListAsync<ListApiDc>($@"
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
                Callback($"Kode gudang ({dcKode.ToUpper()}) tidak tersedia!", null);
            }
            else {
                string separator = "/api/";

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
                    string findUrl = $"{_as.AppName.ToUpper()}HO";
                    if (currentPath.ToUpper().Contains($"/{findUrl}")) {
                        int idx = currentPath.ToUpper().IndexOf(findUrl);
                        if (idx >= 0) {
                            idx += _as.AppName.Length;
                            currentPath = $"{currentPath[..idx]}{dcKode.ToUpper()}{currentPath[(idx + 2)..]}";
                        }
                    }
                }

                string pathApiDc = string.IsNullOrEmpty(dbi.API_PATH) ? currentPath : $"{dbi.API_PATH}{currentPath?.Split(separator).Last()}";
                var urlApiDc = new Uri($"http://{hostApiDc}{pathApiDc}{request.QueryString.Value}");

                string hashText = this._chiper.HashText(this._as.AppName);
                request.Headers["x-api-key"] = hashText;

                // API Key Khusus Bypass ~ Case Sensitive
                NameValueCollection queryApiDc = HttpUtility.ParseQueryString(urlApiDc.Query);
                queryApiDc.Set("key", hashText);

                string ipOrigin = this._gs.GetIpOriginData(request.HttpContext.Connection, request.HttpContext.Request, true);
                queryApiDc.Set("mask_ip", this._chiper.EncryptText(ipOrigin));

                var uriBuilder = new UriBuilder(urlApiDc) {
                    Query = queryApiDc.ToString()
                };

                Callback(null, uriBuilder.Uri);
            }
        }

        public async Task<string> GetAppHoApiUrlBase(string apiPath) {
            //
            // http://xxx.xxx.xxx.xxx/{appNameAsPath}/api?secret=*********
            //
            string appNameAsPath = this._as.AppName.ToUpper();
            string apiUrl = await this._orapg.ExecScalarAsync<string>($@"
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

    }

}
