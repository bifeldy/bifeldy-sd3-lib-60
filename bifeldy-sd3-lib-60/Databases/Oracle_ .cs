/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Turunan `CDatabase`
 *              :: Harap Didaftarkan Ke DI Container
 *              :: Instance Oracle
 * 
 */

using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Oracle.ManagedDataAccess.Client;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Databases {

    public interface IOracle : IOraPg {
        void InitializeConnection(string dbTnsOdp = null, string dbUsername = null, string dbPassword = null);
        COracle NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbNameSid);
    }

    public sealed class COracle : CDatabase, IOracle {

        private readonly ILogger<COracle> _logger;
        private readonly EnvVar _envVar;
        private readonly IApplicationService _as;

        public COracle(
            DbContextOptions<COracle> options,
            ILogger<COracle> logger,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IConverterService cs
        ) : base(options, envVar, logger, cs) {
            _logger = logger;
            _envVar = envVar.Value;
            _as = @as;
            // --
            InitializeConnection();
            // --
            Database.SetCommandTimeout(1800); // 30 Minute
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) {
            options.UseOracle(DbConnectionString, opt => {
                opt.UseOracleSQLCompatibility("11");
            });
        }

        public void InitializeConnection(string dbTnsOdp = null, string dbUsername = null, string dbPassword = null) {
            string _dbTnsOdp = dbTnsOdp ?? _as.GetVariabel("ODPOrcl", _envVar.KUNCI_GXXX);
            if (!string.IsNullOrEmpty(_dbTnsOdp)) {
                _dbTnsOdp = Regex.Replace(_dbTnsOdp, @"\s+", "");
            }
            DbTnsOdp = _dbTnsOdp;
            string _dbName = null;
            if (!string.IsNullOrEmpty(DbTnsOdp)) {
                _dbName = DbTnsOdp.Split(new string[] { "SERVICE_NAME=" }, StringSplitOptions.None)[1].Split(new string[] { ")" }, StringSplitOptions.None)[0];
            }
            DbName = _dbName;
            DbUsername = dbUsername ?? _as.GetVariabel("UserOrcl", _envVar.KUNCI_GXXX);
            DbPassword = dbPassword ?? _as.GetVariabel("PasswordOrcl", _envVar.KUNCI_GXXX);
            DbConnectionString = $"Data Source={DbTnsOdp};User ID={DbUsername};Password={DbPassword};";
        }

        protected override DbCommand CreateCommand() {
            OracleCommand cmd = (OracleCommand) base.CreateCommand();
            cmd.BindByName = true;
            cmd.InitialLOBFetchSize = -1;
            cmd.InitialLONGFetchSize = -1;
            return cmd;
        }

        protected override void BindQueryParameter(DbCommand cmd, List<CDbQueryParamBind> parameters) {
            char prefix = ':';
            cmd.Parameters.Clear();
            if (parameters != null) {
                for (int i = 0; i < parameters.Count; i++) {
                    string pName = parameters[i].NAME.StartsWith($"{prefix}") ? parameters[i].NAME.Substring(1) : parameters[i].NAME;
                    if (string.IsNullOrEmpty(pName)) {
                        throw new Exception("Nama Parameter Wajib Diisi");
                    }
                    dynamic pVal = parameters[i].VALUE;
                    Type pValType = (pVal == null) ? typeof(DBNull) : pVal.GetType();
                    if (pValType.IsArray) {
                        string bindStr = string.Empty;
                        int id = 1;
                        foreach (dynamic data in pVal) {
                            if (!string.IsNullOrEmpty(bindStr)) {
                                bindStr += ", ";
                            }
                            bindStr += $"{prefix}{pName}_{id}";
                            cmd.Parameters.Add(new OracleParameter {
                                ParameterName = $"{pName}_{id}",
                                Value = data ?? DBNull.Value
                            });
                            id++;
                        }
                        Regex regex = new Regex($"{prefix}{pName}");
                        cmd.CommandText = regex.Replace(cmd.CommandText, bindStr, 1);
                    }
                    else {
                        OracleParameter param = new OracleParameter {
                            ParameterName = pName,
                            Value = pVal ?? DBNull.Value
                        };
                        if (parameters[i].SIZE > 0) {
                            param.Size = parameters[i].SIZE;
                        }
                        if (parameters[i].DIRECTION > 0) {
                            param.Direction = parameters[i].DIRECTION;
                        }
                        cmd.Parameters.Add(param);
                    }
                }
            }
            LogQueryParameter(cmd, prefix);
        }

        public override async Task<DataColumnCollection> GetAllColumnTableAsync(string tableName) {
            OracleCommand cmd = (OracleCommand) CreateCommand();
            cmd.CommandText = $@"SELECT * FROM {tableName} WHERE ROWNUM <= 1";
            cmd.CommandType = CommandType.Text;
            return await GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            OracleCommand cmd = (OracleCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await GetDataTableAsync(cmd);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null) {
            OracleCommand cmd = (OracleCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecScalarAsync<T>(cmd);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            OracleCommand cmd = (OracleCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecQueryAsync(cmd);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null) {
            OracleCommand cmd = (OracleCommand) CreateCommand();
            cmd.CommandText = procedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            BindQueryParameter(cmd, bindParam);
            return await ExecProcedureAsync(cmd);
        }

        public override async Task<bool> BulkInsertInto(string tableName, DataTable dataTable) {
            bool result = false;
            Exception exception = null;
            OracleBulkCopy dbBulkCopy = null;
            try {
                await OpenConnection();
                dbBulkCopy = new OracleBulkCopy((OracleConnection) GetConnection()) {
                    DestinationTableName = tableName
                };
                dbBulkCopy.WriteToServer(dataTable);
                result = true;
            }
            catch (Exception ex) {
                _logger.LogError($"[ORA_BULK_INSERT] {ex.Message}");
                exception = ex;
            }
            finally {
                if (dbBulkCopy != null) {
                    dbBulkCopy.Close();
                }
                await CloseConnection();
            }
            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        public override async Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            OracleCommand cmd = (OracleCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecReaderAsync(cmd);
        }

        public override async Task<string> RetrieveBlob(string stringPathDownload, string stringFileName, string queryString, List<CDbQueryParamBind> bindParam = null) {
            OracleCommand cmd = (OracleCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await RetrieveBlob(cmd, stringPathDownload, stringFileName);
        }

        public COracle NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbNameSid) {
            COracle oracle = (COracle) Clone();
            string dbTnsOdp = $"(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={dbIpAddrss})(PORT={dbPort})))(CONNECT_DATA=(SERVICE_NAME={dbNameSid})))";
            oracle.InitializeConnection(dbUsername, dbPassword, dbTnsOdp);
            return oracle;
        }

    }

}
