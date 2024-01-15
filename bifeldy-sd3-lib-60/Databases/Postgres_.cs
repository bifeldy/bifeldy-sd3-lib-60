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

namespace bifeldy_sd3_lib_60.Databases {

    public interface IPostgres : IOraPg {
        void InitializeConnection(string dbIpAddress = null, string dbPort = null, string dbUsername = null, string dbPassword = null, string dbName = null);
        CPostgres NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbName);
    }

    public sealed class CPostgres : CDatabase, IPostgres {

        private readonly ILogger<CPostgres> _logger;
        private readonly EnvVar _envVar;
        private readonly IApplicationService _as;

        public CPostgres(
            DbContextOptions<CPostgres> options,
            ILogger<CPostgres> logger,
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
            options.UseNpgsql(DbConnectionString)
                .LogTo(s => Console.WriteLine(s))
                .EnableDetailedErrors(_as.DebugMode)
                .EnableSensitiveDataLogging(_as.DebugMode);
        }

        public void InitializeConnection(string dbIpAddress = null, string dbPort = null, string dbUsername = null, string dbPassword = null, string dbName = null) {
            DbIpAddrss = dbIpAddress ?? _as.GetVariabel("IPPostgres", _envVar.KUNCI_GXXX);
            DbPort = dbPort ?? _as.GetVariabel("PortPostgres", _envVar.KUNCI_GXXX);
            DbUsername = dbUsername ?? _as.GetVariabel("UserPostgres", _envVar.KUNCI_GXXX);
            DbPassword = dbPassword ?? _as.GetVariabel("PasswordPostgres", _envVar.KUNCI_GXXX);
            DbName = dbName ?? _as.GetVariabel("DatabasePostgres", _envVar.KUNCI_GXXX);
            DbConnectionString = $"Host={DbIpAddrss};Port={DbPort};Username={DbUsername};Password={DbPassword};Database={DbName};";
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
                            cmd.Parameters.Add(new NpgsqlParameter {
                                ParameterName = $"{pName}_{id}",
                                Value = data ?? DBNull.Value
                            });
                            id++;
                        }
                        Regex regex = new Regex($"{prefix}{pName}");
                        cmd.CommandText = regex.Replace(cmd.CommandText, bindStr, 1);
                    }
                    else {
                        NpgsqlParameter param = new NpgsqlParameter {
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
            NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
            cmd.CommandText = $@"SELECT * FROM {tableName} LIMIT 1";
            cmd.CommandType = CommandType.Text;
            return await GetAllColumnTableAsync(tableName, cmd);
        }

        public override async Task<DataTable> GetDataTableAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await GetDataTableAsync(cmd);
        }

        public override async Task<T> ExecScalarAsync<T>(string queryString, List<CDbQueryParamBind> bindParam = null) {
            NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecScalarAsync<T>(cmd);
        }

        public override async Task<bool> ExecQueryAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecQueryAsync(cmd);
        }

        public override async Task<CDbExecProcResult> ExecProcedureAsync(string procedureName, List<CDbQueryParamBind> bindParam = null) {
            NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
            string sqlTextQueryParameters = "(";
            if (bindParam != null) {
                for (int i = 0; i < bindParam.Count; i++) {
                    sqlTextQueryParameters += $":{bindParam[i].NAME}";
                    if (i + 1 < bindParam.Count) sqlTextQueryParameters += ",";
                }
            }
            sqlTextQueryParameters += ")";
            cmd.CommandText = $"CALL {procedureName} {sqlTextQueryParameters}";
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecProcedureAsync(cmd);
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

                NpgsqlDbType[] types = new NpgsqlDbType[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
                cmd.CommandText = $"SELECT * FROM {tableName} LIMIT 1";
                using (NpgsqlDataReader rdr = (NpgsqlDataReader) await cmd.ExecuteReaderAsync()) {
                    if (rdr.FieldCount != colCount) {
                        throw new Exception("Jumlah Kolom Tabel Tidak Sama");
                    }
                    ReadOnlyCollection<NpgsqlDbColumn> columns = rdr.GetColumnSchema();
                    for (int i = 0; i < colCount; i++) {
                        types[i] = (NpgsqlDbType)columns[i].NpgsqlDbType;
                        lengths[i] = columns[i].ColumnSize == null ? 0 : (int)columns[i].ColumnSize;
                        fieldNames[i] = columns[i].ColumnName;
                    }
                }

                StringBuilder sB = new StringBuilder(fieldNames[0]);
                for (int p = 1; p < colCount; p++) {
                    sB.Append(", " + fieldNames[p]);
                }

                using (NpgsqlBinaryImporter writer = ((NpgsqlConnection) GetConnection()).BeginBinaryImport($"COPY {tableName} ({sB}) FROM STDIN (FORMAT BINARY)")) {
                    for (int j = 0; j < dataTable.Rows.Count; j++) {
                        DataRow dR = dataTable.Rows[j];
                        writer.StartRow();

                        for (int i = 0; i < colCount; i++) {
                            if (dR[i] == DBNull.Value) {
                                writer.WriteNull();
                            }
                            else {
                                switch (types[i]) {
                                    case NpgsqlDbType.Bigint:
                                        writer.Write((long) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Bit:
                                        if (lengths[i] > 1) {
                                            writer.Write((byte[]) dR[i], types[i]);
                                        }
                                        else {
                                            writer.Write((byte) dR[i], types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Boolean:
                                        writer.Write((bool) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Bytea:
                                        writer.Write((byte[]) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Char:
                                        if (dR[i] is string) {
                                            writer.Write((string) dR[i], types[i]);
                                        }
                                        else if (dR[i] is Guid) {
                                            string value = dR[i].ToString();
                                            writer.Write(value, types[i]);
                                        }
                                        else if (lengths[i] > 1) {
                                            writer.Write((char[]) dR[i], types[i]);
                                        }
                                        else {
                                            char[] s = dR[i].ToString().ToCharArray();
                                            writer.Write(s[0], types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Time:
                                    case NpgsqlDbType.Timestamp:
                                    case NpgsqlDbType.TimestampTz:
                                    case NpgsqlDbType.Date:
                                        writer.Write((DateTime) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Double:
                                        writer.Write((double) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Integer:
                                        try {
                                            if (dR[i] is int) {
                                                writer.Write((int) dR[i], types[i]);
                                                break;
                                            }
                                            else if (dR[i] is string) {
                                                int swap = Convert.ToInt32(dR[i]);
                                                writer.Write(swap, types[i]);
                                                break;
                                            }
                                        }
                                        catch (Exception ex) {
                                            _logger.LogError($"[PG_DBTYPE_INTEGER] {ex.Message}");
                                            string sh = ex.Message;
                                        }
                                        writer.Write(dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Interval:
                                        writer.Write((TimeSpan) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Numeric:
                                    case NpgsqlDbType.Money:
                                        writer.Write((decimal) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Real:
                                        writer.Write((float) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Smallint:
                                        try {
                                            if (dR[i] is byte) {
                                                short swap = Convert.ToInt16(dR[i]);
                                                writer.Write(swap, types[i]);
                                                break;
                                            }
                                            writer.Write((short)dR[i], types[i]);
                                        }
                                        catch (Exception ex) {
                                            _logger.LogError($"[PG_DBTYPE_SMALLINT] {ex.Message}");
                                            string ms = ex.Message;
                                        }
                                        break;
                                    case NpgsqlDbType.Varchar:
                                    case NpgsqlDbType.Text:
                                        writer.Write((string) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Uuid:
                                        writer.Write((Guid) dR[i], types[i]);
                                        break;
                                    case NpgsqlDbType.Xml:
                                        writer.Write((string) dR[i], types[i]);
                                        break;
                                }
                            }
                        }
                    }

                    writer.Complete();
                }

                result = true;
            }
            catch (Exception ex) {
                _logger.LogError($"[PG_BULK_INSERT] {ex.Message}");
                exception = ex;
            }

            return (exception == null) ? result : throw exception;
        }

        /// <summary> Jangan Lupa Di Close Koneksinya (Wajib) </summary>
        public override async Task<DbDataReader> ExecReaderAsync(string queryString, List<CDbQueryParamBind> bindParam = null) {
            NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await ExecReaderAsync(cmd);
        }

        public override async Task<string> RetrieveBlob(string stringPathDownload, string stringFileName, string queryString, List<CDbQueryParamBind> bindParam = null) {
            NpgsqlCommand cmd = (NpgsqlCommand) CreateCommand();
            cmd.CommandText = queryString;
            cmd.CommandType = CommandType.Text;
            BindQueryParameter(cmd, bindParam);
            return await RetrieveBlob(cmd, stringPathDownload, stringFileName);
        }

        public CPostgres NewExternalConnection(string dbIpAddrss, string dbPort, string dbUsername, string dbPassword, string dbName) {
            CPostgres postgres = (CPostgres) Clone();
            postgres.InitializeConnection(dbIpAddrss, dbPort, dbUsername, dbPassword, dbName);
            return postgres;
        }

    }

}
