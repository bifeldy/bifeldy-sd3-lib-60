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
 *              :: Instance Sqlite
 * 
 */

using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Databases {

    public interface ISqlite : IDatabase {
        CSqlite NewExternalConnection(string dbName);
        CSqlite CloneConnection();
    }

    public sealed class CSqlite : CDatabase, ISqlite {

        public CSqlite (
            DbContextOptions<CSqlite> options,
            ILogger<CSqlite> logger,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IConverterService cs,
            IGlobalService gs
        ) : base(options, logger, envVar, @as, cs, gs) {
            this.InitializeConnection();
            this.SetCommandTimeout();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) {
            _ = options.UseSqlite(this.DbConnectionString)
                // .LogTo(s => Console.WriteLine(s))
                .EnableDetailedErrors(this._as.DebugMode)
                .EnableSensitiveDataLogging(this._as.DebugMode);
        }

        public void InitializeConnection(string dbName = null) {
            this.DbName = dbName ?? Path.Combine(this._as.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, $"{this._as.AppName}.db");
            this.DbConnectionString = $"Data Source={this.DbName}";
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

                    bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(pValType) && pValType != typeof(string);
                    if (isEnumerable) {
                        string bindStr = string.Empty;
                        int id = 1;
                        foreach (dynamic data in pVal) {
                            if (!string.IsNullOrEmpty(bindStr)) {
                                bindStr += ", ";
                            }

                            bindStr += $"{prefix}{pName}_{id}";
                            _ = cmd.Parameters.Add(new SqliteParameter() {
                                ParameterName = $"{pName}_{id}",
                                Value = data ?? DBNull.Value
                            });
                            id++;
                        }

                        var regex = new Regex($"{prefix}{pName}");
                        cmd.CommandText = regex.Replace(cmd.CommandText, bindStr, 1);
                    }
                    else {
                        var param = new SqliteParameter() {
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
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = $@"SELECT * FROM {tableName} LIMIT 1";
            cmd.CommandType = CommandType.Text;
            return await this.GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetDataTableAsync(cmd, token);
        }

        public override IAsyncEnumerable<T> GetAsyncEnumerable<T>(string queryString, List<CDbQueryParamBind> bindParam = null, [EnumeratorCancellation] CancellationToken token = default, Action<T> callback = null, int commandTimeoutSeconds = 3600) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return this.GetAsyncEnumerable(cmd, token, callback);
        }

        public override async Task<List<T>> GetListAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, CancellationToken token = default, Action<T> callback = null, int commandTimeoutSeconds = 3600) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetListAsync(cmd, token, callback);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecScalarAsync<T>(cmd, token);
        }

        public override async Task<int> ExecQueryWithResultAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryWithResultAsync(cmd, token);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryAsync(cmd, minRowsAffected, shouldEqualMinRowsAffected, token);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            await Task.Delay(0);
            throw new Exception("Sqlite Tidak Memiliki Stored Procedure");
        }

        public override async Task<bool> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            bool result = false;
            Exception exception = null;

            try {
                if (string.IsNullOrEmpty(tableName)) {
                    throw new Exception("Target Tabel Tidak Ditemukan");
                }

                int colCount = dataTable.Columns.Count;

                var types = new Type[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);

                cmd.CommandText = $"SELECT * FROM {tableName} WHERE 1 = 0";
                using (var rdr = (SqliteDataReader) await this.ExecReaderAsync(cmd, CommandBehavior.Default, token)) {
                    if (rdr.FieldCount != colCount) {
                        throw new Exception("Jumlah Kolom Tabel Tidak Sama");
                    }

                    DataColumnCollection columns = rdr.GetSchemaTable().Columns;
                    for (int i = 0; i < colCount; i++) {
                        types[i] = columns[i].DataType;
                        lengths[i] = columns[i].MaxLength;
                        fieldNames[i] = columns[i].ColumnName;
                    }
                }

                var param = new List<CDbQueryParamBind>();
                var sB = new StringBuilder($"INSERT INTO {tableName} (");

                string sbHeader = string.Empty;
                for (int c = 0; c < colCount; c++) {
                    if (!string.IsNullOrEmpty(sbHeader)) {
                        sbHeader += ", ";
                    }

                    sbHeader += fieldNames[c];
                }

                _ = sB.Append(sbHeader + ") VALUES (");
                string sbRow = "(";
                for (int r = 0; r < dataTable.Rows.Count; r++) {
                    if (sbRow.EndsWith(")")) {
                        sbRow += ", (";
                    }

                    string sbColumn = string.Empty;
                    for (int c = 0; c < colCount; c++) {
                        if (!string.IsNullOrEmpty(sbColumn)) {
                            sbColumn += ", ";
                        }

                        string paramKey = $"{fieldNames[c]}_{r}";
                        sbColumn += paramKey;
                        param.Add(new CDbQueryParamBind {
                            NAME = paramKey,
                            VALUE = dataTable.Rows[r][fieldNames[c]]
                        });
                    }

                    sbRow += $"{sbColumn} )";
                }

                _ = sB.Append(sbRow);

                string query = sB.ToString();
                _ = await this.TransactionStart();
                bool run = await this.ExecQueryAsync(query, param);
                _ = this.TransactionCommit();

                result = run;
            }
            catch (Exception ex) {
                await this.TransactionRollback();
                this._logger.LogError("[PG_BULK_INSERT] {ex}", ex.Message);
                exception = ex;
            }

            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        public override async Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null, CommandBehavior commandBehavior = CommandBehavior.Default, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecReaderAsync(cmd, commandBehavior, token);
        }

        public override async Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (SqliteCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.RetrieveBlob(cmd, stringPathDownload, stringCustomSingleFileName, encoding ?? Encoding.UTF8, token);
        }

        public CSqlite NewExternalConnection(string dbName) {
            var sqlite = (CSqlite) this.Clone();
            sqlite.InitializeConnection(dbName);
            sqlite.ReSetConnectionString();
            return sqlite;
        }

        public CSqlite CloneConnection() {
            var sqlite = (CSqlite) this.Clone();
            sqlite.InitializeConnection(this.DbName);
            sqlite.ReSetConnectionString();
            sqlite.SetCommandTimeout();
            return sqlite;
        }

    }

}
