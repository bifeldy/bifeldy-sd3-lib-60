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
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Abstractions {

    public interface IRepository {
        Task<string> GetJenisDc(bool isPg, IDatabase db, string kodeDc);
        Task<EJenisDc> GetJenisDc(bool isPg, IDatabase db);
        Task<string> GetKodeDc(bool isPg, IDatabase db);
        Task<string> GetNamaDc(bool isPg, IDatabase db);
        Task<bool> IsDcHo(bool isPg, IDatabase db);
        Task<bool> IsWhHo(bool isPg, IDatabase db);
        Task<bool> IsHo(bool isPg, IDatabase db);
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
                res = await _db.SingleOrDefaultAsync();
            }
            else {
                IQueryable<DC_TABEL_DC_T> sql = _db.Where(d => d.TBL_DC_KODE.ToUpper() == kodeDc.ToUpper());
                res = await sql.SingleOrDefaultAsync();
            }

            return res.TBL_JENIS_DC.ToUpper();
        }

        public async Task<EJenisDc> GetJenisDc(bool isPg, IDatabase db) {
            if (this.JenisDc == 0) {
                // Sementara (& Selamanya) Di Hard-Coded ~

                string _dbUser = db.DbUsername.ToUpper();
                if (_dbUser.StartsWith("PGBOUNCER_")) {
                    _dbUser = _dbUser[10..];
                }

                if (_dbUser.Contains("DCHO") || _dbUser.Contains("WHHO")) {
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

            return this.JenisDc;
        }

        public async Task<string> GetKodeDc(bool isPg, IDatabase db) {
            if (string.IsNullOrEmpty(this.KodeDc)) {
                // Sementara (& Selamanya) Di Hard-Coded ~

                string _dbUser = db.DbUsername.ToUpper();
                if (_dbUser.StartsWith("PGBOUNCER_")) {
                    _dbUser = _dbUser[10..];
                }

                if (_dbUser.Contains("DCHO")) {
                    this.KodeDc = "DCHO";
                }
                else if (_dbUser.Contains("WHHO")) {
                    this.KodeDc = "WHHO";
                }
                else {
                    this.KodeDc = (await db.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_DC_KODE?.ToUpper();
                }
            }

            return this.KodeDc;
        }

        public async Task<string> GetNamaDc(bool isPg, IDatabase db) {
            if (string.IsNullOrEmpty(this.NamaDc)) {
                // Sementara (& Selamanya) Di Hard-Coded ~

                string _dbUser = db.DbUsername.ToUpper();
                if (_dbUser.StartsWith("PGBOUNCER_")) {
                    _dbUser = _dbUser[10..];
                }

                if (_dbUser.Contains("DCHO")) {
                    this.NamaDc = "DC HEAD OFFICE";
                }
                else if (_dbUser.Contains("WHHO")) {
                    this.NamaDc = "WH HEAD OFFICE";
                }
                else {
                    this.NamaDc = (await db.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_DC_NAMA.ToUpper();
                }
            }

            return this.NamaDc;
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
            bool isDcHo = await this.IsDcHo(isPg, db);
            bool isWhHo = await this.IsWhHo(isPg, db);
            return isDcHo || isWhHo;
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
