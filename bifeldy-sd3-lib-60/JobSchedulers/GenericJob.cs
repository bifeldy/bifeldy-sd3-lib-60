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
using Microsoft.Extensions.Options;

using Quartz;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Abstractions;

namespace bifeldy_sd3_lib_60.JobSchedulers {

    public sealed class GenericJob : IJob {

        private readonly EnvVar _env;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GenericJob> _logger;
        private readonly IApplicationService _as;
        private readonly IOraPg _orapg;

        public GenericJob(
            IOptions<EnvVar> env,
            IServiceProvider serviceProvider,
            ILogger<GenericJob> logger,
            IApplicationService @as,
            IOraPg orapg
        ) {
            this._env = env.Value;
            this._serviceProvider = serviceProvider;
            this._logger = logger;
            this._as = @as;
            this._orapg = orapg;
        }

        public async Task Execute(IJobExecutionContext context) {
            foreach (KeyValuePair<string, object> jdm in context.JobDetail.JobDataMap) {
                try {
                    string errorMessage = null;

                    _ = await this._orapg.ExecQueryAsync(
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
                        var func = (Func<IJobExecutionContext, IServiceProvider, bool, IDatabase, Task>)jdm.Value;
                        await func(context, this._serviceProvider, this._env.IS_USING_POSTGRES, this._orapg);
                    }
                    catch (Exception ex) {
                        errorMessage = ex.Message;
                    }

                    _ = await this._orapg.ExecQueryAsync(
                        $@"
                            UPDATE api_quartz_job_queue
                            SET error_message = :error_message {(string.IsNullOrEmpty(errorMessage) ? ", completed_at = CURRENT_TIMESTAMP" : "")}
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
