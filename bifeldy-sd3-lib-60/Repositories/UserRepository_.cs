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
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IUserRepository {
        Task<bool> Create(DC_USER_T user);
        Task<List<DC_USER_T>> GetAll(string userNameNik = null);
        Task<DC_USER_T> GetByUserNik(string userNik);
        Task<DC_USER_T> GetByUserName(string userName);
        Task<DC_USER_T> GetByUserNameNik(string userNameNik);
        Task<DC_USER_T> GetByUserNameNikPassword(string userNameNik, string password);
        Task<bool> Delete(string userNik);
        Task<string> LoginUser(string userNameNik, string password);
        Task LogoutUser();
    }

    [TransientServiceRegistration]
    public sealed class CUserRepository : CRepository, IUserRepository {

        private readonly AuthenticationStateProvider _asp;

        private readonly IOraPg _orapg;

        public CUserRepository(
            AuthenticationStateProvider asp,
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            this._asp = asp;
            this._orapg = orapg;
        }

        public async Task<bool> Create(DC_USER_T user) {
            _ = this._orapg.Set<DC_USER_T>().Add(user);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_USER_T>> GetAll(string userNameNik = null) {
            DbSet<DC_USER_T> dbSet = this._orapg.Set<DC_USER_T>();
            IQueryable<DC_USER_T> query = null;
            if (!string.IsNullOrEmpty(userNameNik)) {
                query = dbSet.Where(u => u.USER_NAME.ToUpper() == userNameNik.ToUpper() || u.USER_NIK.ToUpper() == userNameNik.ToUpper());
            }

            return await (query ?? dbSet).ToListAsync();
        }

        public async Task<DC_USER_T> GetByUserNik(string userNik) {
            return await this._orapg.Set<DC_USER_T>()
                .Where(u => u.USER_NIK.ToUpper() == userNik.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<DC_USER_T> GetByUserName(string userName) {
            return await this._orapg.Set<DC_USER_T>()
                .Where(u => u.USER_NAME.ToUpper() == userName.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<DC_USER_T> GetByUserNameNik(string userNameNik) {
            return await this._orapg.Set<DC_USER_T>()
                .Where(u => (
                        u.USER_NAME.ToUpper() == userNameNik.ToUpper() ||
                        u.USER_NIK.ToUpper() == userNameNik.ToUpper()
                    )
                    && u.USER_NAME.ToUpper() != null && u.USER_NIK.ToUpper() != null
                )
                .SingleOrDefaultAsync();
        }

        public async Task<DC_USER_T> GetByUserNameNikPassword(string userNameNik, string password) {
            return await this._orapg.Set<DC_USER_T>()
                .Where(u => (
                        u.USER_NAME.ToUpper() == userNameNik.ToUpper() ||
                        u.USER_NIK.ToUpper() == userNameNik.ToUpper()
                    ) && u.USER_PASSWORD.ToUpper() == password.ToUpper()
                    && u.USER_NAME.ToUpper() != null && u.USER_NIK.ToUpper() != null
                )
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string userNik) {
            DC_USER_T user = await this.GetByUserNik(userNik);
            _ = this._orapg.Set<DC_USER_T>().Remove(user);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<string> LoginUser(string userNameNik, string password) {
            DC_USER_T dcUserT = await this.GetByUserNameNikPassword(userNameNik, password);
            if (dcUserT != null) {
                var casp = (BlazorAuthenticationStateProvider) this._asp;
                await casp.UpdateAuthenticationState(new UserWebSession() {
                    name = dcUserT.USER_NAME,
                    nik = dcUserT.USER_NIK,
                    role = UserSessionRole.USER_SD_SSD_3,
                    dc_user_t = dcUserT
                });
                return null;
            }

            return "Username / Password Salah";
        }

        public async Task LogoutUser() {
            var casp = (BlazorAuthenticationStateProvider) this._asp;
            await casp.UpdateAuthenticationState(null);
        }

    }

}
