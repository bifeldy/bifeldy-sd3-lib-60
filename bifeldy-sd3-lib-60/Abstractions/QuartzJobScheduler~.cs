/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Template Quartz Job
*              :: Model Supaya Tidak Perlu Install Package Nuget Quartz
* 
*/

using Quartz;

namespace bifeldy_sd3_lib_60.Abstractions {

    public abstract class CQuartzJobScheduler : IJob {

        protected IJobExecutionContext _context;

        public Task Execute(IJobExecutionContext context) {
            _context = context;
            return DoWork();
        }

        public abstract Task DoWork();

    }

}
