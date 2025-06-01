/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Dipakai Untuk Hit & Run Di Background
 * 
 */

using Quartz;

namespace bifeldy_sd3_lib_60.JobSchedulers {

    public sealed class GenericJob : IJob {

        private readonly IServiceProvider _serviceProvider;

        public GenericJob(IServiceProvider serviceProvider) {
            this._serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context) {
            foreach (KeyValuePair<string, object> jdm in context.JobDetail.JobDataMap) {
                var func = (Func<IJobExecutionContext, IServiceProvider, Task>)jdm.Value;
                await func(context, this._serviceProvider);
            }
        }

    }

}
