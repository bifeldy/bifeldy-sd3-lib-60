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
        Task<EJenisDc> GetJenisDc();
        Task<string> GetKodeDc();
        Task<string> GetNamaDc();
        Task<bool> IsDcHo();
        Task<bool> IsWhHo();
        Task<bool> IsHo();
        Task<DateTime> OraPg_DateYesterdayOrTommorow(int lastDay);
        Task<DateTime> OraPg_GetLastOrNextMonth(int lastMonth);
        Task<DateTime> OraPg_GetCurrentTimestamp();
        Task<DateTime> OraPg_GetCurrentDate();
    }

    public abstract class CRepository : IRepository {

        private readonly EnvVar _envVar;

        private readonly IApplicationService _as;

        private readonly IOraPg _orapg;
        private readonly IMsSQL _mssql;

        private EJenisDc JenisDc = 0;
        private string KodeDc = null;
        private string NamaDc = null;

        public CRepository(IOptions<EnvVar> envVar, IApplicationService @as, IOraPg orapg, IMsSQL mssql) {
            this._envVar = envVar.Value;
            this._as = @as;
            this._orapg = orapg;
            this._mssql = mssql;
        }

        public async Task<EJenisDc> GetJenisDc() {
            if (this.JenisDc == 0) {
                if (this._orapg.DbUsername.ToUpper().Contains("DCHO") || this._orapg.DbUsername.ToUpper().Contains("WHHO")) {
                    this.JenisDc = EJenisDc.HO;
                }
                else {
                    string jenisDc = (await this._orapg.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_JENIS_DC.ToUpper();

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

        public async Task<string> GetKodeDc() {
            if (string.IsNullOrEmpty(this.KodeDc)) {
                if (this._orapg.DbUsername.ToUpper().Contains("DCHO")) {
                    this.KodeDc = "DCHO";
                }
                else if (this._orapg.DbUsername.ToUpper().Contains("WHHO")) {
                    this.KodeDc = "WHHO";
                }
                else {
                    this.KodeDc = (await this._orapg.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_DC_KODE?.ToUpper();
                }
            }

            return this.KodeDc;
        }

        public async Task<string> GetNamaDc() {
            if (string.IsNullOrEmpty(this.NamaDc)) {
                if (this._orapg.DbUsername.ToUpper().Contains("DCHO")) {
                    this.NamaDc = "DC HEAD OFFICE";
                }
                else if (this._orapg.DbUsername.ToUpper().Contains("WHHO")) {
                    this.NamaDc = "WH HEAD OFFICE";
                }
                else {
                    this.NamaDc = (await this._orapg.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_DC_NAMA.ToUpper();
                }
            }

            return this.NamaDc;
        }

        public async Task<bool> IsDcHo() {
            string kodeDc = await this.GetKodeDc();
            return kodeDc == "DCHO";
        }

        public async Task<bool> IsWhHo() {
            string kodeDc = await this.GetKodeDc();
            return kodeDc == "WHHO";
        }

        public async Task<bool> IsHo() {
            bool isDcHo = await this.IsDcHo();
            bool isWhHo = await this.IsWhHo();
            return isDcHo || isWhHo;
        }

        public async Task<DateTime> OraPg_DateYesterdayOrTommorow(int lastDay) {
            return await this._orapg.ExecScalarAsync<DateTime>(
                $@"
                    SELECT {(this._envVar.IS_USING_POSTGRES ? "CURRENT_DATE" : "TRUNC(SYSDATE)")} {(lastDay >= 0 ? "+" : "-")} :last_day
                    {(this._envVar.IS_USING_POSTGRES ? "" : "FROM DUAL")}
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "last_day", VALUE = lastDay }
                }
            );
        }

        public async Task<DateTime> OraPg_GetLastOrNextMonth(int lastMonth) {
            return await this._orapg.ExecScalarAsync<DateTime>(
                $@"
                    SELECT TRUNC(add_months({(this._envVar.IS_USING_POSTGRES ? "CURRENT_DATE" : "SYSDATE")}, {(lastMonth >= 0 ? "+" : "-")} :last_month))
                    {(this._envVar.IS_USING_POSTGRES ? "" : "FROM DUAL")}
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "last_month", VALUE = lastMonth }
                }
            );
        }

        public async Task<DateTime> OraPg_GetCurrentTimestamp() {
            return await this._orapg.ExecScalarAsync<DateTime>($@"
                SELECT {(this._envVar.IS_USING_POSTGRES ? "CURRENT_TIMESTAMP" : "SYSDATE FROM DUAL")}
            ");
        }

        public async Task<DateTime> OraPg_GetCurrentDate() {
            return await this._orapg.ExecScalarAsync<DateTime>($@"
                SELECT {(this._envVar.IS_USING_POSTGRES ? "CURRENT_DATE" : "TRUNC(SYSDATE) FROM DUAL")}
            ");
        }

    }

}
