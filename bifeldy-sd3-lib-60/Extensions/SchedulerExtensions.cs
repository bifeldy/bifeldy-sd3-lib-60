/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Quartz;

using bifeldy_sd3_lib_60.JobSchedulers;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class SchedulerExtensions {

        private const string SCHEDULER_HIT_AND_RUN_GROUP = "HIT_AND_RUN";

        public static Task<DateTimeOffset> ScheduleJobRunNow(this IScheduler scheduler, IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action) {
            string jobName = string.Join("___", action.Select(a => a.Key));
            var jobData = new JobDataMap();

            foreach (KeyValuePair<string, Func<IJobExecutionContext, IServiceProvider, Task>> kvp in action) {
                jobData.Add(kvp.Key, kvp.Value);
            }

            IJobDetail jobDetail = JobBuilder.Create<GenericJob>().WithIdentity(jobName, SCHEDULER_HIT_AND_RUN_GROUP).UsingJobData(jobData).Build();
            ITrigger trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(1))).Build();

            return scheduler.ScheduleJob(jobDetail, trigger);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNow(this IScheduler scheduler, string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action) {
            var jobData = new Dictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> {
                { jobName, action }
            };

            return scheduler.ScheduleJobRunNow(jobData);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelay(this IScheduler scheduler, IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay) {
            string jobName = string.Join("___", action.Select(a => a.Key));
            var jobData = new JobDataMap();

            foreach (KeyValuePair<string, Func<IJobExecutionContext, IServiceProvider, Task>> kvp in action) {
                jobData.Add(kvp.Key, kvp.Value);
            }

            IJobDetail jobDetail = JobBuilder.Create<GenericJob>().WithIdentity(jobName, SCHEDULER_HIT_AND_RUN_GROUP).UsingJobData(jobData).Build();
            ITrigger trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(initialDelay)).Build();

            return scheduler.ScheduleJob(jobDetail, trigger);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelay(this IScheduler scheduler, string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay) {
            var jobData = new Dictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> {
                { jobName, action }
            };

            return scheduler.ScheduleJobRunNowWithDelay(jobData, initialDelay);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(this IScheduler scheduler, IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay, TimeSpan interval) {
            string jobName = string.Join("___", action.Select(a => a.Key));
            var jobData = new JobDataMap();

            foreach (KeyValuePair<string, Func<IJobExecutionContext, IServiceProvider, Task>> kvp in action) {
                jobData.Add(kvp.Key, kvp.Value);
            }

            IJobDetail jobDetail = JobBuilder.Create<GenericJob>().WithIdentity(jobName, SCHEDULER_HIT_AND_RUN_GROUP).UsingJobData(jobData).Build();

            ITrigger trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.UtcNow.Add(initialDelay))
                .WithSimpleSchedule(s => s.WithInterval(interval).RepeatForever())
                .Build();

            return scheduler.ScheduleJob(jobDetail, trigger);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(this IScheduler scheduler, string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay, TimeSpan interval) {
            var jobData = new Dictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> {
                { jobName, action }
            };

            return scheduler.ScheduleJobRunNowWithDelayInterval(jobData, initialDelay, interval);
        }

    }

}
