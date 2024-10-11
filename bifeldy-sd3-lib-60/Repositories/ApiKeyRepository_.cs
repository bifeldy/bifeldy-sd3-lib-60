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
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IApiKeyRepository {
        Task<bool> Create(API_KEY_T apiKey);
        Task<List<API_KEY_T>> GetAll(string key = null);
        Task<API_KEY_T> GetByKey(string key);
        Task<bool> Delete(string key);
        Task<API_KEY_T> SecretLogin(string key);
        Task<bool> CheckKeyOrigin(string ipOrigin, string key);
    }

    public sealed class CApiKeyRepository : CRepository, IApiKeyRepository {

        private readonly IApplicationService _as;
        private readonly IGlobalService _gs;

        private readonly IOraPg _orapg;

        public CApiKeyRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IGlobalService gs,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            this._as = @as;
            this._gs = gs;
            this._orapg = orapg;
        }

        public async Task<bool> Create(API_KEY_T apiKey) {
            apiKey.APP_NAME = this._as.AppName.ToUpper();
            _ = this._orapg.Set<API_KEY_T>().Add(apiKey);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<API_KEY_T>> GetAll(string key = null) {
            DbSet<API_KEY_T> dbSet = this._orapg.Set<API_KEY_T>();
            IQueryable<API_KEY_T> query = dbSet.Where(ak => ak.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || ak.APP_NAME.ToUpper() == "*");
            if (!string.IsNullOrEmpty(key)) {
                query = dbSet.Where(ak => ak.KEY.ToUpper() == key.ToUpper());
            }

            return await (query ?? dbSet).ToListAsync();
        }

        public async Task<API_KEY_T> GetByKey(string key) {
            return await this._orapg.Set<API_KEY_T>().Where(ak =>
                ak.KEY.ToUpper() == key.ToUpper() && (
                    ak.APP_NAME.ToUpper() == this._as.AppName.ToUpper() || ak.APP_NAME.ToUpper() == "*"
                )
            ).SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string key) {
            API_KEY_T apiKey = await this.GetByKey(key);
            _ = this._orapg.Set<API_KEY_T>().Remove(apiKey);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<API_KEY_T> SecretLogin(string key) {
            return await this._orapg.Set<API_KEY_T>().Where(ak =>
                ak.KEY.ToUpper() == key.ToUpper() &&
                ak.IP_ORIGIN == "*" &&
                ak.APP_NAME == "*"
            ).SingleOrDefaultAsync();
        }

        public async Task<bool> CheckKeyOrigin(string ipOrigin, string key) {
            API_KEY_T ak = await this.GetByKey(key);
            return ak != null
                ? ak.IP_ORIGIN.ToUpper().Split(";").Select(io => io.Trim()).Contains(ipOrigin.ToUpper()) || ak.IP_ORIGIN == "*"
                : this._gs.AllowedIpOrigin.Contains(ipOrigin);
        }

    }

}
