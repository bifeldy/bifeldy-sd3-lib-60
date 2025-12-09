/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Transaksi Database
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 *              :: Hanya Untuk Inherit
 *              :: Mohon & Harap Tidak Digunakan
 * 
 */

using Microsoft.EntityFrameworkCore;

using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Abstractions {

    public interface IRepository {
        Task<string> GetJenisDc(bool isPg, IDatabase db, string kodeDc);
        Task<EJenisDc> GetJenisDc(bool isPg, IDatabase db);
        Task<string> GetKodeDc(bool isPg, IDatabase db);
        Task<string> GetNamaDc(bool isPg, IDatabase db);
        Task<bool> IsDcHo(bool isPg, IDatabase db);
        Task<bool> IsNonDc(bool isPg, IDatabase db);
        Task<bool> IsWhHo(bool isPg, IDatabase db);
        Task<bool> IsHo(bool isPg, IDatabase db);
        Task<bool> IsDc(bool isPg, IDatabase db);
        Task<DateTime> OraPg_DateYesterdayOrTommorow(bool isPg, IDatabase db, int lastDay);
        Task<DateTime> OraPg_GetLastOrNextMonth(bool isPg, IDatabase db, int lastMonth);
        Task<DateTime> OraPg_GetCurrentTimestamp(bool isPg, IDatabase db);
        Task<DateTime> OraPg_GetCurrentDate(bool isPg, IDatabase db);
    }

    public abstract class CRepository : IRepository {

        private EJenisDc JenisDc = 0;
        private string KodeDc = null;
        private string NamaDc = null;

        public CRepository() {
            //
        }

        public async Task<string> GetJenisDc(bool isPg, IDatabase db, string kodeDc) {
            DC_TABEL_DC_T res = null;

            DbSet<DC_TABEL_DC_T> _db = db.Set<DC_TABEL_DC_T>();
            if (string.IsNullOrEmpty(kodeDc)) {
                res = await _db.AsNoTracking().SingleOrDefaultAsync();
            }
            else {
                IQueryable<DC_TABEL_DC_T> sql = _db.Where(d => d.TBL_DC_KODE.ToUpper() == kodeDc.ToUpper());
                res = await sql.AsNoTracking().SingleOrDefaultAsync();
            }

            return res?.TBL_JENIS_DC?.ToUpper();
        }

        public async Task<EJenisDc> GetJenisDc(bool isPg, IDatabase db) {
            if (this.JenisDc == 0) {
                string _dbConStr = db.DbConnectionString?.ToUpper();
                if (!string.IsNullOrEmpty(_dbConStr)) {
                    // Sementara (& Selamanya) Di Hard-Coded ~

                    if (
                        _dbConStr.Contains("KCBN") || _dbConStr.Contains("PGCBN") ||
                        _dbConStr.Contains("RLTM") || _dbConStr.Contains("REALTIME") || _dbConStr.Contains("TIMESCALE")
                    ) {
                        this.JenisDc = EJenisDc.NONDC;
                    }
                    else if (_dbConStr.Contains("DCHO") || _dbConStr.Contains("WHHO")) {
                        this.JenisDc = EJenisDc.HO;
                    }
                    else {
                        string jenisDc = await this.GetJenisDc(isPg, db, null);

                        if (Enum.TryParse(jenisDc, true, out EJenisDc eJenisDc)) {
                            this.JenisDc = eJenisDc;
                        }
                        else {
                            throw new Exception("Jenis DC Tidak Valid");
                        }
                    }
                }
            }

            return this.JenisDc;
        }

        public async Task<string> GetKodeDc(bool isPg, IDatabase db) {
            if (string.IsNullOrEmpty(this.KodeDc)) {
                string _dbConStr = db.DbConnectionString?.ToUpper();
                if (!string.IsNullOrEmpty(_dbConStr)) {
                    // Sementara (& Selamanya) Di Hard-Coded ~

                    if (_dbConStr.Contains("KCBN") || _dbConStr.Contains("PGCBN")) {
                        this.KodeDc = "KCBN";
                    }
                    else if (_dbConStr.Contains("RLTM") || _dbConStr.Contains("REALTIME") || _dbConStr.Contains("TIMESCALE")) {
                        this.KodeDc = "RLTM";
                    }
                    else if(_dbConStr.Contains("DCHO")) {
                        this.KodeDc = "DCHO";
                    }
                    else if (_dbConStr.Contains("WHHO")) {
                        this.KodeDc = "WHHO";
                    }
                    else {
                        DC_TABEL_DC_T _kodeDc = await db.Set<DC_TABEL_DC_T>().AsNoTracking().SingleOrDefaultAsync();
                        this.KodeDc = _kodeDc?.TBL_DC_KODE?.ToUpper();
                    }
                }
            }

            return this.KodeDc?.ToUpper();
        }

        public async Task<string> GetNamaDc(bool isPg, IDatabase db) {
            if (string.IsNullOrEmpty(this.NamaDc)) {
                string _dbConStr = db.DbConnectionString?.ToUpper();
                if (!string.IsNullOrEmpty(_dbConStr)) {
                    // Sementara (& Selamanya) Di Hard-Coded ~

                    if (_dbConStr.Contains("KCBN") || _dbConStr.Contains("PGCBN")) {
                        this.NamaDc = "KONSOLIDASI CBN";
                    }
                    else if (_dbConStr.Contains("RLTM") || _dbConStr.Contains("REALTIME") || _dbConStr.Contains("TIMESCALE")) {
                        this.NamaDc = "REAL-TIME-SCALE";
                    }
                    else if (_dbConStr.Contains("DCHO")) {
                        this.NamaDc = "DC HEAD OFFICE";
                    }
                    else if (_dbConStr.Contains("WHHO")) {
                        this.NamaDc = "WH HEAD OFFICE";
                    }
                    else {
                        DC_TABEL_DC_T _namaDc = await db.Set<DC_TABEL_DC_T>().AsNoTracking().SingleOrDefaultAsync();
                        this.NamaDc = _namaDc?.TBL_DC_NAMA?.ToUpper();
                    }
                }
            }

            return this.NamaDc?.ToUpper();
        }

        public async Task<bool> IsNonDc(bool isPg, IDatabase db) {
            EJenisDc jenisDc = await this.GetJenisDc(isPg, db);
            return jenisDc == EJenisDc.NONDC;
        }

        public async Task<bool> IsDcHo(bool isPg, IDatabase db) {
            string kodeDc = await this.GetKodeDc(isPg, db);
            return kodeDc == "DCHO";
        }

        public async Task<bool> IsWhHo(bool isPg, IDatabase db) {
            string kodeDc = await this.GetKodeDc(isPg, db);
            return kodeDc == "WHHO";
        }

        public async Task<bool> IsHo(bool isPg, IDatabase db) {
            EJenisDc jenisDc = await this.GetJenisDc(isPg, db);
            return jenisDc == EJenisDc.HO;
        }

        public async Task<bool> IsDc(bool isPg, IDatabase db) {
            bool isNonDc = await this.IsNonDc(isPg, db);
            bool isDcHo = await this.IsDcHo(isPg, db);
            bool isWhHo = await this.IsWhHo(isPg, db);
            bool isHo = await this.IsHo(isPg, db);
            return !isNonDc && !isDcHo && !isWhHo && !isHo;
        }

        public async Task<DateTime> OraPg_DateYesterdayOrTommorow(bool isPg, IDatabase db, int lastDay) {
            return await db.ExecScalarAsync<DateTime>(
                $@"
                    SELECT {(isPg ? "CURRENT_DATE" : "TRUNC(SYSDATE)")} {(lastDay >= 0 ? "+" : "-")} :last_day
                    {(isPg ? "" : "FROM DUAL")}
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "last_day", VALUE = lastDay }
                }
            );
        }

        public async Task<DateTime> OraPg_GetLastOrNextMonth(bool isPg, IDatabase db, int lastMonth) {
            return await db.ExecScalarAsync<DateTime>(
                $@"
                    SELECT TRUNC(add_months({(isPg ? "CURRENT_DATE" : "SYSDATE")}, {(lastMonth >= 0 ? "+" : "-")} :last_month))
                    {(isPg ? "" : "FROM DUAL")}
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "last_month", VALUE = lastMonth }
                }
            );
        }

        public async Task<DateTime> OraPg_GetCurrentTimestamp(bool isPg, IDatabase db) {
            return await db.ExecScalarAsync<DateTime>($@"
                SELECT CURRENT_TIMESTAMP {(isPg ? "" : "FROM DUAL")}
            ");
        }

        public async Task<DateTime> OraPg_GetCurrentDate(bool isPg, IDatabase db) {
            return await db.ExecScalarAsync<DateTime>($@"
                SELECT {(isPg ? "CURRENT_DATE" : "TRUNC(SYSDATE) FROM DUAL")}
            ");
        }

    }

}
