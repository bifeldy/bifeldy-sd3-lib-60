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
        Task<bool> Create(DC_APIKEY_T apiKey);
        Task<List<DC_APIKEY_T>> GetAll(string key = null);
        Task<DC_APIKEY_T> GetById(string key);
        Task<bool> Delete(string key);
        Task<bool> CheckKeyOrigin(string ipOrigin, string apiKey);
    }

    public sealed class CApiKeyRepository : CRepository, IApiKeyRepository {

        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;

        private readonly IOraPg _orapg;

        public CApiKeyRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IApplicationService app,
            IGlobalService gs,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            _app = app;
            _gs = gs;
            _orapg = orapg;
        }

        public async Task<bool> Create(DC_APIKEY_T apiKey) {
            _orapg.Set<DC_APIKEY_T>().Add(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_APIKEY_T>> GetAll(string key = null) {
            DbSet<DC_APIKEY_T> dbSet = _orapg.Set<DC_APIKEY_T>();
            IQueryable<DC_APIKEY_T> query = null;
            if (!string.IsNullOrEmpty(key)) {
                query = dbSet.Where(ak => ak.KEY.ToUpper() == key.ToUpper() && ak.APP_NAME.ToUpper() == _app.AppName.ToUpper());
            }
            return await ((query == null) ? dbSet : query).ToListAsync();
        }

        public async Task<DC_APIKEY_T> GetById(string key) {
            return await _orapg.Set<DC_APIKEY_T>().Where(ak => ak.KEY.ToUpper() == key.ToUpper()).SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string key) {
            DC_APIKEY_T apiKey = await GetById(key);
            _orapg.Set<DC_APIKEY_T>().Remove(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<bool> CheckKeyOrigin(string ipOrigin, string apiKey) {
            DC_APIKEY_T ak = await GetById(apiKey);
            if (ak != null) {
                bool IP_ORIGIN = ak.IP_ORIGIN.ToUpper().Split(";").Select(io => io.Trim()).Contains(ipOrigin.ToUpper()) || ak.IP_ORIGIN == "*";
                bool APP_NAME = ak.APP_NAME?.ToUpper() == _app.AppName.ToUpper() || ak.APP_NAME?.ToUpper() == "*";
                if (string.IsNullOrEmpty(ak.APP_NAME)) {
                    return IP_ORIGIN;
                }
                return IP_ORIGIN && APP_NAME;
            }
            return _gs.AllowedIpOrigin.Contains(ipOrigin);
        }

    }

}
