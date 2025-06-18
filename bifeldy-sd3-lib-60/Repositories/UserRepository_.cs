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
 *              :: Hanya Bisa Digunakan Pada Blazor Saja ~
 * 
 */

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.TableView;
using bifeldy_sd3_lib_60.UserAuth;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IUserRepository {
        Task<bool> Create(bool isPg, IDatabase db, DC_USER_T user);
        Task<List<DC_USER_T>> GetAll(bool isPg, IDatabase db, string userNameNik = null);
        Task<DC_USER_T> GetByUserNik(bool isPg, IDatabase db, string userNik);
        Task<DC_USER_T> GetByUserName(bool isPg, IDatabase db, string userName);
        Task<DC_USER_T> GetByUserNameNik(bool isPg, IDatabase db, string userNameNik);
        Task<DC_USER_T> GetByUserNameNikPassword(bool isPg, IDatabase db, string userNameNik, string password);
        Task<bool> Delete(bool isPg, IDatabase db, string userNik);
        Task<string> LoginUser(bool isPg, IDatabase db, string userNameNik, string password);
        Task LogoutUser();
    }

    [ScopedServiceRegistration]
    public sealed class CUserRepository : CRepository, IUserRepository {

        private readonly AuthenticationStateProvider _asp;

        public CUserRepository(AuthenticationStateProvider asp) {
            this._asp = asp;
        }

        public async Task<bool> Create(bool isPg, IDatabase db, DC_USER_T user) {
            _ = db.Set<DC_USER_T>().Add(user);
            return await db.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_USER_T>> GetAll(bool isPg, IDatabase db, string userNameNik = null) {
            DbSet<DC_USER_T> dbSet = db.Set<DC_USER_T>();
            IQueryable<DC_USER_T> query = null;
            if (!string.IsNullOrEmpty(userNameNik)) {
                query = dbSet.Where(u => u.USER_NAME.ToUpper() == userNameNik.ToUpper() || u.USER_NIK.ToUpper() == userNameNik.ToUpper());
            }

            return await (query ?? dbSet).ToListAsync();
        }

        public async Task<DC_USER_T> GetByUserNik(bool isPg, IDatabase db, string userNik) {
            return await db.Set<DC_USER_T>()
                .Where(u => u.USER_NIK.ToUpper() == userNik.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<DC_USER_T> GetByUserName(bool isPg, IDatabase db, string userName) {
            return await db.Set<DC_USER_T>()
                .Where(u => u.USER_NAME.ToUpper() == userName.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<DC_USER_T> GetByUserNameNik(bool isPg, IDatabase db, string userNameNik) {
            return await db.Set<DC_USER_T>()
                .Where(u => (
                        u.USER_NAME.ToUpper() == userNameNik.ToUpper() ||
                        u.USER_NIK.ToUpper() == userNameNik.ToUpper()
                    )
                    && u.USER_NAME.ToUpper() != null && u.USER_NIK.ToUpper() != null
                )
                .SingleOrDefaultAsync();
        }

        public async Task<DC_USER_T> GetByUserNameNikPassword(bool isPg, IDatabase db, string userNameNik, string password) {
            return await db.Set<DC_USER_T>()
                .Where(u => (
                        u.USER_NAME.ToUpper() == userNameNik.ToUpper() ||
                        u.USER_NIK.ToUpper() == userNameNik.ToUpper()
                    ) && u.USER_PASSWORD.ToUpper() == password.ToUpper()
                    && u.USER_NAME.ToUpper() != null && u.USER_NIK.ToUpper() != null
                )
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(bool isPg, IDatabase db, string userNik) {
            DC_USER_T user = await this.GetByUserNik(isPg, db, userNik);
            _ = db.Set<DC_USER_T>().Remove(user);
            return await db.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<string> LoginUser(bool isPg, IDatabase db, string userNameNik, string password) {
            DC_USER_T dcUserT = await this.GetByUserNameNikPassword(isPg, db, userNameNik, password);
            if (dcUserT != null) {
                var basp = (BlazorAuthenticationStateProvider)this._asp;
                await basp.UpdateAuthenticationState(new UserWebSession() {
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
            var _basp = (BlazorAuthenticationStateProvider) this._asp;
            await _basp.UpdateAuthenticationState(null);
        }

    }

}
