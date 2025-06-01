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

        public static Task<DateTimeOffset> ScheduleJobRunNow(this IScheduler scheduler, IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action) {
            var data = new JobDataMap();
            foreach (KeyValuePair<string, Func<IJobExecutionContext, IServiceProvider, Task>> kvp in action) {
                data.Add(kvp.Key, kvp.Value);
            }

            IJobDetail jobDetail = JobBuilder.Create<GenericJob>().UsingJobData(data).Build();
            ITrigger trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(1))).Build();

            return scheduler.ScheduleJob(jobDetail, trigger);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNow(this IScheduler scheduler, string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action) {
            var data = new Dictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> {
                { jobName, action }
            };

            return scheduler.ScheduleJobRunNow(data);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelay(this IScheduler scheduler, IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay) {
            var data = new JobDataMap();
            foreach (KeyValuePair<string, Func<IJobExecutionContext, IServiceProvider, Task>> kvp in action) {
                data.Add(kvp.Key, kvp.Value);
            }

            IJobDetail jobDetail = JobBuilder.Create<GenericJob>().UsingJobData(data).Build();
            ITrigger trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(initialDelay)).Build();

            return scheduler.ScheduleJob(jobDetail, trigger);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelay(this IScheduler scheduler, string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay) {
            var data = new Dictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> {
                { jobName, action }
            };

            return scheduler.ScheduleJobRunNowWithDelay(data, initialDelay);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(this IScheduler scheduler, IDictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> action, TimeSpan initialDelay, TimeSpan interval) {
            var data = new JobDataMap();
            foreach (KeyValuePair<string, Func<IJobExecutionContext, IServiceProvider, Task>> kvp in action) {
                data.Add(kvp.Key, kvp.Value);
            }

            IJobDetail jobDetail = JobBuilder.Create<GenericJob>().UsingJobData(data).Build();

            ITrigger trigger = TriggerBuilder.Create()
                .StartAt(DateTimeOffset.UtcNow.Add(initialDelay))
                .WithSimpleSchedule(s => s.WithInterval(interval).RepeatForever())
                .Build();

            return scheduler.ScheduleJob(jobDetail, trigger);
        }

        public static Task<DateTimeOffset> ScheduleJobRunNowWithDelayInterval(this IScheduler scheduler, string jobName, Func<IJobExecutionContext, IServiceProvider, Task> action, TimeSpan initialDelay, TimeSpan interval) {
            var data = new Dictionary<string, Func<IJobExecutionContext, IServiceProvider, Task>> {
                { jobName, action }
            };

            return scheduler.ScheduleJobRunNowWithDelayInterval(data, initialDelay, interval);
        }

    }

}
