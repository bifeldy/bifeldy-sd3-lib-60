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
using System.Text;
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
        COracle NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbNameSid);
        COracle CloneConnection();
    }

    public sealed class COracle : CDatabase, IOracle {

        public COracle(
            DbContextOptions<COracle> options,
            ILogger<COracle> logger,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IConverterService cs,
            IGlobalService gs
        ) : base(options, logger, envVar, @as, cs, gs) {
            this.InitializeConnection();
            this.SetCommandTimeout();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) {
            _ = options.UseOracle(this.DbConnectionString, opt => {
                    _ = opt.UseOracleSQLCompatibility("11");
                })
                // .LogTo(s => Console.WriteLine(s))
                .EnableDetailedErrors(this._as.DebugMode)
                .EnableSensitiveDataLogging(this._as.DebugMode);
        }

        public void InitializeConnection(string dbUsername = null, string dbPassword = null, string dbTnsOdp = null) {
            string _dbTnsOdp = dbTnsOdp ?? this._as.GetVariabel("ODPOrcl", this._envVar.KUNCI_GXXX);
            if (!string.IsNullOrEmpty(_dbTnsOdp)) {
                _dbTnsOdp = Regex.Replace(_dbTnsOdp, @"\s+", "");
            }

            this.DbTnsOdp = _dbTnsOdp;
            string _dbName = null;
            if (!string.IsNullOrEmpty(this.DbTnsOdp)) {
                _dbName = this.DbTnsOdp.Split(new string[] { "SERVICE_NAME=" }, StringSplitOptions.None)[1].Split(new string[] { ")" }, StringSplitOptions.None)[0];
            }

            this.DbName = _dbName;
            this.DbUsername = dbUsername ?? this._as.GetVariabel("UserOrcl", this._envVar.KUNCI_GXXX);
            this.DbPassword = dbPassword ?? this._as.GetVariabel("PasswordOrcl", this._envVar.KUNCI_GXXX);
            this.DbConnectionString = $"Data Source={this.DbTnsOdp};User ID={this.DbUsername};Password={this.DbPassword};Connection Timeout=180;"; // 3 Minutes
        }

        protected override DbCommand CreateCommand(int commandTimeoutSeconds) {
            var cmd = (OracleCommand) base.CreateCommand(commandTimeoutSeconds);
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
                    string pName = parameters[i].NAME.StartsWith($"{prefix}") ? parameters[i].NAME[1..] : parameters[i].NAME;
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
                            _ = cmd.Parameters.Add(new OracleParameter() {
                                ParameterName = $"{pName}_{id}",
                                Value = data ?? DBNull.Value
                            });
                            id++;
                        }

                        var regex = new Regex($"{prefix}{pName}");
                        cmd.CommandText = regex.Replace(cmd.CommandText, bindStr, 1);
                    }
                    else {
                        var param = new OracleParameter() {
                            ParameterName = pName,
                            Value = pVal ?? DBNull.Value
                        };
                        if (parameters[i].SIZE > 0) {
                            param.Size = parameters[i].SIZE;
                        }

                        if (parameters[i].DIRECTION > 0) {
                            param.Direction = parameters[i].DIRECTION;
                        }

                        _ = cmd.Parameters.Add(param);
                    }
                }
            }

            this.LogQueryParameter(cmd, prefix);
        }

        public override async Task<DataColumnCollection> GetAllColumnTableAsync(string tableName, int commandTimeoutSeconds = 3600) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = $@"SELECT * FROM {tableName} WHERE ROWNUM <= 1";
            cmd.CommandType = CommandType.Text;
            return await this.GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetDataTableAsync(cmd, token);
        }

        public override async Task<List<T>> GetListAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, CancellationToken token = default, Action<T> callback = null, int commandTimeoutSeconds = 3600) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetListAsync(cmd, token, callback);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecScalarAsync<T>(cmd, token);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryAsync(cmd, minRowsAffected, shouldEqualMinRowsAffected, token);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = procedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecProcedureAsync(cmd, token);
        }

        public override async Task<bool> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            bool result = false;
            Exception exception = null;
            try {
                await this.OpenConnection();
                using (var dbBulkCopy = new OracleBulkCopy((OracleConnection) this.GetConnection()) {
                    DestinationTableName = tableName
                }) {
                    // Ga Ada Yang Async
                    dbBulkCopy.WriteToServer(dataTable /*, token */);
                    result = true;
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[ORA_BULK_INSERT] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        public override async Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null, CommandBehavior commandBehavior = CommandBehavior.Default, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecReaderAsync(cmd, commandBehavior, token);
        }

        public override async Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (OracleCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.RetrieveBlob(cmd, stringPathDownload, stringCustomSingleFileName, encoding ?? Encoding.UTF8, token);
        }

        public COracle NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbNameSid) {
            var oracle = (COracle) this.Clone();
            string dbTnsOdp = $"(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={dbIpAddrss})(PORT={dbPort})))(CONNECT_DATA=(SERVICE_NAME={dbNameSid})))";
            oracle.InitializeConnection(dbUsername, dbPassword, dbTnsOdp);
            oracle.ReSetConnectionString();
            return oracle;
        }

        public COracle CloneConnection() {
            var oracle = (COracle) this.Clone();
            oracle.InitializeConnection(this.DbUsername, this.DbPassword, this.DbTnsOdp);
            oracle.ReSetConnectionString();
            oracle.SetCommandTimeout();
            return oracle;
        }

    }

}
