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
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

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
        Task<IDbContextTransaction> TransactionStart(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        Task TransactionCommit();
        Task TransactionRollback();
        Task<DataColumnCollection> GetAllColumnTableAsync(string tableName);
        Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null);
        Task<bool> BulkInsertInto(string tableName, DataTable dataTable);
        Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<string> RetrieveBlob(string stringPathDownload, string stringFileName, string queryString, List<CDbQueryParamBind> bindParam = null);
    }

    public abstract partial class CDatabase : DbContext, IDatabase, ICloneable {

        private readonly ILogger<CDatabase> _logger;
        private readonly IConverterService _cs;

        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DbIpAddrss { get; set; }
        public string DbPort { get; set; }
        public string DbName { get; set; }
        public string DbTnsOdp { get; set; }
        public string DbConnectionString { get; set; }

        public bool HasUnCommitRollbackSqlQuery => Database.CurrentTransaction != null;

        public CDatabase(DbContextOptions options, ILogger<CDatabase> logger, IConverterService cs) : base(options) {
            _logger = logger;
            _cs = cs;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            Assembly entitiesAssembly = typeof(EntityTable).Assembly;
            modelBuilder.RegisterAllEntities<EntityTable>(entitiesAssembly);
        }

        public object Clone() {
            return MemberwiseClone();
        }

        protected virtual DbConnection GetConnection() {
            return Database.GetDbConnection();
        }

        protected virtual DbCommand CreateCommand() {
            DbCommand cmd = GetConnection().CreateCommand();
            cmd.CommandTimeout = 1800; // 30 Minutes
            return cmd;
        }

        protected async Task OpenConnection() {
            if (Database.CurrentTransaction == null) {
                if (GetConnection().State != ConnectionState.Closed) {
                    throw new Exception("Database Connection Is Already In Use!");
                }
                await Database.OpenConnectionAsync();
            }
        }

        /// <summary> Jangan Lupa Di Commit Atau Rollback Sebelum Menjalankan Ini </summary>
        protected async Task CloseConnection(bool force = false) {
            if (Database.CurrentTransaction == null || force) {
                if (GetConnection().State != ConnectionState.Closed) {
                    await Database.CloseConnectionAsync();
                }
            }
        }

        public async Task<IDbContextTransaction> TransactionStart(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) {
            await OpenConnection();
            return await Database.BeginTransactionAsync(isolationLevel);
        }

        public async Task TransactionCommit() {
            await Database.CurrentTransaction.CommitAsync();
            await CloseConnection(true);
        }

        public async Task TransactionRollback() {
            await Database.CurrentTransaction.RollbackAsync();
            await CloseConnection(true);
        }

        protected void LogQueryParameter(DbCommand databaseCommand, char databaseParameterPrefix) {
            string sqlTextQueryParameters = databaseCommand.CommandText;
            for (int i = 0; i < databaseCommand.Parameters.Count; i++) {
                dynamic pVal = databaseCommand.Parameters[i].Value;
                Type pValType = (pVal == null) ? typeof(string) : pVal.GetType();
                if (pValType == typeof(string) || pValType == typeof(DateTime)) {
                    pVal = $"'{pVal}'";
                }
                Regex regex = new Regex($"{databaseParameterPrefix}{databaseCommand.Parameters[i].ParameterName}");
                sqlTextQueryParameters = regex.Replace(sqlTextQueryParameters, pVal.ToString(), 1);
            }
            sqlTextQueryParameters = sqlTextQueryParameters.Replace($"\r\n", " ");
            sqlTextQueryParameters = Regex.Replace(sqlTextQueryParameters, @"\s+", " ");
            _logger.LogInformation($"[SQL_PARAMETERS] {sqlTextQueryParameters.Trim()}");
        }

        protected virtual async Task<DataColumnCollection> GetAllColumnTableAsync(string tableName, DbCommand databaseCommand) {
            DataTable dt = await GetDataTableAsync(databaseCommand);
            return dt.Columns;
        }

        protected virtual async Task<DataTable> GetDataTableAsync(DbCommand databaseCommand) {
            DataTable result = new DataTable();
            DbDataReader dr = null;
            Exception exception = null;
            try {
                // await OpenConnection();
                // dataAdapter.Fill(result);
                dr = await ExecReaderAsync(databaseCommand);
                result.Load(dr);
            }
            catch (Exception ex) {
                _logger.LogError($"[DATA_TABLE] {ex.Message}");
                exception = ex;
            }
            finally {
                if (dr != null) {
                    dr.Close();
                }
                await CloseConnection();
            }
            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<T> ExecScalarAsync<T>(DbCommand databaseCommand) {
            T result = _cs.GetDefaultValueT<T>();
            Exception exception = null;
            try {
                await OpenConnection();
                object _obj = await databaseCommand.ExecuteScalarAsync();
                if (_obj != null) {
                    result = (T)Convert.ChangeType(_obj, typeof(T));
                }
            }
            catch (Exception ex) {
                _logger.LogError($"[EXEC_SCALAR] {ex.Message}");
                exception = ex;
            }
            finally {
                await CloseConnection();
            }
            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<bool> ExecQueryAsync(DbCommand databaseCommand) {
            bool result = false;
            Exception exception = null;
            try {
                await OpenConnection();
                result = await databaseCommand.ExecuteNonQueryAsync() >= 0;
            }
            catch (Exception ex) {
                _logger.LogError($"[EXEC_QUERY] {ex.Message}");
                exception = ex;
            }
            finally {
                await CloseConnection();
            }
            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<CDbExecProcResult> ExecProcedureAsync(DbCommand databaseCommand) {
            CDbExecProcResult result = new CDbExecProcResult {
                STATUS = false,
                QUERY = databaseCommand.CommandText,
                PARAMETERS = databaseCommand.Parameters
            };
            Exception exception = null;
            try {
                await OpenConnection();
                result.STATUS = await databaseCommand.ExecuteNonQueryAsync() == -1;
                result.PARAMETERS = databaseCommand.Parameters;
            }
            catch (Exception ex) {
                _logger.LogError($"[EXEC_PROCEDURE] {ex.Message}");
                exception = ex;
            }
            finally {
                await CloseConnection();
            }
            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        /// <summary> Saat Setelah Selesai Baca Dan Tidak Digunakan Lagi </summary>
        /// <summary> Bisa Pakai Manual Panggil Fungsi Close / Commit / Rollback Di Atas </summary>
        protected virtual async Task<DbDataReader> ExecReaderAsync(DbCommand databaseCommand) {
            DbDataReader result = null;
            Exception exception = null;
            try {
                await OpenConnection();
                result = await databaseCommand.ExecuteReaderAsync();
            }
            catch (Exception ex) {
                _logger.LogError($"[EXEC_READER] {ex.Message}");
                exception = ex;
            }
            finally {
                // Kalau Koneksinya Di Close Dari Sini Tidak Akan Bisa Pakai Reader Untuk Baca Lagi
                // await CloseConnection();
            }
            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<string> RetrieveBlob(DbCommand databaseCommand, string stringPathDownload, string stringFileName) {
            string result = null;
            Exception exception = null;
            try {
                await OpenConnection();
                string filePathResult = $"{stringPathDownload}/{stringFileName}";
                DbDataReader rdrGetBlob = await databaseCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                if (!rdrGetBlob.HasRows) {
                    throw new Exception("Error file not found");
                }
                while (await rdrGetBlob.ReadAsync()) {
                    FileStream fs = new FileStream(filePathResult, FileMode.OpenOrCreate, FileAccess.Write);
                    BinaryWriter bw = new BinaryWriter(fs);
                    long startIndex = 0;
                    int bufferSize = 8192;
                    byte[] outbyte = new byte[bufferSize - 1];
                    int retval = (int)rdrGetBlob.GetBytes(0, startIndex, outbyte, 0, bufferSize);
                    while (retval != bufferSize) {
                        bw.Write(outbyte);
                        bw.Flush();
                        Array.Clear(outbyte, 0, bufferSize);
                        startIndex += bufferSize;
                        retval = (int)rdrGetBlob.GetBytes(0, startIndex, outbyte, 0, bufferSize);
                    }
                    bw.Write(outbyte, 0, (retval > 0 ? retval : 1) - 1);
                    bw.Flush();
                    bw.Close();
                }
                rdrGetBlob.Close();
                rdrGetBlob = null;
                result = filePathResult;
            }
            catch (Exception ex) {
                _logger.LogError($"[RETRIEVE_BLOB] {ex.Message}");
                exception = ex;
            }
            finally {
                await CloseConnection();
            }
            return (exception == null) ? result : throw exception;
        }

        /** Wajib di Override */

        protected abstract void BindQueryParameter(DbCommand databaseCommand, List<CDbQueryParamBind> parameters);

        public abstract Task<DataColumnCollection> GetAllColumnTableAsync(string tableName);
        public abstract Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<bool> BulkInsertInto(string tableName, DataTable dataTable);
        public abstract Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<string> RetrieveBlob(string stringPathDownload, string stringFileName, string queryString, List<CDbQueryParamBind> bindParam = null);

    }

}
