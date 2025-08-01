﻿/**
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
using System.Text;
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
        CMsSQL CloneConnection();
    }

    public sealed class CMsSQL : CDatabase, IMsSQL {

        public CMsSQL (
            DbContextOptions<CMsSQL> options,
            ILogger<CMsSQL> logger,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IConverterService cs,
            IGlobalService gs
        ) : base(options, logger, envVar, @as, cs, gs) {
            this.InitializeConnection();
            this.SetCommandTimeout();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) {
            _ = options.UseSqlServer(this.DbConnectionString)
                // .LogTo(s => Console.WriteLine(s))
                .EnableDetailedErrors(this._as.DebugMode)
                .EnableSensitiveDataLogging(this._as.DebugMode);
        }

        public void InitializeConnection(string dbIpAddrss = null, string dbName = null, string dbUsername = null, string dbPassword = null) {
            this.DbIpAddrss = dbIpAddrss ?? this._as.GetVariabel("IPSql", this._envVar.KUNCI_GXXX);
            this.DbName = dbName ?? this._as.GetVariabel("DatabaseSql", this._envVar.KUNCI_GXXX);
            this.DbUsername = dbUsername ?? this._as.GetVariabel("UserSql", this._envVar.KUNCI_GXXX);
            this.DbPassword = dbPassword ?? this._as.GetVariabel("PasswordSql", this._envVar.KUNCI_GXXX);
            this.DbConnectionString = $"Data Source={this.DbIpAddrss};Initial Catalog={this.DbName};User ID={this.DbUsername};Password={this.DbPassword};Connection Timeout=180;"; // 3 Minutes
        }

        protected override void BindQueryParameter(DbCommand cmd, List<CDbQueryParamBind> parameters) {
            char prefix = '@';
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
                            _ = cmd.Parameters.Add(new SqlParameter() {
                                ParameterName = $"{pName}_{id}",
                                Value = data ?? DBNull.Value
                            });
                            id++;
                        }

                        var regex = new Regex($"{prefix}{pName}");
                        cmd.CommandText = regex.Replace(cmd.CommandText, bindStr, 1);
                    }
                    else {
                        var param = new SqlParameter() {
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
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = $@"SELECT * FROM {tableName} LIMIT 1";
            cmd.CommandType = CommandType.Text;
            return await this.GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetDataTableAsync(cmd, token);
        }

        public override async Task<List<T>> GetListAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, CancellationToken token = default, Action<T> callback = null, int commandTimeoutSeconds = 3600) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetListAsync(cmd, token, callback);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecScalarAsync<T>(cmd, token);
        }

        public override async Task<int> ExecQueryWithResultAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryWithResultAsync(cmd, token);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryAsync(cmd, minRowsAffected, shouldEqualMinRowsAffected, token);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
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
                using (var dbBulkCopy = new SqlBulkCopy((SqlConnection) this.GetConnection()) {
                    DestinationTableName = tableName
                }) {
                    await dbBulkCopy.WriteToServerAsync(dataTable, token);
                    result = true;
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[SQL_BULK_INSERT] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        public override async Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null, CommandBehavior commandBehavior = CommandBehavior.Default, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecReaderAsync(cmd, commandBehavior, token);
        }

        public override async Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.RetrieveBlob(cmd, stringPathDownload, stringCustomSingleFileName, encoding ?? Encoding.UTF8, token);
        }

        public CMsSQL NewExternalConnection(string dbIpAddrss, string dbUsername, string dbPassword, string dbName) {
            var mssql = (CMsSQL) this.Clone();
            mssql.InitializeConnection(dbIpAddrss, dbUsername, dbPassword, dbName);
            mssql.ReSetConnectionString();
            return mssql;
        }

        public CMsSQL CloneConnection() {
            var mssql = (CMsSQL) this.Clone();
            mssql.InitializeConnection(this.DbIpAddrss, this.DbUsername, this.DbPassword, this.DbName);
            mssql.ReSetConnectionString();
            mssql.SetCommandTimeout();
            return mssql;
        }

    }

}
