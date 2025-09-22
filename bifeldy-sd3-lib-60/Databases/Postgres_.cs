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
 *              :: Instance Postgre
 * 
 */

using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Repositories;

namespace bifeldy_sd3_lib_60.Databases {

    public interface IPostgres : IOraPg {
        CPostgres NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbName);
        CPostgres CloneConnection();
    }

    public sealed class CPostgres : CDatabase, IPostgres {

        private readonly IHttpContextAccessor _hca;
        private readonly IServerConfigRepository _scr;

        public CPostgres(
            DbContextOptions<CPostgres> options,
            ILogger<CPostgres> logger,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IConverterService cs,
            IGlobalService gs,
            IHttpContextAccessor hca,
            IServerConfigRepository scr
        ) : base(options, logger, envVar, @as, cs, gs) {
            this._hca = hca;
            this._scr = scr;
            //
            this.InitializeConnection();
            this.SetCommandTimeout();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) {
            _ = options.UseNpgsql(this.DbConnectionString)
                // .LogTo(s => Console.WriteLine(s))
                .EnableDetailedErrors(this._as.DebugMode)
                .EnableSensitiveDataLogging(this._as.DebugMode);
        }

        public void InitializeConnection(string dbIpAddrss = null, string dbPort = null, string dbUsername = null, string dbPassword = null, string dbName = null) {
            string kunciGxxx = null;

            if (this._hca.HttpContext != null) {
                kunciGxxx = this._hca.HttpContext.Items["kunci_gxxx"]?.ToString();
            }

            kunciGxxx ??= this._scr.CurrentLoadedKodeServerKunciDc();

            this.DbIpAddrss = dbIpAddrss ?? this._as.GetVariabel("IPPostgres", kunciGxxx);
            this.DbPort = dbPort ?? this._as.GetVariabel("PortPostgres", kunciGxxx);
            this.DbUsername = dbUsername ?? this._as.GetVariabel("UserPostgres", kunciGxxx);
            this.DbPassword = dbPassword ?? this._as.GetVariabel("PasswordPostgres", kunciGxxx);
            this.DbName = dbName ?? this._as.GetVariabel("DatabasePostgres", kunciGxxx);

            this.DbConnectionString = $"Host={this.DbIpAddrss};Port={this.DbPort};Username={this.DbUsername};Password={this.DbPassword};Database={this.DbName};Timeout=180;ApplicationName={this._as.AppName}_{this._as.AppVersion};"; // 3 Minutes
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
                            _ = cmd.Parameters.Add(new NpgsqlParameter() {
                                ParameterName = $"{pName}_{id}",
                                Value = data ?? DBNull.Value
                            });
                            id++;
                        }

                        var regex = new Regex($"{prefix}{pName}");
                        cmd.CommandText = regex.Replace(cmd.CommandText, bindStr, 1);
                    }
                    else {
                        var param = new NpgsqlParameter() {
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
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = $@"SELECT * FROM {tableName} LIMIT 1";
            cmd.CommandType = CommandType.Text;
            return await this.GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetDataTableAsync(cmd, token);
        }

        public override async Task<List<T>> GetListAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, CancellationToken token = default, Action<T> callback = null, int commandTimeoutSeconds = 3600) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetListAsync(cmd, token, callback);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecScalarAsync<T>(cmd, token);
        }

        public override async Task<int> ExecQueryWithResultAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryWithResultAsync(cmd, token);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryAsync(cmd, minRowsAffected, shouldEqualMinRowsAffected, token);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            string sqlTextQueryParameters = "(";
            if (bindParam != null) {
                for (int i = 0; i < bindParam.Count; i++) {
                    sqlTextQueryParameters += $":{bindParam[i].NAME}";
                    if (i + 1 < bindParam.Count) {
                        sqlTextQueryParameters += ",";
                    }
                }
            }

            sqlTextQueryParameters += ")";
            cmd.CommandText = $"CALL {procedureName} {sqlTextQueryParameters}";
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecProcedureAsync(cmd, token);
        }

        // https://stackoverflow.com/questions/65687071/bulk-insert-copy-ienumerable-into-table-with-npgsql
        public override async Task<bool> BulkInsertInto(string tableName, DataTable dataTable, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            bool result = false;
            Exception exception = null;
            try {
                if (string.IsNullOrEmpty(tableName)) {
                    throw new Exception("Target Tabel Tidak Ditemukan");
                }

                int colCount = dataTable.Columns.Count;
                var lsCol = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToUpper()).ToList();
                if (colCount != lsCol.Count) {
                    throw new Exception($"Jumlah Kolom Mapping Tabel Aneh Tidak Sesuai");
                }

                var types = new NpgsqlDbType[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
                cmd.CommandText = $"SELECT * FROM {tableName} WHERE 1 = 0";
                using (var rdr = (NpgsqlDataReader) await this.ExecReaderAsync(cmd, CommandBehavior.Default, token)) {
                    ReadOnlyCollection<NpgsqlDbColumn> columns = rdr.GetColumnSchema();

                    for (int i = 0; i < colCount; i++) {
                        NpgsqlDbColumn column = columns.FirstOrDefault(c => c.ColumnName.ToUpper() == lsCol[i]);
                        if (column == null) {
                            throw new Exception($"Kolom {lsCol[i]} Tidak Tersedia Di Tabel Tujuan {tableName}");
                        }

                        types[i] = (NpgsqlDbType) column.NpgsqlDbType;
                        lengths[i] = column.ColumnSize == null ? 0 : (int) column.ColumnSize;
                        fieldNames[i] = column.ColumnName;
                    }
                }

                var sB = new StringBuilder(fieldNames[0]);
                for (int p = 1; p < colCount; p++) {
                    _ = sB.Append(", " + fieldNames[p]);
                }

                using (NpgsqlBinaryImporter writer = await ((NpgsqlConnection) this.GetConnection()).BeginBinaryImportAsync($"COPY {tableName} ({sB}) FROM STDIN (FORMAT BINARY)", token)) {
                    for (int j = 0; j < dataTable.Rows.Count; j++) {
                        DataRow dR = dataTable.Rows[j];
                        await writer.StartRowAsync(token);

                        for (int i = 0; i < colCount; i++) {
                            if (dR[fieldNames[i]] == DBNull.Value) {
                                await writer.WriteNullAsync(token);
                            }
                            else {
                                dynamic _obj = dR[fieldNames[i]];
                                switch (types[i]) {
                                    case NpgsqlDbType.Bigint:
                                        await writer.WriteAsync(Convert.ToInt64(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Integer:
                                        await writer.WriteAsync(Convert.ToInt32(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Smallint:
                                        await writer.WriteAsync(Convert.ToInt16(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Money:
                                    case NpgsqlDbType.Numeric:
                                        await writer.WriteAsync(((decimal) Convert.ToDecimal(_obj)).RemoveTrail(), types[i], token);
                                        break;
                                    case NpgsqlDbType.Double:
                                        await writer.WriteAsync(Convert.ToDouble(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Real:
                                        await writer.WriteAsync(Convert.ToSingle(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Boolean:
                                        await writer.WriteAsync(Convert.ToBoolean(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Char:
                                        if (lengths[i] == 1) {
                                            string str = Convert.ToString(_obj);
                                            if (string.IsNullOrEmpty(str)) {
                                                _obj = string.Empty;
                                            }
                                            else {
                                                char[] chr = str.ToCharArray();
                                                if (chr.Length == lengths[i]) {
                                                    await writer.WriteAsync(chr[lengths[i] - 1], types[i], token);
                                                    break;
                                                }
                                            }
                                        }

                                        goto case NpgsqlDbType.Varchar;
                                    case NpgsqlDbType.Varchar:
                                    case NpgsqlDbType.Text:
                                        await writer.WriteAsync(Convert.ToString(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Time:
                                    case NpgsqlDbType.Timestamp:
                                    case NpgsqlDbType.TimestampTz:
                                    case NpgsqlDbType.Date:
                                        await writer.WriteAsync(Convert.ToDateTime(_obj), types[i], token);
                                        break;
                                    case NpgsqlDbType.Bytea:
                                        await writer.WriteAsync((byte[]) _obj, types[i], token);
                                        break;
                                    default:
                                        await writer.WriteAsync(_obj, types[i], token);
                                        break;

                                    //
                                    // TODO :: Add More Handles While Free Time ~
                                    //
                                }
                            }
                        }
                    }

                    _ = await writer.CompleteAsync(token);
                }

                result = true;
            }
            catch (Exception ex) {
                this._logger.LogError("[PG_BULK_INSERT] {ex}", ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        public override async Task<string> BulkGetCsv(string queryString, string delimiter, string filename, List<CDbQueryParamBind> bindParam = null, string outputFolderPath = null, bool useRawQueryWithoutParam = false, bool includeHeader = true, bool useDoubleQuote = true, bool allUppercase = true, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            string result = null;
            Exception exception = null;
            try {
                encoding ??= Encoding.UTF8;

                if (!useRawQueryWithoutParam) {
                    return await base.BulkGetCsv(queryString, delimiter, filename, bindParam, outputFolderPath, useRawQueryWithoutParam, includeHeader, useDoubleQuote, allUppercase, encoding, commandTimeoutSeconds, token);
                }

                if (bindParam != null) {
                    throw new Exception("Parameter Binding Terdeteksi, Mohon Mematikan Fitur `useRawQueryWithoutParam`");
                }

                string tempPath = Path.Combine(outputFolderPath ?? this._gs.TempFolderPath, filename);
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }

                string sqlQuery = string.Empty;

                if (includeHeader) {
                    sqlQuery = $"SELECT * FROM ({queryString}) alias_{DateTime.Now.Ticks} WHERE 1 = 0";

                    using (var rdr = (NpgsqlDataReader)await this.ExecReaderAsync(sqlQuery, bindParam, CommandBehavior.SequentialAccess, commandTimeoutSeconds, token)) {
                        ReadOnlyCollection<NpgsqlDbColumn> columns = rdr.GetColumnSchema();
                        string header = string.Join(delimiter, columns.Select(c => {
                            string text = c.ColumnName;

                            if (allUppercase) {
                                text = text.ToUpper();
                            }

                            if (useDoubleQuote) {
                                text = $"\"{text.Replace("\"", "\"\"")}\"";
                            }

                            return text;
                        }));

                        using (var writer = new StreamWriter(tempPath, false, encoding)) {
                            await writer.WriteLineAsync(header.AsMemory(), token);
                        }
                    }
                }

                sqlQuery = $"COPY ({queryString}) TO STDOUT WITH CSV DELIMITER '{delimiter}'";
                if (!useDoubleQuote) {
                    sqlQuery += " QUOTE '\x01'";
                }

                using (TextReader reader = await ((NpgsqlConnection)this.GetConnection()).BeginTextExportAsync(sqlQuery, token)) {
                    using (var writer = new StreamWriter(tempPath, true, encoding)) {
                        string line = string.Empty;
                        while ((line = await reader.ReadLineAsync()) != null && !token.IsCancellationRequested) {
                            if (allUppercase) {
                                line = line.ToUpper();
                            }

                            if (!useDoubleQuote) {
                                if (line.Contains("\x01")) {
                                    line = line.Replace("\x01", "");
                                }
                            }

                            await writer.WriteLineAsync(line.AsMemory(), token);
                        }
                    }
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
                this._logger.LogError("[{name}_BULK_GET_CSV] {ex}", this.GetType().Name, ex.Message);
                exception = ex;
            }
            finally {
                await this.CloseConnection();
            }

            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        public override async Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null, CommandBehavior commandBehavior = CommandBehavior.Default, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecReaderAsync(cmd, commandBehavior, token);
        }

        public override async Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null, Encoding encoding = null, int commandTimeoutSeconds = 3600, CancellationToken token = default) {
            var cmd = (NpgsqlCommand) this.CreateCommand(commandTimeoutSeconds);
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.RetrieveBlob(cmd, stringPathDownload, stringCustomSingleFileName, encoding ?? Encoding.UTF8, token);
        }

        public CPostgres NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbName) {
            var postgres = (CPostgres) this.Clone();
            postgres.InitializeConnection(dbIpAddrss, dbPort, dbUsername, dbPassword, dbName);
            postgres.ReSetConnectionString();
            return postgres;
        }

        public CPostgres CloneConnection() {
            var postgres = (CPostgres) this.Clone();
            postgres.InitializeConnection(this.DbIpAddrss, this.DbPort, this.DbUsername, this.DbPassword, this.DbName);
            postgres.ReSetConnectionString();
            postgres.SetCommandTimeout();
            return postgres;
        }

    }

}
