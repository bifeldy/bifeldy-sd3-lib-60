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

    public interface IAuthRepository {
        Task<bool> Create(DC_AUTH_T auth);
        Task<List<DC_AUTH_T>> GetAll(string userName = null);
        Task<DC_AUTH_T> GetByUserName(string userName);
        Task<DC_AUTH_T> GetByUserNamePass(string userName, string pass);
        Task<bool> Delete(string userName);
    }

    public sealed class CAuthRepository : CRepository, IAuthRepository {

        private readonly IOraPg _orapg;

        public CAuthRepository(
            IOptions<EnvVar> envVar,
            IApplicationService @as,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            _orapg = orapg;
        }

        public async Task<bool> Create(DC_AUTH_T userName) {
            _orapg.Set<DC_AUTH_T>().Add(userName);
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_AUTH_T>> GetAll(string userName = null) {
            DbSet<DC_AUTH_T> dbSet = _orapg.Set<DC_AUTH_T>();
            IQueryable<DC_AUTH_T> query = null;
            if (!string.IsNullOrEmpty(userName)) {
                query = dbSet.Where(a => a.USERNAME.ToUpper() == userName.ToUpper());
            }
            return await ((query == null) ? dbSet : query).ToListAsync();
        }

        public async Task<DC_AUTH_T> GetByUserName(string userName) {
            return await _orapg.Set<DC_AUTH_T>().Where(a => a.USERNAME.ToUpper() == userName.ToUpper()).SingleOrDefaultAsync();
        }

        public async Task<DC_AUTH_T> GetByUserNamePass(string userName, string pass) {
            return await _orapg.Set<DC_AUTH_T>()
                .Where(u => u.USERNAME.ToUpper() == userName.ToUpper() && u.PASS.ToUpper() == pass.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string userName) {
            DC_AUTH_T auth = await GetByUserName(userName);
            _orapg.Set<DC_AUTH_T>().Remove(auth);
            return await _orapg.SaveChangesAsync() > 0;
        }

    }

}
