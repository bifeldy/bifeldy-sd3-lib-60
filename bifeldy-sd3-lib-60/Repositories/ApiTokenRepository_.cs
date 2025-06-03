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

using Microsoft.EntityFrameworkCore;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IApiTokenRepository {
        Task<bool> Create(bool isPg, IDatabase db, API_TOKEN_T apiToken);
        Task<List<API_TOKEN_T>> GetAll(bool isPg, IDatabase db, string userName = null);
        Task<API_TOKEN_T> GetByUserName(bool isPg, IDatabase db, string userName);
        Task<API_TOKEN_T> GetByUserNamePass(bool isPg, IDatabase db, string userName, string password);
        Task<bool> Delete(bool isPg, IDatabase db, string userName);
        Task<bool> CheckTokenSekaliPakaiIsValid(bool isPg, IDatabase db, API_TOKEN_T apiToken, string tokenSekaliPakai);
        Task<API_TOKEN_T> LoginBot(bool isPg, IDatabase db, string userName, string password);
    }

    [ScopedServiceRegistration]
    public sealed class CApiTokenRepository : CRepository, IApiTokenRepository {

        private readonly IApplicationService _as;

        public CApiTokenRepository(IApplicationService @as) {
            this._as = @as;
        }

        public async Task<bool> Create(bool isPg, IDatabase db, API_TOKEN_T apiToken) {
            apiToken.APP_NAME = this._as.AppName.ToUpper();
            _ = db.Set<API_TOKEN_T>().Add(apiToken);
            return await db.SaveChangesAsync() > 0;
        }

        public async Task<List<API_TOKEN_T>> GetAll(bool isPg, IDatabase db, string userName = null) {
            DbSet<API_TOKEN_T> dbSet = db.Set<API_TOKEN_T>();
            IQueryable<API_TOKEN_T> query = dbSet.Where(at => at.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*");
            if (!string.IsNullOrEmpty(userName)) {
                query = dbSet.Where(at => at.USER_NAME.ToUpper() == userName.ToUpper());
            }

            return await (query ?? dbSet).ToListAsync();
        }

        public async Task<API_TOKEN_T> GetByUserName(bool isPg, IDatabase db, string userName) {
            return await db.Set<API_TOKEN_T>().Where(at =>
                at.USER_NAME.ToUpper() == userName.ToUpper() && (
                    at.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*"
                )
            ).SingleOrDefaultAsync();
        }

        public async Task<API_TOKEN_T> GetByUserNamePass(bool isPg, IDatabase db, string userName, string password) {
            return await db.Set<API_TOKEN_T>()
                .Where(at =>
                    at.USER_NAME.ToUpper() == userName.ToUpper() &&
                    at.PASSWORD.ToUpper() == password.ToUpper() && (
                        at.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*"
                    )
                )
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(bool isPg, IDatabase db, string userName) {
            API_TOKEN_T auth = await this.GetByUserName(isPg, db, userName);
            _ = db.Set<API_TOKEN_T>().Remove(auth);
            return await db.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<bool> CheckTokenSekaliPakaiIsValid(bool isPg, IDatabase db, API_TOKEN_T apiToken, string tokenSekaliPakai) {
            bool tokenSekaliPakaiValid = apiToken?.TOKEN_SEKALI_PAKAI.ToUpper() == tokenSekaliPakai.ToUpper();
            if (tokenSekaliPakaiValid) {
                apiToken.TOKEN_SEKALI_PAKAI = null;
                _ = db.Set<API_TOKEN_T>().Update(apiToken);
                _ = await db.SaveChangesAsync();
            }

            return tokenSekaliPakaiValid;
        }

        public async Task<API_TOKEN_T> LoginBot(bool isPg, IDatabase db, string userName, string password) {
            API_TOKEN_T apiToken = await this.GetByUserNamePass(isPg, db, userName, password);
            if (apiToken != null) {
                apiToken.TOKEN_SEKALI_PAKAI = null;
                apiToken.LAST_LOGIN = DateTime.Now;
                _ = db.Set<API_TOKEN_T>().Update(apiToken);
                if (await db.SaveChangesAsync() > 0) {
                    return await this.GetByUserNamePass(isPg, db, userName, password);
                }
            }

            return null;
        }

    }

}
