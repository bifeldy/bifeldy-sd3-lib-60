/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Class Database Bawaan
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 *              :: Hanya Untuk Inherit
 *              :: Mohon & Harap Tidak Digunakan
 * 
 */

using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Abstractions {

    public partial interface IDatabase {
        string DbUsername { get; } // Hanya Expose Get Saja
        string DbName { get; } // Hanya Expose Get Saja
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        object Clone();
        Task CloseConnection(bool force = false);
        Task<IDbContextTransaction> TransactionStart(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        Task TransactionCommit(DbTransaction useTrx = null, bool forceCloseConnection = false);
        Task TransactionRollback(DbTransaction useTrx = null, bool forceCloseConnection = false);
        Task<DataColumnCollection> GetAllColumnTableAsync(string tableName, int commandTimeoutSeconds = 3600);
        Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<List<T>> GetListAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, CancellationToken token = default, Action<T> callback = null, int commandTimeoutSeconds = 3600);
        Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<int> ExecQueryWithResultAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<bool> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<string> BulkGetCsv(string queryString, string delimiter, string filename, List<CDbQueryParamBind> bindParam = null, string outputFolderPath = null, bool useRawQueryWithoutParam = false, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null, CommandBehavior commandBehavior = CommandBehavior.Default, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
    }

    public abstract partial class CDatabase : DbContext, IDatabase, ICloneable {

        protected readonly EnvVar _envVar;

        protected readonly ILogger<CDatabase> _logger;
        protected readonly IConverterService _cs;
        protected readonly IApplicationService _as;
        protected readonly IGlobalService _gs;

        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DbIpAddrss { get; set; }
        public string DbPort { get; set; }
        public string DbName { get; set; }
        public string DbTnsOdp { get; set; }
        public string DbConnectionString { get; set; }

        public bool HasUnCommitRollbackSqlQuery => this.Database.CurrentTransaction != null;

        private DbCommand CurrentActiveCommandTransaction = null;

        public CDatabase(
            DbContextOptions options,
            ILogger<CDatabase> logger,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IConverterService cs,
            IGlobalService gs
        ) : base(options) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;
            this._cs = cs;
            this._gs = gs;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            var libAsm = Assembly.GetExecutingAssembly();
            var prgAsm = Assembly.GetEntryAssembly();

            modelBuilder.RegisterAllEntities<EntityTableView>(libAsm);
            modelBuilder.RegisterAllEntities<EntityTableView>(prgAsm);

            // DbSet<T> Case Sensitive ~
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes()) {
                string tblName = entityType.GetTableName();
                tblName = this._envVar.IS_USING_POSTGRES ? tblName.ToLower() : tblName.ToUpper();
                entityType.SetTableName(tblName);

                foreach (IMutableProperty property in entityType.GetProperties()) {
                    string colName = property.GetColumnBaseName();
                    colName = this._envVar.IS_USING_POSTGRES ? colName.ToLower() : colName.ToUpper();
                    property.SetColumnName(colName);
                }
            }
        }

        public object Clone() => this.MemberwiseClone();

        protected void ReSetConnectionString() => this.Database.SetConnectionString(this.DbConnectionString);

        protected virtual DbConnection GetConnection() => this.Database.GetDbConnection();

        protected virtual void SetCommandTimeout(int commandTimeoutSeconds = 3600) {
            this.Database.SetCommandTimeout(commandTimeoutSeconds); // 60 Minute
        }

        protected virtual DbCommand CreateCommand(int commandTimeoutSeconds) {
            if (this.Database.CurrentTransaction == null) {
                DbCommand cmd = this.GetConnection().CreateCommand();
                cmd.CommandTimeout = commandTimeoutSeconds; // 60 Minutes
                return cmd;
            }
            else {
                if (this.CurrentActiveCommandTransaction == null) {
                    this.CurrentActiveCommandTransaction = this.GetConnection().CreateCommand();
                    this.CurrentActiveCommandTransaction.CommandTimeout = commandTimeoutSeconds; // 60 Minutes
                }

                return this.CurrentActiveCommandTransaction;
            }
        }

        protected async Task OpenConnection() {
            if (this.Database.CurrentTransaction == null) {
                if (this.GetConnection().State != ConnectionState.Closed) {
                    throw new Exception("Koneksi Database Sedang Digunakan");
                }

                await this.Database.OpenConnectionAsync();
            }
        }

        /// <summary> Jangan Lupa Di Commit Atau Rollback Sebelum Menjalankan Ini </summary>
        public async Task CloseConnection(bool force = false) {
            if (this.Database.CurrentTransaction != null && force) {
                this.CurrentActiveCommandTransaction = null;
            }

            if (this.Database.CurrentTransaction == null || force) {
                if (this.GetConnection().State != ConnectionState.Closed) {
                    await this.Database.CloseConnectionAsync();
                }
            }
        }

        public async Task<IDbContextTransaction> TransactionStart(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) {
            await this.OpenConnection();
            return await this.Database.BeginTransactionAsync(isolationLevel);
        }

        public async Task TransactionCommit(DbTransaction useTrx = null, bool forceCloseConnection = false) {
            DbTransaction trx = useTrx ?? this.Database.CurrentTransaction?.GetDbTransaction();
            if (trx != null) {
                IDbContextTransaction ctx = await this.Database.UseTransactionAsync(trx);
                await ctx.CommitAsync();
            }

            await this.CloseConnection(forceCloseConnection);
        }

        public async Task TransactionRollback(DbTransaction useTrx = null, bool closeConnection = false) {
            DbTransaction trx = useTrx ?? this.Database.CurrentTransaction?.GetDbTransaction();
            if (trx != null) {
                IDbContextTransaction ctx = await this.Database.UseTransactionAsync(trx);
                await ctx.RollbackAsync();
            }

            await this.CloseConnection(closeConnection);
        }

        protected void LogQueryParameter(DbCommand databaseCommand, char databaseParameterPrefix) {
            string sqlTextQueryParameters = databaseCommand.CommandText;
            for (int i = 0; i < databaseCommand.Parameters.Count; i++) {
                dynamic pVal = databaseCommand.Parameters[i].Value;

                Type pValType = pVal?.GetType();
                if (pValType == null || pValType == typeof(DBNull)) {
                    pVal = "NULL";
                }
                else if (pValType == typeof(string)) {
                    pVal = $"'{pVal}'";
                }
                else if (pValType == typeof(DateTime)) {
                    pVal = $"TO_TIMESTAMP('{((DateTime)pVal).ToLocalTime():yyyy-MM-dd HH:mm:ss}', 'yyyy-MM-dd HH24:mi:ss')";
                }

                var regex = new Regex($"{databaseParameterPrefix}{databaseCommand.Parameters[i].ParameterName}");
                sqlTextQueryParameters = regex.Replace(sqlTextQueryParameters, pVal.ToString(), 1);
            }

            sqlTextQueryParameters = sqlTextQueryParameters.Replace(Environment.NewLine, " ");
            sqlTextQueryParameters = Regex.Replace(sqlTextQueryParameters, @"\s+", " ");
            sqlTextQueryParameters = sqlTextQueryParameters.Trim();

            this._logger.LogInformation("[SQL_PARAMETERS] {sqlTextQueryParameters}", sqlTextQueryParameters);
        }

        protected virtual async Task<DataColumnCollection> GetAllColumnTableAsync(string tableName, DbCommand databaseCommand) {
            DataTable dt = await this.GetDataTableAsync(databaseCommand);
            return dt.Columns;
        }

        protected virtual async Task<DataTable> GetDataTableAsync(DbCommand databaseCommand, CancellationToken token = default) {
            var result = new DataTable();
            Exception exception = null;
            try {
                // await OpenConnection();
                // dataAdapter.Fill(result);
                using (DbDataReader dr = await this.ExecReaderAsync(databaseCommand, CommandBehavior.Default, token)) {
                    result.Load(dr);
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[DATA_TABLE] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<List<T>> GetListAsync<T>(DbCommand databaseCommand, CancellationToken token = default, Action<T> callback = null) {
            var result = new List<T>();
            Exception exception = null;
            try {
                using (DbDataReader dr = await this.ExecReaderAsync(databaseCommand, CommandBehavior.SequentialAccess, token)) {
                    result = dr.ToList(token, callback);
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[DATA_LIST] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<T> ExecScalarAsync<T>(DbCommand databaseCommand, CancellationToken token = default) {
            T result = default;
            Exception exception = null;
            try {
                await this.OpenConnection();
                object _obj = await databaseCommand.ExecuteScalarAsync(token);
                if (_obj != null && _obj != DBNull.Value) {
                    result = this._cs.ObjectToT<T>(_obj);
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[EXEC_SCALAR] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<int> ExecQueryWithResultAsync(DbCommand databaseCommand, CancellationToken token = default) {
            int result = 0;
            Exception exception = null;
            try {
                await this.OpenConnection();
                result = await databaseCommand.ExecuteNonQueryAsync(token);
            }
            catch (Exception ex) {
                this._logger.LogError("[EXEC_QUERY] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<bool> ExecQueryAsync(DbCommand databaseCommand, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, CancellationToken token = default) {
            int res = await this.ExecQueryWithResultAsync(databaseCommand, token);
            return shouldEqualMinRowsAffected ? res == minRowsAffected : res >= minRowsAffected;
        }

        protected virtual async Task<CDbExecProcResult> ExecProcedureAsync(DbCommand databaseCommand, CancellationToken token = default) {
            var result = new CDbExecProcResult() {
                STATUS = false,
                QUERY = databaseCommand.CommandText,
                PARAMETERS = databaseCommand.Parameters
            };
            Exception exception = null;
            try {
                await this.OpenConnection();
                result.STATUS = await databaseCommand.ExecuteNonQueryAsync(token) == -1;
                result.PARAMETERS = databaseCommand.Parameters;
            }
            catch (Exception ex) {
                this._logger.LogError("[EXEC_PROCEDURE] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        /// <summary> Saat Setelah Selesai Baca Dan Tidak Digunakan Lagi </summary>
        /// <summary> Bisa Pakai Manual Panggil Fungsi Close / Commit / Rollback Di Atas </summary>
        protected virtual async Task<DbDataReader> ExecReaderAsync(DbCommand databaseCommand, CommandBehavior commandBehavior, CancellationToken token = default) {
            DbDataReader result = null;
            Exception exception = null;
            try {
                await this.OpenConnection();
                result = await databaseCommand.ExecuteReaderAsync(commandBehavior, token);
            }
            catch (Exception ex) {
                this._logger.LogError("[EXEC_READER] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                // Kalau Koneksinya Di Close Dari Sini Tidak Akan Bisa Pakai Reader Untuk Baca Lagi
                // await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<List<string>> RetrieveBlob(DbCommand databaseCommand, string stringPathDownload, string stringFileName = null, Encoding encoding = null, CancellationToken token = default) {
            var result = new List<string>();
            Exception exception = null;
            try {
                string _oldCmdTxt = databaseCommand.CommandText;
                databaseCommand.CommandText = $"SELECT COUNT(*) FROM ( {_oldCmdTxt} ) RetrieveBlob_{DateTime.Now.Ticks}";
                ulong _totalFiles = await this.ExecScalarAsync<ulong>(databaseCommand, token);
                if (_totalFiles <= 0) {
                    throw new Exception("File Tidak Ditemukan");
                }

                databaseCommand.CommandText = _oldCmdTxt;
                using (DbDataReader rdrGetBlob = await this.ExecReaderAsync(databaseCommand, CommandBehavior.SequentialAccess, token)) {
                    if (string.IsNullOrEmpty(stringFileName) && rdrGetBlob.FieldCount != 2) {
                        throw new Exception($"Jika Nama File Kosong Maka Harus Berjumlah 2 Kolom{Environment.NewLine}SELECT kolom_blob_data, kolom_nama_file FROM ...");
                    }
                    else if (!string.IsNullOrEmpty(stringFileName) && rdrGetBlob.FieldCount > 1) {
                        throw new Exception($"Harus Berjumlah 1 Kolom{Environment.NewLine}SELECT kolom_blob_data FROM ...");
                    }

                    int bufferSize = 1024;
                    byte[] outByte = new byte[bufferSize];

                    while (await rdrGetBlob.ReadAsync(token)) {
                        string filePath = Path.Combine(stringPathDownload, stringFileName);

                        if (rdrGetBlob.FieldCount == 2) {
                            string fileMultipleName = rdrGetBlob.GetString(1);
                            if (string.IsNullOrEmpty(fileMultipleName)) {
                                fileMultipleName = $"{DateTime.Now.Ticks}";
                            }

                            filePath = Path.Combine(stringPathDownload, fileMultipleName);
                        }

                        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write)) {
                            using (var bw = new BinaryWriter(fs, encoding ?? Encoding.UTF8)) {
                                long startIndex = 0;
                                long retval = rdrGetBlob.GetBytes(0, startIndex, outByte, 0, bufferSize);

                                while (retval == bufferSize) {
                                    bw.Write(outByte);
                                    bw.Flush();
                                    startIndex += bufferSize;
                                    retval = rdrGetBlob.GetBytes(0, startIndex, outByte, 0, bufferSize);
                                }

                                if (retval > 0) {
                                    bw.Write(outByte, 0, (int) retval);
                                }

                                bw.Flush();
                            }
                        }

                        result.Add(filePath);
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[RETRIEVE_BLOB] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        public virtual async Task<string> BulkGetCsv(string queryString, string delimiter, string filename, List<CDbQueryParamBind> bindParam = null, string outputFolderPath = null, bool useRawQueryWithoutParamWithoutParam = false, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            string result = null;
            Exception exception = null;
            try {
                string tempPath = Path.Combine(outputFolderPath ?? this._gs.TempFolderPath, filename);
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }

                string sqlQuery = $"SELECT * FROM ({queryString}) alias_{DateTime.Now.Ticks}";
                using (DbDataReader rdr = await this.ExecReaderAsync(sqlQuery, bindParam, CommandBehavior.SequentialAccess, commandTimeoutSeconds, token)) {
                    await rdr.ToCsv(delimiter, tempPath, includeHeader, useDoubleQuote, allUppercase, encoding ?? Encoding.UTF8, token);
                }

                string realPath = Path.Combine(outputFolderPath ?? this._gs.CsvFolderPath, filename);
                if (File.Exists(realPath)) {
                    File.Delete(realPath);
                }

                File.Move(tempPath, $"{realPath}.tmp", true);
                File.Move($"{realPath}.tmp", realPath, true);

                result = realPath;
            }
            catch (Exception ex) {
                this._logger.LogError("[BULK_GET_CSV] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        /** Wajib di Override */

        protected abstract void BindQueryParameter(DbCommand databaseCommand, List<CDbQueryParamBind> parameters);

        public abstract Task<DataColumnCollection> GetAllColumnTableAsync(string tableName, int commandTimeoutSeconds = 3600);
        public abstract Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<List<T>> GetListAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, CancellationToken token = default, Action<T> callback = null, int commandTimeoutSeconds = 3600);
        public abstract Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<int> ExecQueryWithResultAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<bool> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null, CommandBehavior commandBehavior = CommandBehavior.Default, int commandTimeoutSeconds = 3600, CancellationToken token = default);
        public abstract Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default);

    }

}
