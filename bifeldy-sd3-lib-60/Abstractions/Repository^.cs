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

        public CRepository(IOptions<EnvVar> envVar, IApplicationService @as, IOraPg orapg, IMsSQL mssql) {
            _envVar = envVar.Value;
            _as = @as;
            _orapg = orapg;
            _mssql = mssql;
        }

        public async Task<string> GetJenisDc() {
            if (_orapg.DbUsername.ToUpper().Contains("DCHO")) {
                return "HO";
            }
            else {
                return (await _orapg.Set<DC_TABEL_DC_T>().FirstOrDefaultAsync()).TBL_JENIS_DC.ToUpper();
            }
        }

        public async Task<string> GetKodeDc() {
            if (_orapg.DbUsername.ToUpper().Contains("DCHO")) {
                return "DCHO";
            }
            else {
                return (await _orapg.Set<DC_TABEL_DC_T>().FirstOrDefaultAsync()).TBL_DC_KODE.ToUpper();
            }
        }

        public async Task<string> GetNamaDc() {
            if (_orapg.DbUsername.ToUpper().Contains("DCHO")) {
                return "DC HEAD OFFICE";
            }
            else {
                return (await _orapg.Set<DC_TABEL_DC_T>().FirstOrDefaultAsync()).TBL_DC_NAMA.ToUpper();
            }
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
