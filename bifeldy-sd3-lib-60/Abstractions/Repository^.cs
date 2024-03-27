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
using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Abstractions {

    public interface IRepository {
        Task<string> GetJenisDc();
        Task<string> GetKodeDc();
        Task<string> GetNamaDc();
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

        private string JenisDc = null;
        private string KodeDc = null;
        private string NamaDc = null;

        public CRepository(IOptions<EnvVar> envVar, IApplicationService @as, IOraPg orapg, IMsSQL mssql) {
            _envVar = envVar.Value;
            _as = @as;
            _orapg = orapg;
            _mssql = mssql;
        }

        public async Task<string> GetJenisDc() {
            if (string.IsNullOrEmpty(JenisDc)) {
                if (_orapg.DbUsername.ToUpper().Contains("DCHO")) {
                    JenisDc = "HO";
                }
                else {
                    JenisDc = (await _orapg.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_JENIS_DC.ToUpper();
                }
            }
            return JenisDc;
        }

        public async Task<string> GetKodeDc() {
            if (string.IsNullOrEmpty(KodeDc)) {
                if (_orapg.DbUsername.ToUpper().Contains("DCHO")) {
                    KodeDc = "DCHO";
                }
                else {
                    KodeDc = (await _orapg.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_DC_KODE?.ToUpper();
                }
            }
            return KodeDc;
        }

        public async Task<string> GetNamaDc() {
            if (string.IsNullOrEmpty(NamaDc)) {
                if (_orapg.DbUsername.ToUpper().Contains("DCHO")) {
                    NamaDc = "DC HEAD OFFICE";
                }
                else {
                    NamaDc = (await _orapg.Set<DC_TABEL_DC_T>().SingleOrDefaultAsync()).TBL_DC_NAMA.ToUpper();
                }
            }
            return NamaDc;
        }

        public async Task<DateTime> OraPg_DateYesterdayOrTommorow(int lastDay) {
            return await _orapg.ExecScalarAsync<DateTime>(
                $@"
                    SELECT {(_envVar.IS_USING_POSTGRES ? "CURRENT_DATE" : "TRUNC(SYSDATE)")} {(lastDay >= 0 ? "+" : "-")} :last_day
                    {(_envVar.IS_USING_POSTGRES ? "" : "FROM DUAL")}
                ",
                new List<CDbQueryParamBind> {
                    new CDbQueryParamBind { NAME = "last_day", VALUE = lastDay }
                }
            );
        }

        public async Task<DateTime> OraPg_GetLastOrNextMonth(int lastMonth) {
            return await _orapg.ExecScalarAsync<DateTime>(
                $@"
                    SELECT TRUNC(add_months({(_envVar.IS_USING_POSTGRES ? "CURRENT_DATE" : "SYSDATE")}, {(lastMonth >= 0 ? "+" : "-")} :last_month))
                    {(_envVar.IS_USING_POSTGRES ? "" : "FROM DUAL")}
                ",
                new List<CDbQueryParamBind> {
                    new CDbQueryParamBind { NAME = "last_month", VALUE = lastMonth }
                }
            );
        }

        public async Task<DateTime> OraPg_GetCurrentTimestamp() {
            return await _orapg.ExecScalarAsync<DateTime>($@"
                SELECT {(_envVar.IS_USING_POSTGRES ? "CURRENT_TIMESTAMP" : "SYSDATE FROM DUAL")}
            ");
        }

        public async Task<DateTime> OraPg_GetCurrentDate() {
            return await _orapg.ExecScalarAsync<DateTime>($@"
                SELECT {(_envVar.IS_USING_POSTGRES ? "CURRENT_DATE" : "TRUNC(SYSDATE) FROM DUAL")}
            ");
        }

    }

}
