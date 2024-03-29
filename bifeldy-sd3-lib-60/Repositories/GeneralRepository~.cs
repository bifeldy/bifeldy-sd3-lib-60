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

using System.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Confluent.Kafka;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Tables;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IGeneralRepository : IRepository {
        string DbName { get; }
        Task<string> GetURLWebService(string webType);
        Task<bool> SaveKafkaToTable(string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName);
        Task<List<DC_TABEL_V>> GetListBranchDbInformation(string kodeDcInduk);
        Task<IDictionary<string, (bool, CDatabase)>> GetListBranchDbConnection(string kodeDcInduk);
        Task<(bool, CDatabase, CDatabase)> OpenConnectionToDcFromHo(string kodeDcTarget);
    }

    public class CGeneralRepository : CRepository, IGeneralRepository {

        private readonly EnvVar _envVar;

        private readonly ILogger<CGeneralRepository> _logger;
        private readonly IApplicationService _as;
        private readonly IHttpService _http;
        private readonly IConverterService _converter;

        private readonly IOracle _oracle;
        private readonly IPostgres _postgres;
        private readonly IOraPg _orapg;
        private readonly IMsSQL _mssql;

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

        public string DbName {
            get {
                string FullDbName = string.Empty;
                try {
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

        public async Task<bool> SaveKafkaToTable(string topic, decimal offset, decimal partition, Message<string, string> msg, string logTableName) {
            return await _orapg.ExecQueryAsync($@"
                INSERT INTO {logTableName} (TPC, OFFS, PARTT, KEY, VAL, TMSTAMP)
                VALUES (:tpc, :offs, :partt, :key, :value, :tmstmp)
            ", new List<CDbQueryParamBind> {
                new CDbQueryParamBind { NAME = "tpc", VALUE = topic },
                new CDbQueryParamBind { NAME = "offs", VALUE = offset },
                new CDbQueryParamBind { NAME = "partt", VALUE = partition },
                new CDbQueryParamBind { NAME = "key", VALUE = msg.Key },
                new CDbQueryParamBind { NAME = "value", VALUE = msg.Value },
                new CDbQueryParamBind { NAME = "tmstmp", VALUE = msg.Timestamp.UtcDateTime }
            });
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
        public async Task<IDictionary<string, (bool, CDatabase)>> GetListBranchDbConnection(string kodeDcInduk) {
            IDictionary<string, (bool, CDatabase)> dbCons = new Dictionary<string, (bool, CDatabase)>();

            string kodeDc = await GetKodeDc();
            DC_TABEL_V dc = await _orapg.Set<DC_TABEL_V>().Where(d => d.TBL_DC_KODE.ToUpper() == kodeDc.ToUpper()).FirstOrDefaultAsync();
            if (kodeDc.ToUpper() == "DCHO" || dc.TBL_JENIS_DC.ToUpper() != "INDUK") {
                throw new Exception("Khusus DC Induk");
            }

            List<DC_TABEL_V> dbInfo = await GetListBranchDbInformation(kodeDcInduk);
            foreach (DC_TABEL_V dbi in dbInfo) {
                CDatabase dbCon = null;
                bool isPostgre = dbi.FLAG_DBPG?.ToUpper() == "Y";
                if (isPostgre) {
                    dbCon = _postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                }
                else {
                    dbCon = _oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT, dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                }
                dbCons.Add(dbi.TBL_DC_KODE.ToUpper(), (isPostgre, dbCon));
            }

            return dbCons;
        }

        public async Task<(bool, CDatabase, CDatabase)> OpenConnectionToDcFromHo(string kodeDcTarget) {
            CDatabase dbConHo = null;
            CDatabase dbOraPgDc = null;
            CDatabase dbSqlDc = null;
            bool isDcPg = false;

            string kodeDcSekarang = await GetKodeDc();
            if (kodeDcSekarang.ToUpper() != "DCHO") {
                List<DC_TABEL_V> dbInfo = await GetListBranchDbInformation("DCHO");
                DC_TABEL_V dcho = dbInfo.FirstOrDefault();
                dbConHo = _oracle.NewExternalConnection(dcho.IP_DB, dcho.DB_PORT.ToString(), dcho.DB_USER_NAME, dcho.DB_PASSWORD, dcho.DB_SID);
            }
            else {
                dbConHo = (CDatabase) _orapg;
            }

            DC_TABEL_IP_T dbi = dbConHo.Set<DC_TABEL_IP_T>().Where(d => d.DC_KODE.ToUpper() == kodeDcTarget.ToUpper()).SingleOrDefault();
            if (dbi != null) {
                isDcPg = dbi.FLAG_DBPG?.ToUpper() == "Y";
                if (isDcPg) {
                    dbOraPgDc = _postgres.NewExternalConnection(dbi.DBPG_IP, dbi.DBPG_PORT, dbi.DBPG_USER, dbi.DBPG_PASS, dbi.DBPG_NAME);
                }
                else {
                    dbOraPgDc = _oracle.NewExternalConnection(dbi.IP_DB, dbi.DB_PORT.ToString(), dbi.DB_USER_NAME, dbi.DB_PASSWORD, dbi.DB_SID);
                }
                dbSqlDc = _mssql.NewExternalConnection(dbi.DB_IP_SQL, dbi.DB_USER_SQL, dbi.DB_PWD_SQL, dbi.SCHEMA_DPD);
            }

            return (isDcPg, dbOraPgDc, dbSqlDc);
        }

    }

}
