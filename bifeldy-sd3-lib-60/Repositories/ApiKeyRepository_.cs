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

    public interface IApiKeyRepository {
        Task<bool> Create(API_KEY_T apiKey);
        Task<List<API_KEY_T>> GetAll(string key = null);
        Task<API_KEY_T> GetByKey(string key);
        Task<bool> Delete(string key);
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
            _as = @as;
            _gs = gs;
            _orapg = orapg;
        }

        public async Task<bool> Create(API_KEY_T apiKey) {
            apiKey.APP_NAME = _as.AppName.ToUpper();
            _orapg.Set<API_KEY_T>().Add(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<API_KEY_T>> GetAll(string key = null) {
            DbSet<API_KEY_T> dbSet = _orapg.Set<API_KEY_T>();
            IQueryable<API_KEY_T> query = dbSet.Where(ak => ak.APP_NAME.ToUpper() == _as.AppName.ToUpper());
            if (!string.IsNullOrEmpty(key)) {
                query = dbSet.Where(ak => ak.KEY.ToUpper() == key.ToUpper());
            }
            return await ((query == null) ? dbSet : query).ToListAsync();
        }

        public async Task<API_KEY_T> GetByKey(string key) {
            return await _orapg.Set<API_KEY_T>().Where(ak =>
                ak.KEY.ToUpper() == key.ToUpper() && (
                    ak.APP_NAME.ToUpper() == _as.AppName.ToUpper() || ak.APP_NAME.ToUpper() == "*"
                )
            ).SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string key) {
            API_KEY_T apiKey = await GetByKey(key);
            _orapg.Set<API_KEY_T>().Remove(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<bool> CheckKeyOrigin(string ipOrigin, string key) {
            API_KEY_T ak = await GetByKey(key);
            if (ak != null) {
                return ak.IP_ORIGIN.ToUpper().Split(";").Select(io => io.Trim()).Contains(ipOrigin.ToUpper()) || ak.IP_ORIGIN == "*";
            }
            return _gs.AllowedIpOrigin.Contains(ipOrigin);
        }

    }

}
