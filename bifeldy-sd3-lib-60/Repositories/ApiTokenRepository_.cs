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
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IApiTokenRepository {
        Task<bool> Create(API_TOKEN_T apiToken);
        Task<List<API_TOKEN_T>> GetAll(string userName = null);
        Task<API_TOKEN_T> GetByUserName(string userName);
        Task<API_TOKEN_T> GetByUserNamePass(string userName, string password);
        Task<bool> Delete(string userName);
        Task<bool> CheckTokenSekaliPakaiIsValid(API_TOKEN_T apiToken, string tokenSekaliPakai);
        Task<API_TOKEN_T> LoginBot(string userName, string password);
    }

    [ScopedServiceRegistration]
    public sealed class CApiTokenRepository : CRepository, IApiTokenRepository {

        private readonly IApplicationService _as;
        private readonly IOraPg _orapg;

        public CApiTokenRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            this._as = @as;
            this._orapg = orapg;
        }

        public async Task<bool> Create(API_TOKEN_T apiToken) {
            apiToken.APP_NAME = this._as.AppName.ToUpper();
            _ = this._orapg.Set<API_TOKEN_T>().Add(apiToken);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<API_TOKEN_T>> GetAll(string userName = null) {
            DbSet<API_TOKEN_T> dbSet = this._orapg.Set<API_TOKEN_T>();
            IQueryable<API_TOKEN_T> query = dbSet.Where(at => at.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*");
            if (!string.IsNullOrEmpty(userName)) {
                query = dbSet.Where(at => at.USER_NAME.ToUpper() == userName.ToUpper());
            }

            return await (query ?? dbSet).ToListAsync();
        }

        public async Task<API_TOKEN_T> GetByUserName(string userName) {
            return await this._orapg.Set<API_TOKEN_T>().Where(at =>
                at.USER_NAME.ToUpper() == userName.ToUpper() && (
                    at.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*"
                )
            ).SingleOrDefaultAsync();
        }

        public async Task<API_TOKEN_T> GetByUserNamePass(string userName, string password) {
            return await this._orapg.Set<API_TOKEN_T>()
                .Where(at =>
                    at.USER_NAME.ToUpper() == userName.ToUpper() &&
                    at.PASSWORD.ToUpper() == password.ToUpper() && (
                        at.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*"
                    )
                )
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string userName) {
            API_TOKEN_T auth = await this.GetByUserName(userName);
            _ = this._orapg.Set<API_TOKEN_T>().Remove(auth);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<bool> CheckTokenSekaliPakaiIsValid(API_TOKEN_T apiToken, string tokenSekaliPakai) {
            bool tokenSekaliPakaiValid = apiToken?.TOKEN_SEKALI_PAKAI.ToUpper() == tokenSekaliPakai.ToUpper();
            if (tokenSekaliPakaiValid) {
                apiToken.TOKEN_SEKALI_PAKAI = null;
                _ = this._orapg.Set<API_TOKEN_T>().Update(apiToken);
                _ = await this._orapg.SaveChangesAsync();
            }

            return tokenSekaliPakaiValid;
        }

        public async Task<API_TOKEN_T> LoginBot(string userName, string password) {
            API_TOKEN_T apiToken = await this.GetByUserNamePass(userName, password);
            if (apiToken != null) {
                apiToken.TOKEN_SEKALI_PAKAI = null;
                apiToken.LAST_LOGIN = DateTime.Now;
                _ = this._orapg.Set<API_TOKEN_T>().Update(apiToken);
                if (await this._orapg.SaveChangesAsync() > 0) {
                    return await this.GetByUserNamePass(userName, password);
                }
            }

            return null;
        }

    }

}
