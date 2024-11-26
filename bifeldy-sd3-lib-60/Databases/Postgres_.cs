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

namespace bifeldy_sd3_lib_60.Databases {

    public interface IPostgres : IOraPg {
        CPostgres NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbName);
        CPostgres CloneConnection();
    }

    public sealed class CPostgres : CDatabase, IPostgres {

        private readonly ILogger<CPostgres> _logger;
        private readonly EnvVar _envVar;
        private readonly IApplicationService _as;
        private readonly ICsvService _csv;

        public CPostgres(
            DbContextOptions<CPostgres> options,
            ILogger<CPostgres> logger,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IConverterService cs,
            ICsvService csv
        ) : base(options, envVar, logger, cs, csv) {
            this._logger = logger;
            this._envVar = envVar.Value;
            this._as = @as;
            this._csv = csv;
            // --
            this.InitializeConnection();
            // --
            this.Database.SetCommandTimeout(1800); // 30 Minute
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) {
            _ = options.UseNpgsql(this.DbConnectionString)
                .LogTo(s => Console.WriteLine(s))
                .EnableDetailedErrors(this._as.DebugMode)
                .EnableSensitiveDataLogging(this._as.DebugMode);
        }

        public void InitializeConnection(string dbIpAddrss = null, string dbPort = null, string dbUsername = null, string dbPassword = null, string dbName = null) {
            this.DbIpAddrss = dbIpAddrss ?? this._as.GetVariabel("IPPostgres", this._envVar.KUNCI_GXXX);
            this.DbPort = dbPort ?? this._as.GetVariabel("PortPostgres", this._envVar.KUNCI_GXXX);
            this.DbUsername = dbUsername ?? this._as.GetVariabel("UserPostgres", this._envVar.KUNCI_GXXX);
            this.DbPassword = dbPassword ?? this._as.GetVariabel("PasswordPostgres", this._envVar.KUNCI_GXXX);
            this.DbName = dbName ?? this._as.GetVariabel("DatabasePostgres", this._envVar.KUNCI_GXXX);
            this.DbConnectionString = $"Host={this.DbIpAddrss};Port={this.DbPort};Username={this.DbUsername};Password={this.DbPassword};Database={this.DbName};Timeout=180;ApplicationName={this._as.AppName}_{this._as.AppVersion};"; // 3 menit
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

        public override async Task<DataColumnCollection> GetAllColumnTableAsync(string tableName) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
            cmd.CommandText = $@"SELECT * FROM {tableName} LIMIT 1";
            cmd.CommandType = CommandType.Text;
            return await this.GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetDataTableAsync(cmd);
        }

        public override async Task<List<T>> GetListAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.GetListAsync<T>(cmd);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecScalarAsync<T>(cmd);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null, int minRowsAffected = 1, bool shouldEqualMinRowsAffected = false) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecQueryAsync(cmd, minRowsAffected, shouldEqualMinRowsAffected);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
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
            return await this.ExecProcedureAsync(cmd);
        }

        // https://stackoverflow.com/questions/65687071/bulk-insert-copy-ienumerable-into-table-with-npgsql
        public override async Task<bool> BulkInsertInto(string tableName, DataTable dataTable) {
            bool result = false;
            Exception exception = null;
            try {
                if (string.IsNullOrEmpty(tableName)) {
                    throw new Exception("Target Tabel Tidak Ditemukan");
                }

                int colCount = dataTable.Columns.Count;

                var types = new NpgsqlDbType[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                var cmd = (NpgsqlCommand) this.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {tableName} WHERE 1 = 0";
                using (var rdr = (NpgsqlDataReader) await this.ExecReaderAsync(cmd)) {
                    if (rdr.FieldCount != colCount) {
                        throw new Exception($"Jumlah Kolom Tabel Tujuan {tableName} Tidak Sama Dengan Input DataTable {dataTable.TableName}");
                    }

                    ReadOnlyCollection<NpgsqlDbColumn> columns = rdr.GetColumnSchema();
                    for (int i = 0; i < colCount; i++) {
                        types[i] = (NpgsqlDbType) columns[i].NpgsqlDbType;
                        lengths[i] = columns[i].ColumnSize == null ? 0 : (int) columns[i].ColumnSize;
                        fieldNames[i] = columns[i].ColumnName;
                    }
                }

                var sB = new StringBuilder(fieldNames[0]);
                for (int p = 1; p < colCount; p++) {
                    _ = sB.Append(", " + fieldNames[p]);
                }

                using (NpgsqlBinaryImporter writer = ((NpgsqlConnection) this.GetConnection()).BeginBinaryImport($"COPY {tableName} ({sB}) FROM STDIN (FORMAT BINARY)")) {
                    for (int j = 0; j < dataTable.Rows.Count; j++) {
                        DataRow dR = dataTable.Rows[j];
                        writer.StartRow();

                        for (int i = 0; i < colCount; i++) {
                            if (dR[fieldNames[i]] == DBNull.Value) {
                                writer.WriteNull();
                            }
                            else {
                                dynamic _obj = dR[fieldNames[i]];
                                switch (types[i]) {
                                    case NpgsqlDbType.Bigint:
                                        writer.Write(Convert.ToInt64(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Integer:
                                        writer.Write(Convert.ToInt32(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Smallint:
                                        writer.Write(Convert.ToInt16(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Money:
                                    case NpgsqlDbType.Numeric:
                                        writer.Write(((decimal) Convert.ToDecimal(_obj)).RemoveTrail(), types[i]);
                                        break;
                                    case NpgsqlDbType.Double:
                                        writer.Write(Convert.ToDouble(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Real:
                                        writer.Write(Convert.ToSingle(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Boolean:
                                        writer.Write(Convert.ToBoolean(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Char:
                                        if (lengths[i] == 1) {
                                            writer.Write(Convert.ToString(_obj).ToCharArray().First(), types[i]);
                                            break;
                                        }

                                        goto case NpgsqlDbType.Varchar;
                                    case NpgsqlDbType.Varchar:
                                    case NpgsqlDbType.Text:
                                        writer.Write(Convert.ToString(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Time:
                                    case NpgsqlDbType.Timestamp:
                                    case NpgsqlDbType.TimestampTz:
                                    case NpgsqlDbType.Date:
                                        writer.Write(Convert.ToDateTime(_obj), types[i]);
                                        break;
                                    case NpgsqlDbType.Bytea:
                                        writer.Write((byte[]) _obj, types[i]);
                                        break;
                                    default:
                                        writer.Write(_obj, types[i]);
                                        break;
                                    // TODO :: Add More Handles While Free Time ~
                                }
                            }
                        }
                    }

                    _ = writer.Complete();
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

        public override async Task<string> BulkGetCsv(string rawQuery, string delimiter, string filename, string outputPath = null) {
            string result = null;
            Exception exception = null;
            try {
                string path = Path.Combine(outputPath ?? this._csv.CsvFolderPath, filename);
                if (File.Exists(path)) {
                    File.Delete(path);
                }

                if (string.IsNullOrEmpty(rawQuery) || string.IsNullOrEmpty(delimiter)) {
                    throw new Exception("Select Raw Query + Delimiter Harus Di Isi");
                }

                string sqlQuery = $"SELECT * FROM ({rawQuery}) alias_{DateTime.Now.Ticks} WHERE 1 = 0";
                sqlQuery = sqlQuery.Replace($"\r\n", " ");
                sqlQuery = Regex.Replace(sqlQuery, @"\s+", " ");
                this._logger.LogInformation("[{name}_BULK_GET_CSV] {sqlQuery}", this.GetType().Name, sqlQuery);
                using (var rdr = (NpgsqlDataReader) await this.ExecReaderAsync(sqlQuery)) {
                    ReadOnlyCollection<NpgsqlDbColumn> columns = rdr.GetColumnSchema();
                    string struktur = columns.Select(c => c.ColumnName).Aggregate((i, j) => $"{i}{delimiter}{j}");
                    using (var streamWriter = new StreamWriter(path, true)) {
                        streamWriter.WriteLine(struktur.ToUpper());
                        streamWriter.Flush();
                    }
                }

                sqlQuery = $"COPY ({rawQuery}) TO STDOUT WITH CSV DELIMITER '{delimiter}'";
                sqlQuery = sqlQuery.Replace($"\r\n", " ");
                sqlQuery = Regex.Replace(sqlQuery, @"\s+", " ");
                this._logger.LogInformation("[{name}_BULK_GET_CSV] {sqlQuery}", this.GetType().Name, sqlQuery);

                using (TextReader reader = ((NpgsqlConnection) this.GetConnection()).BeginTextExport(sqlQuery)) {
                    using (var streamWriter = new StreamWriter(path, true)) {
                        string line = null;
                        do {
                            line = reader.ReadLine()?.Trim();
                            if (!string.IsNullOrEmpty(line)) {
                                streamWriter.WriteLine(line.ToUpper());
                                streamWriter.Flush();
                            }
                        }
                        while (!string.IsNullOrEmpty(line));
                        result = path;
                    }
                }
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
        public override async Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null, CommandBehavior commandBehavior = CommandBehavior.Default) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.ExecReaderAsync(cmd, commandBehavior);
        }

        public override async Task<List<string>> RetrieveBlob(string stringPathDownload, string queryString, List<CDbQueryParamBind> bindParam = null, string stringCustomSingleFileName = null) {
            var cmd = (NpgsqlCommand) this.CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            this.BindQueryParameter(cmd, bindParam);
            return await this.RetrieveBlob(cmd, stringPathDownload, stringCustomSingleFileName);
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
            return postgres;
        }

    }

}
