﻿/**
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
        Task<IDbContextTransaction> TransactionStart(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        Task TransactionCommit(DbTransaction useTrx = null);
        Task TransactionRollback(DbTransaction useTrx = null);
        Task<DataColumnCollection> GetAllColumnTableAsync(string tableName);
        Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null);
        Task<bool> BulkInsertInto(string tableName, DataTable dataTable);
        Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null);
    }

    public abstract partial class CDatabase : DbContext, IDatabase, ICloneable {

        private readonly EnvVar _envVar;

        private readonly ILogger<CDatabase> _logger;
        private readonly IConverterService _cs;

        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DbIpAddrss { get; set; }
        public string DbPort { get; set; }
        public string DbName { get; set; }
        public string DbTnsOdp { get; set; }
        public string DbConnectionString { get; set; }

        public bool HasUnCommitRollbackSqlQuery => this.Database.CurrentTransaction != null;

        public CDatabase(DbContextOptions options, IOptions<EnvVar> envVar, ILogger<CDatabase> logger, IConverterService cs) : base(options) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._cs = cs;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            var libAsm = Assembly.GetExecutingAssembly();
            var prgAsm = Assembly.GetEntryAssembly();
            modelBuilder.RegisterAllEntities<EntityTable>(libAsm);
            modelBuilder.RegisterAllEntities<EntityTable>(prgAsm);
            // DbSet<T> Case Sensitive ~
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes()) {
                string tblName = entityType.GetTableName();
                if (this._envVar.IS_USING_POSTGRES) {
                    entityType.SetTableName(tblName.ToLower());
                }

                foreach (IMutableProperty property in entityType.GetProperties()) {
                    string colName = property.GetColumnBaseName();
                    if (this._envVar.IS_USING_POSTGRES) {
                        property.SetColumnName(colName.ToLower());
                    }
                }
            }
        }

        public object Clone() => this.MemberwiseClone();

        protected void ReSetConnectionString() => this.Database.SetConnectionString(this.DbConnectionString);

        protected virtual DbConnection GetConnection() => this.Database.GetDbConnection();

        protected virtual DbCommand CreateCommand() {
            DbCommand cmd = this.GetConnection().CreateCommand();
            cmd.CommandTimeout = 1800; // 30 Minutes
            return cmd;
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
        protected async Task CloseConnection(bool force = false) {
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

        public async Task TransactionCommit(DbTransaction useTrx = null) {
            DbTransaction trx = useTrx ?? this.Database.CurrentTransaction.GetDbTransaction();
            IDbContextTransaction ctx = await this.Database.UseTransactionAsync(trx);
            await ctx.CommitAsync();
            await this.CloseConnection(true);
        }

        public async Task TransactionRollback(DbTransaction useTrx = null) {
            DbTransaction trx = useTrx ?? this.Database.CurrentTransaction.GetDbTransaction();
            IDbContextTransaction ctx = await this.Database.UseTransactionAsync(trx);
            await ctx.RollbackAsync();
            await this.CloseConnection(true);
        }

        protected void LogQueryParameter(DbCommand databaseCommand, char databaseParameterPrefix) {
            string sqlTextQueryParameters = databaseCommand.CommandText;
            for (int i = 0; i < databaseCommand.Parameters.Count; i++) {
                dynamic pVal = databaseCommand.Parameters[i].Value;
                Type pValType = (pVal == null) ? typeof(string) : pVal.GetType();
                if (pValType == typeof(string) || pValType == typeof(DateTime)) {
                    pVal = $"'{pVal}'";
                }

                var regex = new Regex($"{databaseParameterPrefix}{databaseCommand.Parameters[i].ParameterName}");
                sqlTextQueryParameters = regex.Replace(sqlTextQueryParameters, pVal.ToString(), 1);
            }

            sqlTextQueryParameters = sqlTextQueryParameters.Replace($"\r\n", " ");
            sqlTextQueryParameters = Regex.Replace(sqlTextQueryParameters, @"\s+", " ");
            this._logger.LogInformation("[SQL_PARAMETERS] {sqlTextQueryParameters}", sqlTextQueryParameters.Trim());
        }

        protected virtual async Task<DataColumnCollection> GetAllColumnTableAsync(string tableName, DbCommand databaseCommand) {
            DataTable dt = await this.GetDataTableAsync(databaseCommand);
            return dt.Columns;
        }

        protected virtual async Task<DataTable> GetDataTableAsync(DbCommand databaseCommand) {
            var result = new DataTable();
            DbDataReader dr = null;
            Exception exception = null;
            try {
                // await OpenConnection();
                // dataAdapter.Fill(result);
                dr = await this.ExecReaderAsync(databaseCommand);
                result.Load(dr);
            }
            catch (Exception ex) {
                this._logger.LogError("[DATA_TABLE] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                dr?.Close();
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        protected virtual async Task<T> ExecScalarAsync<T>(DbCommand databaseCommand) {
            T result = default;
            Exception exception = null;
            try {
                await this.OpenConnection();
                object _obj = await databaseCommand.ExecuteScalarAsync();
                if (_obj != null && _obj != DBNull.Value) {
                    result = (T) Convert.ChangeType(_obj, typeof(T));
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

        protected virtual async Task<bool> ExecQueryAsync(DbCommand databaseCommand) {
            bool result = false;
            Exception exception = null;
            try {
                await this.OpenConnection();
                result = await databaseCommand.ExecuteNonQueryAsync() >= 0;
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

        protected virtual async Task<CDbExecProcResult> ExecProcedureAsync(DbCommand databaseCommand) {
            var result = new CDbExecProcResult {
                STATUS = false,
                QUERY = databaseCommand.CommandText,
                PARAMETERS = databaseCommand.Parameters
            };
            Exception exception = null;
            try {
                await this.OpenConnection();
                result.STATUS = await databaseCommand.ExecuteNonQueryAsync() == -1;
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
        protected virtual async Task<DbDataReader> ExecReaderAsync(DbCommand databaseCommand, CommandBehavior commandBehavior = CommandBehavior.Default) {
            DbDataReader result = null;
            Exception exception = null;
            try {
                await this.OpenConnection();
                result = await databaseCommand.ExecuteReaderAsync(commandBehavior);
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

        protected virtual async Task<List<string>> RetrieveBlob(DbCommand databaseCommand, string stringPathDownload, string stringFileName) {
            var result = new List<string>();
            Exception exception = null;
            try {
                string _oldCmdTxt = databaseCommand.CommandText;
                databaseCommand.CommandText = $"SELECT COUNT(*) FROM ( {_oldCmdTxt} ) RetrieveBlob_{DateTime.Now.Ticks}";
                ulong _totalFiles = await this.ExecScalarAsync<ulong>(databaseCommand);
                if (_totalFiles <= 0) {
                    throw new Exception("File Tidak Ditemukan");
                }

                databaseCommand.CommandText = _oldCmdTxt;
                using (DbDataReader rdrGetBlob = await this.ExecReaderAsync(databaseCommand, CommandBehavior.SequentialAccess)) {
                    if (string.IsNullOrEmpty(stringFileName) && rdrGetBlob.FieldCount != 2) {
                        throw new Exception($"Jika Nama File Kosong Maka Harus Berjumlah 2 Kolom{Environment.NewLine}SELECT kolom_blob_data, kolom_nama_file FROM ...");
                    }
                    else if (!string.IsNullOrEmpty(stringFileName) && rdrGetBlob.FieldCount > 1) {
                        throw new Exception($"Harus Berjumlah 1 Kolom{Environment.NewLine}SELECT kolom_blob_data FROM ...");
                    }

                    int bufferSize = 1024;
                    byte[] outByte = new byte[bufferSize];

                    while (await rdrGetBlob.ReadAsync()) {
                        string filePath = Path.Combine(stringPathDownload, stringFileName);

                        if (rdrGetBlob.FieldCount == 2) {
                            string fileMultipleName = rdrGetBlob.GetString(1);
                            if (string.IsNullOrEmpty(fileMultipleName)) {
                                fileMultipleName = $"{DateTime.Now.Ticks}";
                            }

                            filePath = Path.Combine(stringPathDownload, fileMultipleName);
                        }

                        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write)) {
                            using (var bw = new BinaryWriter(fs)) {
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

                    await rdrGetBlob.CloseAsync();
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

        /** Wajib di Override */

        protected abstract void BindQueryParameter(DbCommand databaseCommand, List<CDbQueryParamBind> parameters);

        public abstract Task<DataColumnCollection> GetAllColumnTableAsync(string tableName);
        public abstract Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<bool> BulkInsertInto(string tableName, DataTable dataTable);
        public abstract Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null);
        public abstract Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null);

    }

}
