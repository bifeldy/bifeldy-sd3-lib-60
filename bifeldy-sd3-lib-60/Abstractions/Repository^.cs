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
        Task<string> CekVersi();
        Task<bool> LoginUser(string userNameNik, string password);
    }

    public abstract class CRepository : IRepository {

        private readonly EnvVar _envVar;

        private readonly IApplicationService _as;

        private readonly IOraPg _orapg;

        public CRepository(IOptions<EnvVar> envVar, IApplicationService @as, IOraPg orapg) {
            _envVar = envVar.Value;
            _as = @as;
            _orapg = orapg;
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

        public async Task<string> CekVersi() {
            if (_as.DebugMode) {
                return "OKE";
            }
            else {
                try {
                    string res1 = await _orapg.ExecScalarAsync<string>(
                        $@"
                            SELECT
                                CASE
                                    WHEN COALESCE(aprove, 'N') = 'Y' AND {(
                                            _envVar.IS_USING_POSTGRES ?
                                            "COALESCE(tgl_berlaku, NOW())::DATE <= CURRENT_DATE" :
                                            "TRUNC(COALESCE(tgl_berlaku, SYSDATE)) <= TRUNC(SYSDATE)"
                                        )} 
                                        THEN COALESCE(VERSI_BARU, '0')
                                    WHEN COALESCE(aprove, 'N') = 'N'
                                        THEN COALESCE(versi_lama, '0')
                                    ELSE
                                        COALESCE(versi_lama, '0')
                                END AS VERSI
                            FROM
                                dc_program_vbdtl_t
                            WHERE
                                UPPER(dc_kode) = :dc_kode
                                AND UPPER(nama_prog) LIKE :nama_prog
                        ",
                        new List<CDbQueryParamBind> {
                            new CDbQueryParamBind { NAME = "dc_kode", VALUE = await GetKodeDc() },
                            new CDbQueryParamBind { NAME = "nama_prog", VALUE = $"%{_as.AppName}%" }
                        }
                    );
                    if (string.IsNullOrEmpty(res1)) {
                        return $"Program :: {_as.AppName}" + Environment.NewLine + "Belum Terdaftar Di Master Program DC";
                    }
                    if (res1 == _as.AppVersion) {
                        return "OKE";
                    }
                    else {
                        return $"Versi Program :: {_as.AppName}" + Environment.NewLine + $"Tidak Sama Dengan Master Program = v{res1}";
                    }
                }
                catch (Exception ex1) {
                    return ex1.Message;
                }
            }
        }

        public async Task<bool> LoginUser(string userNameNik, string password) {
            string loggedInUsername = await _orapg.ExecScalarAsync<string>(
                $@"
                    SELECT
                        user_name
                    FROM
                        dc_user_t
                    WHERE
                        (UPPER(user_name) = :user_name OR UPPER(user_nik) = :user_nik)
                        AND UPPER(user_password) = :password
                ",
                new List<CDbQueryParamBind> {
                    new CDbQueryParamBind { NAME = "user_name", VALUE = userNameNik },
                    new CDbQueryParamBind { NAME = "user_nik", VALUE = userNameNik },
                    new CDbQueryParamBind { NAME = "password", VALUE = password }
                }
            );
            return !string.IsNullOrEmpty(loggedInUsername);
        }

    }

}
