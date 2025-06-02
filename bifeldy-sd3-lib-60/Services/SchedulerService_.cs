/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Quartz Job Scheduler
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Databases;

namespace bifeldy_sd3_lib_60.Services {

    public interface ISchedulerService {
        public Task<DateTimeOffset> ScheduleJobRunNow(IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action);
        public Task<DateTimeOffset> ScheduleJobRunNow(string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action);
        public Task<DateTimeOffset> ScheduleJobRunNowWithDelay(IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay);
        public Task<DateTimeOffset> ScheduleJobRunNowWithDelay(string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay);
        public Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay, TimeSpan interval);
        public Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay, TimeSpan interval);
        Task<JobExecutionException> CreateThrowRetry(string jobName, IJobExecutionContext ctx, Exception ex, int delaySecond = 1);
        Task<bool> CheckJobIsNeedToCreateNew(string fileName, bool orCustomBoolCheck = false);
    }

    [SingletonServiceRegistration]
    public sealed class CSchedulerService : ISchedulerService {

        private readonly IScheduler _scheduler;
        private readonly IApplicationService _app;
        private readonly IOraPg _orapg;

        public CSchedulerService(
            IScheduler scheduler,
            IApplicationService app,
            IOraPg orapg
        ) {
            this._scheduler = scheduler;
            this._app = app;
            this._orapg = orapg;
        }

        public Task<DateTimeOffset> ScheduleJobRunNow(IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action) {
            return this._scheduler.ScheduleJobRunNow(action);
        }

        public Task<DateTimeOffset> ScheduleJobRunNow(string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action) {
            return this._scheduler.ScheduleJobRunNow(jobName, action);
        }

        public Task<DateTimeOffset> ScheduleJobRunNowWithDelay(IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay) {
            return this._scheduler.ScheduleJobRunNowWithDelay(action, initialDelay);
        }

        public Task<DateTimeOffset> ScheduleJobRunNowWithDelay(string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay) {
            return this._scheduler.ScheduleJobRunNowWithDelay(jobName, action, initialDelay);
        }

        public Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay, TimeSpan interval) {
            return this._scheduler.ScheduleJobRunNowWithDelayInterval(action, initialDelay, interval);
        }

        public Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay, TimeSpan interval) {
            return this._scheduler.ScheduleJobRunNowWithDelayInterval(jobName, action, initialDelay, interval);
        }

        public async Task<JobExecutionException> CreateThrowRetry(string jobName, IJobExecutionContext ctx, Exception ex, int delaySecond = 1) {
            var retryTrigger = new SimpleTriggerImpl(Guid.NewGuid().ToString()) {
                Description = $"RetryTrigger_{jobName}",
                RepeatCount = 0,
                JobKey = ctx.JobDetail.Key,
                StartTimeUtc = DateBuilder.NextGivenSecondDate(DateTime.Now, delaySecond)
            };

            _ = await ctx.Scheduler.ScheduleJob(retryTrigger);

            return new JobExecutionException(ex, false);
        }

        public async Task<bool> CheckJobIsNeedToCreateNew(string jobName, bool orCustomBoolCheck = false) {
            bool bikinJobBaru = false;

            IReadOnlyCollection<JobKey> jobKeys = await this._scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            JobKey jk = jobKeys.Where(jk => jk.Name == jobName).FirstOrDefault();

            IJobDetail jd = null;
            if (jk != null) {
                jd = await this._scheduler.GetJobDetail(jk);
            }

            decimal recordCount = await this._orapg.ExecScalarAsync<decimal>(
                $@"
                    SELECT COUNT(*)
                    FROM api_quartz_job_queue
                    WHERE
                        app_name = :app_name
                        AND job_name = :job_name
                ",
                new List<CDbQueryParamBind>() {
                    new() { NAME = "app_name", VALUE = this._app.AppName.ToUpper() },
                    new() { NAME = "job_name", VALUE = jobName }
                }
            );

            if (jd != null && recordCount <= 0) {
                _ = await this._orapg.ExecQueryAsync(
                    $@"
                        INSERT INTO api_quartz_job_queue (app_name, job_name, start_at, completed_at)
                        VALUES (:app_name, :job_name, CURRENT_TIMESTAMP, NULL)
                    ",
                    new List<CDbQueryParamBind>() {
                        new() { NAME = "app_name", VALUE = this._app.AppName.ToUpper() },
                        new() { NAME = "job_name", VALUE = jobName }
                    }
                );
            }
            else if (jd != null && recordCount > 0) {
                // SKIP -- Masih Proses
            }
            else if (jd == null && recordCount <= 0) {
                bikinJobBaru = true;
            }
            else if (jd == null && recordCount > 0) {
                DateTime? completedAt = await this._orapg.ExecScalarAsync<DateTime?>(
                    $@"
                        SELECT completed_at
                        FROM api_quartz_job_queue
                        WHERE
                            app_name = :app_name
                            AND job_name = :job_name
                    ",
                    new List<CDbQueryParamBind>() {
                        new() { NAME = "app_name", VALUE = this._app.AppName.ToUpper() },
                        new() { NAME = "job_name", VALUE = jobName }
                    }
                );

                if (completedAt == null || orCustomBoolCheck) {
                    bikinJobBaru = true;
                }
            }

            return bikinJobBaru;
        }

    }

}
