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
 *              :: Instance Microsoft SQL Server
 * 
 */

using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Databases {

    public interface IMsSQL : IDatabase {
        CMsSQL NewExternalConnection(string dbIpAddrss, string dbUsername, string dbPassword, string dbName);
    }

    public sealed class CMsSQL : CDatabase, IMsSQL {

        private readonly ILogger<CMsSQL> _logger;
        private readonly EnvVar _envVar;
        private readonly IApplicationService _as;

        public CMsSQL (
            DbContextOptions<CMsSQL> options,
            ILogger<CMsSQL> logger,
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
            options.UseSqlServer(DbConnectionString)
                .LogTo(s => Console.WriteLine(s))
                .EnableDetailedErrors(_as.DebugMode)
                .EnableSensitiveDataLogging(_as.DebugMode);
        }

        public void InitializeConnection(string dbIpAddrss = null, string dbName = null, string dbUsername = null, string dbPassword = null) {
            DbIpAddrss = dbIpAddrss ?? _as.GetVariabel("IPSql", _envVar.KUNCI_GXXX);
            DbName = dbName ?? _as.GetVariabel("DatabaseSql", _envVar.KUNCI_GXXX);
            DbUsername = dbUsername ?? _as.GetVariabel("UserSql", _envVar.KUNCI_GXXX);
            DbPassword = dbPassword ?? _as.GetVariabel("PasswordSql", _envVar.KUNCI_GXXX);
            DbConnectionString = $"Data Source={DbIpAddrss};Initial Catalog={DbName};User ID={DbUsername};Password={DbPassword};Connection Timeout=180;"; // 3 menit
        }

        protected override void BindQueryParameter(DbCommand cmd, List<CDbQueryParamBind> parameters) {
            char prefix = '@';
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
                            cmd.Parameters.Add(new SqlParameter {
                                ParameterName = $"{pName}_{id}",
                                Value = data ?? DBNull.Value
                            });
                            id++;
                        }
                        Regex regex = new Regex($"{prefix}{pName}");
                        cmd.CommandText = regex.Replace(cmd.CommandText, bindStr, 1);
                    }
                    else {
                        SqlParameter param = new SqlParameter {
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
            SqlCommand cmd = (SqlCommand) CreateCommand();
            cmd.CommandText = $@"SELECT * FROM {tableName} LIMIT 1";
            cmd.CommandType = CommandType.Text;
            return await GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            SqlCommand cmd = (SqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await GetDataTableAsync(cmd);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null) {
            SqlCommand cmd = (SqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecScalarAsync<T>(cmd);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            SqlCommand cmd = (SqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecQueryAsync(cmd);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null) {
            SqlCommand cmd = (SqlCommand) CreateCommand();
            cmd.CommandText = procedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            BindQueryParameter(cmd, bindParam);
            return await ExecProcedureAsync(cmd);
        }

        public override async Task<bool> BulkInsertInto(string tableName, DataTable dataTable) {
            bool result = false;
            Exception exception = null;
            SqlBulkCopy dbBulkCopy = null;
            try {
                await OpenConnection();
                dbBulkCopy = new SqlBulkCopy((SqlConnection) GetConnection()) {
                    DestinationTableName = tableName
                };
                await dbBulkCopy.WriteToServerAsync(dataTable);
                result = true;
            }
            catch (Exception ex) {
                _logger.LogError($"[SQL_BULK_INSERT] {ex.Message}");
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
            SqlCommand cmd = (SqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecReaderAsync(cmd);
        }

        public override async Task<string> RetrieveBlob(string stringPathDownload, string stringFileName, string queryString, List<CDbQueryParamBind> bindParam = null) {
            SqlCommand cmd = (SqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await RetrieveBlob(cmd, stringPathDownload, stringFileName);
        }

        public CMsSQL NewExternalConnection(string dbIpAddrss, string dbUsername, string dbPassword, string dbName) {
            CMsSQL mssql = (CMsSQL) Clone();
            mssql.InitializeConnection(dbIpAddrss, dbUsername, dbPassword, dbName);
            return mssql;
        }

    }

}
