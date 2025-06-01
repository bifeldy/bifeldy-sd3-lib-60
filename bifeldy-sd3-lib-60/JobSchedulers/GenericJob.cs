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

using Microsoft.Extensions.Logging;

using Quartz;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.JobSchedulers {

    public sealed class GenericJob : IJob {

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GenericJob> _logger;
        private readonly IOraPg _orapg;

        public GenericJob(IServiceProvider serviceProvider, ILogger<GenericJob> logger, IOraPg orapg) {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
            this._orapg = orapg;
        }

        public async Task Execute(IJobExecutionContext context) {
            foreach (KeyValuePair<string, object> jdm in context.JobDetail.JobDataMap) {
                string errorMessage = null;

                _ = await this._orapg.ExecQueryAsync(
                    $@"
                        DELETE FROM api_quartz_job_queue
                        WHERE job_name = :job_name
                    ",
                    new List<CDbQueryParamBind>() {
                        new() { NAME = "job_name", VALUE = jdm.Key }
                    }
                );

                try {
                    var func = (Func<IJobExecutionContext, IServiceProvider, Task>)jdm.Value;

                    _ = await this._orapg.ExecQueryAsync(
                        $@"
                            INSERT INTO api_quartz_job_queue (job_name, start_at, completed_at, error_message)
                            VALUES (:job_name, CURRENT_TIMESTAMP, NULL, NULL)
                        ",
                        new List<CDbQueryParamBind>() {
                            new() { NAME = "job_name", VALUE = jdm.Key }
                        }
                    );

                    await func(context, this._serviceProvider);
                }
                catch (Exception ex) {
                    errorMessage = ex.Message;
                    this._logger.LogError("[{Name}_ERROR] ⌚ {Message}", $"{this.GetType().Name}_{jdm.Key}", errorMessage);
                }

                _ = await this._orapg.ExecQueryAsync(
                    $@"
                        UPDATE api_quartz_job_queue
                        SET error_message = :error_message {(string.IsNullOrEmpty(errorMessage) ? ", completed_at = CURRENT_TIMESTAMP" : "")}
                        WHERE job_name = :job_name
                    ",
                    new List<CDbQueryParamBind>() {
                        new() { NAME = "error_message", VALUE = errorMessage },
                        new() { NAME = "job_name", VALUE = jdm.Key }
                    }
                );
            }
        }

    }

}
