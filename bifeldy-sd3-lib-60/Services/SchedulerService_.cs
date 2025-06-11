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

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface ISchedulerService {
        Task<JobExecutionException> CreateThrowRetry(string jobName, IJobExecutionContext ctx, Exception ex, int delaySecond = 1);
        Task<IEnumerable<JobKey>> GetJobs(string jobName = null);
        Task<int> JumlahJobYangSedangBerjalan(string jobName);
        Task<bool> CheckJobIsNeedToCreateNew(string fileName, IDatabase db, Func<Task<bool>> forceCreateNew = null);
        Task<bool> CheckJobIsCompleted(string jobName, IDatabase db, Func<Task<bool>> additionalAndCheck = null);
        public Task<DateTimeOffset?> ScheduleJobRunNow(string jobName, IDatabase db, Func<IJobExecutionContext, IServiceProvider, bool, IDatabase, Task> action, Func<Task<bool>> forceCreateNew = null);
        public Task<DateTimeOffset?> ScheduleJobRunNowWithDelay(string jobName, IDatabase db, Func<IJobExecutionContext, IServiceProvider, bool, IDatabase, Task> action, TimeSpan initialDelay, Func<Task<bool>> forceCreateNew = null);
        public Task<DateTimeOffset?> ScheduleJobRunNowWithDelayInterval(string jobName, IDatabase db, Func<IJobExecutionContext, IServiceProvider, bool, IDatabase, Task> action, TimeSpan initialDelay, TimeSpan interval, Func<Task<bool>> forceCreateNew = null);
        Task CancelAndDeleteJob(JobKey[] jobKeys, IDatabase db, string reason = "Job Dibatalkan");
    }

    [SingletonServiceRegistration]
    public sealed class CSchedulerService : ISchedulerService {

        private readonly IScheduler _scheduler;
        private readonly IApplicationService _app;

        public CSchedulerService(
            IScheduler scheduler,
            IApplicationService app
        ) {
            this._scheduler = scheduler;
            this._app = app;
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

        public async Task<IEnumerable<JobKey>> GetJobs(string jobName = null) {
            IReadOnlyCollection<JobKey> jobKeys = await this._scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            return jobKeys.Where(jk => string.IsNullOrEmpty(jobName) || jk.Name == jobName);
        }

        public async Task<int> JumlahJobYangSedangBerjalan(string jobName) {
            IEnumerable<JobKey> job = await this.GetJobs(jobName);
            return job.Count();
        }

        public async Task<bool> CheckJobIsNeedToCreateNew(string jobName, IDatabase db, Func<Task<bool>> forceCreateNew = null) {
            bool needToCreateNewJob = false;

            JobKey jk = (await this.GetJobs(jobName)).FirstOrDefault();

            IJobDetail jd = null;
            if (jk != null) {
                jd = await this._scheduler.GetJobDetail(jk);
            }

            decimal recordCount = await db.ExecScalarAsync<decimal>(
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
                _ = await db.ExecQueryAsync(
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
                needToCreateNewJob = true;
            }
            else if (jd == null && recordCount > 0) {
                DateTime? completedAt = await db.ExecScalarAsync<DateTime?>(
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

                if (forceCreateNew != null) {
                    needToCreateNewJob = await forceCreateNew();
                }

                if (completedAt == null || needToCreateNewJob) {
                    bool res = await db.ExecQueryAsync(
                        $@"
                            DELETE FROM api_quartz_job_queue
                            WHERE
                                app_name = :app_name
                                AND job_name = :job_name
                        ",
                        new List<CDbQueryParamBind>() {
                            new() { NAME = "app_name", VALUE = this._app.AppName.ToUpper() },
                            new() { NAME = "job_name", VALUE = jobName }
                        }
                    );

                    needToCreateNewJob = true;
                }
            }

            return needToCreateNewJob;
        }

        public async Task<bool> CheckJobIsCompleted(string jobName, IDatabase db, Func<Task<bool>> additionalAndCheck = null) {
            bool isJobCompleted = false;

            JobKey jk = (await this.GetJobs(jobName)).FirstOrDefault();

            IJobDetail jd = null;
            if (jk != null) {
                jd = await this._scheduler.GetJobDetail(jk);
            }

            decimal recordCount = await db.ExecScalarAsync<decimal>(
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

            if (jd == null && recordCount > 0) {
                DateTime? completedAt = await db.ExecScalarAsync<DateTime?>(
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


                if (completedAt != null) {
                    isJobCompleted = true;

                    if (additionalAndCheck != null) {
                        bool customBoolCheck = await additionalAndCheck();
                        isJobCompleted = isJobCompleted && customBoolCheck;
                    }
                }
            }

            return isJobCompleted;
        }

        public async Task<DateTimeOffset?> ScheduleJobRunNow(string jobName, IDatabase db, Func<IJobExecutionContext, IServiceProvider, bool, IDatabase, Task> action, Func<Task<bool>> forceCreateNew = null) {
            return await this.CheckJobIsNeedToCreateNew(jobName, db, forceCreateNew) ? await this._scheduler.ScheduleJobRunNow(jobName, action) : null;
        }

        public async Task<DateTimeOffset?> ScheduleJobRunNowWithDelay(string jobName, IDatabase db, Func<IJobExecutionContext, IServiceProvider, bool, IDatabase, Task> action, TimeSpan initialDelay, Func<Task<bool>> forceCreateNew = null) {
            return await this.CheckJobIsNeedToCreateNew(jobName, db, forceCreateNew) ? await this._scheduler.ScheduleJobRunNowWithDelay(jobName, action, initialDelay) : null;
        }

        public async Task<DateTimeOffset?> ScheduleJobRunNowWithDelayInterval(string jobName, IDatabase db, Func<IJobExecutionContext, IServiceProvider, bool, IDatabase, Task> action, TimeSpan initialDelay, TimeSpan interval, Func<Task<bool>> forceCreateNew = null) {
            return await this.CheckJobIsNeedToCreateNew(jobName, db, forceCreateNew) ? await this._scheduler.ScheduleJobRunNowWithDelayInterval(jobName, action, initialDelay, interval) : null;
        }

        public async Task CancelAndDeleteJob(JobKey[] jobKeys, IDatabase db, string reason = "Job Dibatalkan") {
            if (jobKeys?.Count() > 0) {
                var jks = new List<string>();

                foreach (JobKey jobKey in jobKeys) {
                    IJobDetail jobDetail = null;
                    if (jobKey != null) {
                        jobDetail = await this._scheduler.GetJobDetail(jobKey);
                    }

                    if (jobDetail != null) {
                        jks.Add(jobDetail.Key.Name);
                    }
                }

                _ = await this._scheduler.DeleteJobs(jobKeys);

                _ = await db.ExecQueryAsync(
                    $@"
                        UPDATE api_quartz_job_queue
                        SET
                            error_message = :error_message
                        WHERE
                            app_name = :app_name
                            AND job_name IN (:job_name)
                    ",
                    new List<CDbQueryParamBind>() {
                        new() { NAME = "error_message", VALUE = reason },
                        new() { NAME = "app_name", VALUE = this._app.AppName.ToUpper() },
                        new() { NAME = "job_name", VALUE = jks.ToArray() }
                    }
                );
            }
        }

    }

}
