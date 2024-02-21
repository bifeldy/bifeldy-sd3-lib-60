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
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IApiTokenRepository {
        Task<bool> Create(DC_API_TOKEN_T apiToken);
        Task<List<DC_API_TOKEN_T>> GetAll(string userName = null);
        Task<DC_API_TOKEN_T> GetByUserName(string userName);
        Task<DC_API_TOKEN_T> GetByUserNamePass(string userName, string password);
        Task<bool> Delete(string userName);
        Task<bool> CheckTokenSekaliPakaiIsValid(DC_API_TOKEN_T apiToken, string tokenSekaliPakai);
        Task<DC_API_TOKEN_T> LoginBot(string userName, string password);
    }

    public sealed class CApiTokenRepository : CRepository, IApiTokenRepository {

        private readonly IApplicationService _as;
        private readonly IOraPg _orapg;

        public CApiTokenRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            _as = @as;
            _orapg = orapg;
        }

        public async Task<bool> Create(DC_API_TOKEN_T apiToken) {
            apiToken.APP_NAME = _as.AppName.ToUpper();
            _orapg.Set<DC_API_TOKEN_T>().Add(apiToken);
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_API_TOKEN_T>> GetAll(string userName = null) {
            DbSet<DC_API_TOKEN_T> dbSet = _orapg.Set<DC_API_TOKEN_T>();
            IQueryable<DC_API_TOKEN_T> query = dbSet.Where(at =>
                at.APP_NAME.ToUpper() == _as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*"
            );
            if (!string.IsNullOrEmpty(userName)) {
                query = dbSet.Where(at => at.USER_NAME.ToUpper() == userName.ToUpper());
            }
            return await ((query == null) ? dbSet : query).ToListAsync();
        }

        public async Task<DC_API_TOKEN_T> GetByUserName(string userName) {
            return await _orapg.Set<DC_API_TOKEN_T>().Where(at =>
                at.USER_NAME.ToUpper() == userName.ToUpper() && (
                    at.APP_NAME.ToUpper() == _as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*"
                )
            ).SingleOrDefaultAsync();
        }

        public async Task<DC_API_TOKEN_T> GetByUserNamePass(string userName, string password) {
            return await _orapg.Set<DC_API_TOKEN_T>()
                .Where(at =>
                    at.USER_NAME.ToUpper() == userName.ToUpper() &&
                    at.PASSWORD.ToUpper() == password.ToUpper() && (
                        at.APP_NAME.ToUpper() == _as.AppName.ToUpper() || at.APP_NAME.ToUpper() == "*"
                    )
                )
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string userName) {
            DC_API_TOKEN_T auth = await GetByUserName(userName);
            _orapg.Set<DC_API_TOKEN_T>().Remove(auth);
            return await _orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<bool> CheckTokenSekaliPakaiIsValid(DC_API_TOKEN_T apiToken, string tokenSekaliPakai) {
            bool tokenSekaliPakaiValid = apiToken?.TOKEN_SEKALI_PAKAI.ToUpper() == tokenSekaliPakai.ToUpper();
            if (tokenSekaliPakaiValid) {
                apiToken.TOKEN_SEKALI_PAKAI = null;
                _orapg.Set<DC_API_TOKEN_T>().Update(apiToken);
                await _orapg.SaveChangesAsync();
            }
            return tokenSekaliPakaiValid;
        }

        public async Task<DC_API_TOKEN_T> LoginBot(string userName, string password) {
            DC_API_TOKEN_T apiToken = await GetByUserNamePass(userName, password);
            if (apiToken != null) {
                apiToken.TOKEN_SEKALI_PAKAI = null;
                apiToken.LAST_LOGIN = DateTime.Now;
                _orapg.Set<DC_API_TOKEN_T>().Update(apiToken);
                if (await _orapg.SaveChangesAsync() > 0) {
                    return await GetByUserNamePass(userName, password);
                }
            }
            return null;
        }

    }

}
