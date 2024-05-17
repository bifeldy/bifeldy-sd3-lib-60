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

using System.Data;
using System.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Confluent.Kafka;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IGeneralRepository : IRepository {
        string DbName { get; }
        Task<string> GetURLWebService(string webType);
        Task<bool> SaveKafkaToTable(string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName);
        Task<List<DC_TABEL_V>> GetListBranchDbInformation(string kodeDcInduk);
        Task<IDictionary<string, (bool, CDatabase)>> GetListBranchDbConnection(string kodeDcInduk);
        Task<(bool, CDatabase, CDatabase)> OpenConnectionToDcFromHo(string kodeDcTarget);
        Task GetDcApiPathAppFromHo(HttpRequest request, string dcKode, Action<string, Uri> Callback);
    }

    public class CGeneralRepository : CRepository, IGeneralRepository {

        private readonly EnvVar _envVar;

        private readonly IApplicationService _as;
        private readonly IHttpService _http;
        private readonly IConverterService _converter;

        private readonly IOracle _oracle;
        private readonly IPostgres _postgres;
        private readonly IOraPg _orapg;
        private readonly IMsSQL _mssql;

        public CGeneralRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IHttpService http,
            IConverterService converter,
            IOracle oracle,
            IPostgres postgres,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            this._envVar = envVar.Value;
            this._as = @as;
            this._http = http;
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
                        new List<CDbQueryParamBind> {
                            new() { NAME = "dc_kode", VALUE = await this.GetKodeDc() },
                            new() { NAME = "nama_prog", VALUE = $"%{this._as.AppName}%" }
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
                new List<CDbQueryParamBind> {
                    new() { NAME = "user_name", VALUE = userNameNik },
                    new() { NAME = "user_nik", VALUE = userNameNik },
                    new() { NAME = "password", VALUE = password }
                }
            );
            return !string.IsNullOrEmpty(loggedInUsername);
        }

        public async Task<string> GetURLWebService(string webType) {
            return await this._orapg.ExecScalarAsync<string>(
                $@"SELECT WEB_URL FROM DC_WEBSERVICE_T WHERE WEB_TYPE = :web_type",
                new List<CDbQueryParamBind> {
                    new() { NAME = "web_type", VALUE = webType }
                }
            );
        }

        public async Task<bool> SaveKafkaToTable(string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName) {
            return await this._orapg.ExecQueryAsync($@"
                INSERT INTO {logTableName} (TPC, OFFS, PARTT, KEY, VAL, TMSTAMP)
                VALUES (:tpc, :offs, :partt, :key, :value, :tmstmp)
            ", new List<CDbQueryParamBind> {
                new() { NAME = "tpc", VALUE = topic },
                new() { NAME = "offs", VALUE = offset },
                new() { NAME = "partt", VALUE = partition },
                new() { NAME = "key", VALUE = msg.Key },
                new() { NAME = "value", VALUE = msg.Value },
                new() { NAME = "tmstmp", VALUE = msg.Timestamp.UtcDateTime }
            });
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
        // Sepertinya Yang Ini Akan Kurang Berguna
        // Karena Dapat Akses Langsung Ke Database
        // Cuma Tahu `CDatabase` Tidak Tahu Jenis `Postgre` / `Oracle`
        //
        // IDictionary<string, CDatabase> dbCon = await GetListBranchDbConnection("G001");
        // var res = dbCon["G055"].ExecScalarAsync<...>(...);
        //
        public async Task<IDictionary<string, (bool, CDatabase)>> GetListBranchDbConnection(string kodeDcInduk) {
            IDictionary<string, (bool, CDatabase)> dbCons = new Dictionary<string, (bool, CDatabase)>();

            string kodeDc = await this.GetKodeDc();
            DC_TABEL_V dc = await this._orapg.Set<DC_TABEL_V>().Where(d => d.TBL_DC_KODE.ToUpper() == kodeDc.ToUpper()).FirstOrDefaultAsync();
            if (kodeDc.ToUpper() == "DCHO" || dc.TBL_JENIS_DC.ToUpper() != "INDUK") {
                throw new TidakMemenuhiException("Khusus DC Induk");
            }

            List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation(kodeDcInduk);
            foreach (DC_TABEL_V dbi in dbInfo) {
                CDatabase dbCon = null;
                bool isPostgre = dbi.FLAG_DBPG?.ToUpper() == "Y";
                if (isPostgre) {
                    dbCon = this._postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                }
                else {
                    dbCon = this._oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT, dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                }

                dbCons.Add(dbi.TBL_DC_KODE.ToUpper(), (isPostgre, dbCon));
            }

            return dbCons;
        }

        public async Task<(bool, CDatabase, CDatabase)> OpenConnectionToDcFromHo(string kodeDcTarget) {
            CDatabase dbConHo = null;
            CDatabase dbOraPgDc = null;
            CDatabase dbSqlDc = null;
            bool dbIsUsingPostgre = false;

            string kodeDcSekarang = await this.GetKodeDc();
            if (kodeDcSekarang.ToUpper() != "DCHO") {
                List<DC_TABEL_V> dbInfo = await this.GetListBranchDbInformation("DCHO");
                DC_TABEL_V dcho = dbInfo.FirstOrDefault();
                dbConHo = this._oracle.NewExternalConnection(dcho.IP_DB, dcho.DB_PORT.ToString(), dcho.DB_USER_NAME, dcho.DB_PASSWORD, dcho.DB_SID);
            }
            else {
                dbConHo = (CDatabase) this._orapg;
            }

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

            return (dbIsUsingPostgre, dbOraPgDc, dbSqlDc);
        }

        public async Task GetDcApiPathAppFromHo(HttpRequest request, string dcKode, Action<string, Uri> Callback) {
            string kodeDcSekarang = await this.GetKodeDc();
            if (kodeDcSekarang.ToUpper() != "DCHO") {
                throw new TidakMemenuhiException("Khusus HO!");
            }

            DataTable dt = await this._orapg.GetDataTableAsync($@"
                SELECT
                    a.dc_kode,
                    a.ip_nginx,
                    b.api_path
                FROM
                    dc_tabel_ip_t a
                    LEFT JOIN api_dc_t b ON (
                        a.dc_kode = b.dc_kode
                        AND UPPER(b.app_name) = :app_name
                    )
                WHERE
                    a.dc_kode = :kode_dc
            ", new List<CDbQueryParamBind>() {
                new() { NAME = "app_name", VALUE = this._as.AppName.ToUpper() },
                new() { NAME = "kode_dc", VALUE = dcKode.ToUpper() }
            });
            var listApiDcs = dt.ToList<ListApiDc>();

            ListApiDc dbi = listApiDcs.FirstOrDefault();
            if (dbi == null || string.IsNullOrEmpty(dbi?.IP_NGINX)) {
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
                var urlApiDc = new Uri($"http://{dbi.IP_NGINX}{pathApiDc}{request.QueryString.Value}");

                // API Key Khusus Bypass ~ Case Sensitive
                System.Collections.Specialized.NameValueCollection queryApiDc = HttpUtility.ParseQueryString(urlApiDc.Query);
                queryApiDc.Set("key", this._as.AppName);
                queryApiDc.Set("mask_ip", request.HttpContext.Connection.RemoteIpAddress.ToString());

                var uriBuilder = new UriBuilder(urlApiDc) {
                    Query = queryApiDc.ToString()
                };

                Callback(null, uriBuilder.Uri);
            }
        }

    }

}
