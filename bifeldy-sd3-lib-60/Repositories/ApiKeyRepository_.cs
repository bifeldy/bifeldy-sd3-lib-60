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
        Task<List<DC_APIKEY_T>> GetAll();
        Task<DC_APIKEY_T> GetById(string key);
        Task<bool> Update(string key, DC_APIKEY_T apiKey);
        Task<bool> Delete(string key);
        Task<bool> CheckKeyOrigin(string ipOrigin, string apiKey);
    }

    public sealed class CApiKeyRepository : CRepository, IApiKeyRepository {

        private readonly IGlobalService _gs;

        private readonly IOraPg _orapg;

        public CApiKeyRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IGlobalService gs,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            _gs = gs;
            _orapg = orapg;
        }

        public async Task<bool> Create(DC_APIKEY_T apiKey) {
            _orapg.Set<DC_APIKEY_T>().Add(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_APIKEY_T>> GetAll() {
            return await _orapg.Set<DC_APIKEY_T>().ToListAsync();
        }

        public async Task<DC_APIKEY_T> GetById(string key) {
            return await _orapg.Set<DC_APIKEY_T>().Where(ak => ak.KEY == key).FirstOrDefaultAsync();
        }

        public async Task<bool> Update(string key, DC_APIKEY_T newApiKey) {
            DC_APIKEY_T oldApiKey = await _orapg.Set<DC_APIKEY_T>().Where(ak => ak.KEY == key).FirstOrDefaultAsync();
            oldApiKey.APP_NAME = newApiKey.APP_NAME;
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string key) {
            DC_APIKEY_T apiKey = await _orapg.Set<DC_APIKEY_T>().Where(ak => ak.KEY == key).FirstOrDefaultAsync();
            _orapg.Set<DC_APIKEY_T>().Remove(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        public async Task<bool> CheckKeyOrigin(string ipOrigin, string apiKey) {
            DC_APIKEY_T ak = await GetById(apiKey);
            List<string> allowed = new List<string>(_gs.AllowedIpOrigin);
            if (ak != null) {
                allowed.Add(ak.IP_ORIGIN);
            }
            return allowed.Contains(ipOrigin);
        }

    }

}
