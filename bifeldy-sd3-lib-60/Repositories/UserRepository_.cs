/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Transaksi Database Untuk API Key
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IUserRepository {
        Task<bool> Create(DC_USER_T user);
        Task<List<DC_USER_T>> GetAll(string userNameNik = null);
        Task<DC_USER_T> GetByUsernameNik(string userNameNik);
        Task<bool> Delete(string userNameNik);
        Task<string> LoginUser(string userNameNik, string password);
        Task LogoutUser();
    }

    public sealed class CUserRepository : CRepository, IUserRepository {

        private readonly AuthenticationStateProvider _asp;
        private readonly IGlobalService _gs;

        private readonly IOraPg _orapg;

        public CUserRepository(
            AuthenticationStateProvider asp,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IGlobalService gs,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            _asp = asp;
            _gs = gs;
            _orapg = orapg;
        }

        public async Task<bool> Create(DC_USER_T user) {
            _orapg.Set<DC_USER_T>().Add(user);
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_USER_T>> GetAll(string userNameNik = null) {
            DbSet<DC_USER_T> dbSet = _orapg.Set<DC_USER_T>();
            IQueryable<DC_USER_T> query = null;
            if (!string.IsNullOrEmpty(userNameNik)) {
                query = dbSet.Where(u => u.USER_NAME.ToUpper() == userNameNik.ToUpper() || u.USER_NIK.ToUpper() == userNameNik.ToUpper());
            }
            return await ((query == null) ? dbSet : query).ToListAsync();
        }

        public async Task<DC_USER_T> GetByUsernameNik(string userNameNik) {
            return await _orapg.Set<DC_USER_T>()
                .Where(u => u.USER_NAME.ToUpper() == userNameNik.ToUpper() || u.USER_NIK.ToUpper() == userNameNik.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string userNameNik) {
            DC_USER_T user = await GetByUsernameNik(userNameNik);
            _orapg.Set<DC_USER_T>().Remove(user);
            return await _orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<string> LoginUser(string userNameNik, string password) {
            var user = await GetByUsernameNik(userNameNik);
            if (user?.USER_PASSWORD.ToUpper() == password.ToUpper()) {
                var casp = (CustomAuthenticationStateProvider) _asp;
                await casp.UpdateAuthenticationState(new UserSession {
                    Name = user.USER_NAME,
                    Nik = user.USER_NIK
                });
                return null;
            }
            return "Username / Password Salah";
        }

        public async Task LogoutUser() {
            var casp = (CustomAuthenticationStateProvider) _asp;
            await casp.UpdateAuthenticationState(null);
        }

    }

}
