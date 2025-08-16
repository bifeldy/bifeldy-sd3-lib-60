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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Quartz;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.JobSchedulers {

    public sealed class GenericJob : IJob {

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GenericJob> _logger;
        private readonly IApplicationService _as;

        public GenericJob(
            IServiceProvider serviceProvider,
            ILogger<GenericJob> logger,
            IApplicationService @as
        ) {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
            this._as = @as;
        }

        public async Task Execute(IJobExecutionContext context) {
            IOraPg ___orapg = this._serviceProvider.GetRequiredService<IOraPg>();

            foreach (KeyValuePair<string, object> jdm in context.JobDetail.JobDataMap) {
                try {
                    string errorMessage = null;

                    _ = await ___orapg.ExecQueryAsync(
                        $@"
                            INSERT INTO api_quartz_job_queue (app_name, job_name, start_at, completed_at, error_message)
                            VALUES (:app_name, :job_name, CURRENT_TIMESTAMP, NULL, NULL)
                        ",
                        new List<CDbQueryParamBind>() {
                            new() { NAME = "app_name", VALUE = this._as.AppName.ToUpper() },
                            new() { NAME = "job_name", VALUE = jdm.Key }
                        }
                    );

                    try {
                        var func = (Func<IJobExecutionContext, IServiceProvider, Task>)jdm.Value;
                        await func(context, this._serviceProvider);
                    }
                    catch (Exception ex) {
                        errorMessage = ex.Message;
                    }

                    _ = await ___orapg.ExecQueryAsync(
                        $@"
                            UPDATE
                                api_quartz_job_queue
                            SET
                                error_message = :error_message {(string.IsNullOrEmpty(errorMessage) ? ", completed_at = CURRENT_TIMESTAMP" : "")}
                            WHERE
                                app_name = :app_name
                                AND job_name = :job_name
                        ",
                        new List<CDbQueryParamBind>() {
                            new() { NAME = "error_message", VALUE = errorMessage },
                            new() { NAME = "app_name", VALUE = this._as.AppName.ToUpper() },
                            new() { NAME = "job_name", VALUE = jdm.Key }
                        }
                    );
                }
                catch (JobExecutionException e) {
                    this._logger.LogError("[{name}_ERROR] ⌚ {ex}", $"{this.GetType().Name}_{jdm.Key}", e.Message);
                    throw;
                }
                catch (Exception e) {
                    this._logger.LogError("[{name}_ERROR] ⌚ {ex}", $"{this.GetType().Name}_{jdm.Key}", e.Message);
                }
            }
        }

    }

}
