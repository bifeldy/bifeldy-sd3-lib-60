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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Tables;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Repositories
{

    public interface IGeneralRepository : IRepository {
        string DbName { get; }
        Task<string> GetURLWebService(string webType);
        Task<bool> OraPg_AlterTable_AddColumnIfNotExist(string tableName, string columnName, string columnType);
    }

    public class CGeneralRepository : CRepository, IGeneralRepository
    {

        private readonly EnvVar _envVar;

        private readonly ILogger<CGeneralRepository> _logger;
        private readonly IApplicationService _as;
        private readonly IHttpService _http;
        private readonly IConverterService _converter;

        private readonly IOracle _oracle;
        private readonly IPostgres _postgres;
        private readonly IOraPg _orapg;
        private readonly IMsSQL _mssql;

        private IDictionary<
            string, IDictionary<
                string, CDatabase
            >
        > BranchConnectionInfo { get; } = new Dictionary<
            string, IDictionary<
                string, CDatabase
            >
        >();

        public CGeneralRepository(
            IOptions<EnvVar> envVar,
            ILogger<CGeneralRepository> logger,
            IApplicationService @as,
            IHttpService http,
            IConverterService converter,
            IOracle oracle,
            IPostgres postgres,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            _envVar = envVar.Value;
            _logger = logger;
            _as = @as;
            _http = http;
            _converter = converter;
            _oracle = oracle;
            _postgres = postgres;
            _orapg = orapg;
            _mssql = mssql;
        }

        /** Custom Queries */

        public string DbName
        {
            get
            {
                string FullDbName = string.Empty;
                try
                {
                    FullDbName += _orapg.DbName;
                }
                catch {
                    FullDbName += "-";
                }
                FullDbName += " / ";
                try {
                    FullDbName += _mssql.DbName;
                }
                catch {
                    FullDbName += "-";
                }
                return FullDbName;
            }
        }

        /* ** */

        public async Task<string> CekVersi() {
            if (_as.DebugMode) {
                return "OKE";
            }
            else {
                try {
                    string res1 = await _orapg.ExecScalarAsync<string>(
                        $@"
                            SELECT
                                CASE
                                    WHEN COALESCE(aprove, 'N') = 'Y' AND {(
                                            _envVar.IS_USING_POSTGRES ?
                                            "COALESCE(tgl_berlaku, NOW())::DATE <= CURRENT_DATE" :
                                            "TRUNC(COALESCE(tgl_berlaku, SYSDATE)) <= TRUNC(SYSDATE)"
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
                            new CDbQueryParamBind { NAME = "dc_kode", VALUE = await GetKodeDc() },
                            new CDbQueryParamBind { NAME = "nama_prog", VALUE = $"%{_as.AppName}%" }
                        }
                    );
                    if (string.IsNullOrEmpty(res1)) {
                        return $"Program :: {_as.AppName}" + Environment.NewLine + "Belum Terdaftar Di Master Program DC";
                    }
                    if (res1 == _as.AppVersion) {
                        return "OKE";
                    }
                    else {
                        return $"Versi Program :: {_as.AppName}" + Environment.NewLine + $"Tidak Sama Dengan Master Program = v{res1}";
                    }
                }
                catch (Exception ex1) {
                    return ex1.Message;
                }
            }
        }

        public async Task<bool> LoginUser(string userNameNik, string password) {
            string loggedInUsername = await _orapg.ExecScalarAsync<string>(
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
                    new CDbQueryParamBind { NAME = "user_name", VALUE = userNameNik },
                    new CDbQueryParamBind { NAME = "user_nik", VALUE = userNameNik },
                    new CDbQueryParamBind { NAME = "password", VALUE = password }
                }
            );
            return !string.IsNullOrEmpty(loggedInUsername);
        }

        public async Task<string> GetURLWebService(string webType) {
            return await _orapg.ExecScalarAsync<string>(
                $@"SELECT WEB_URL FROM DC_WEBSERVICE_T WHERE WEB_TYPE = :web_type",
                new List<CDbQueryParamBind> {
                    new CDbQueryParamBind { NAME = "web_type", VALUE = webType }
                }
            );
        }

        /* ** */

        // Bisa Kena SQL Injection
        public async Task<bool> OraPg_AlterTable_AddColumnIfNotExist(string tableName, string columnName, string columnType) {
            List<string> cols = new List<string>();
            DataColumnCollection columns = await _orapg.GetAllColumnTableAsync(tableName);
            foreach (DataColumn col in columns) {
                cols.Add(col.ColumnName.ToUpper());
            }
            if (!cols.Contains(columnName.ToUpper())) {
                return await _orapg.ExecQueryAsync($@"
                    ALTER TABLE {tableName}
                        ADD {(_envVar.IS_USING_POSTGRES ? "COLUMN" : "(")}
                            {columnName} {columnType}
                        {(_envVar.IS_USING_POSTGRES ? "" : ")")}
                ");
            }
            return false;
        }

        /* ** */

        public async Task<List<DC_TABEL_V>> GetListBranchDbInformation(string kodeDcInduk) {
            string url = await GetURLWebService("SYNCHO") ?? _envVar.WS_SYNCHO;
            url += kodeDcInduk;

            HttpResponseMessage httpResponse = await _http.PostData(url, null);
            string httpResString = await httpResponse.Content.ReadAsStringAsync();

            return _converter.JsonToObject<List<DC_TABEL_V>>(httpResString);
        }

        //
        // Sepertinya Yang Ini Akan Kurang Berguna
        // Karena Dapat Akses Langsung Ke Database
        // Cuma Tahu `CDatabase` Tidak Tahu Jenis `Postgre` / `Oracle`
        //
        // IDictionary<string, CDatabase> dbCon = await GetListBranchDbConnection("G001");
        // var res = dbCon["G055"].ExecScalarAsync<...>(...);
        //

        public async Task<IDictionary<string, CDatabase>> GetListBranchDbConnection(string kodeDcInduk) {
            IDictionary<string, CDatabase> dbCons = new Dictionary<string, CDatabase>();

            try {
                List<DC_TABEL_V> dbInfo = await GetListBranchDbInformation(kodeDcInduk);
                foreach (DC_TABEL_V dbi in dbInfo) {
                    CDatabase dbCon;
                    if (dbi.FLAG_DBPG == "Y") {
                        dbCon = _postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                    }
                    else {
                        dbCon = _oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT, dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                    }
                    dbCons.Add(dbi.TBL_DC_KODE, dbCon);
                }
                BranchConnectionInfo[kodeDcInduk] = dbCons;
            }
            catch (Exception ex) {
                _logger.LogError($"[BRANCH_GET_LIST_ERROR] {ex.Message}");
            }

            return BranchConnectionInfo[kodeDcInduk];
        }

    }

}
