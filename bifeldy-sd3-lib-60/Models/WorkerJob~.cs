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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Quartz;

namespace bifeldy_sd3_lib_60.Models {

    public class WorkerJob : IJob {

        private Type JobType { get; }

        public string JobName { get; }
        public string JobGroup { get; }
        public string TriggerName { get; }
        public string TriggerGroup { get; }

        public string AppSettingsJobNameCronExpressionKey { get; }

        public WorkerJob(
            Type jobType,
            string jobName = null, string jobGroup = null, string triggerName = null, string triggerGroup = null,
            string appSettingsJobNameCronExpressionKey = null
        ) {
            JobType = jobType;
            JobName = jobName ?? jobType.Name;
            JobGroup = jobGroup ?? jobType.Name;
            TriggerName = triggerName ?? jobType.Name;
            TriggerGroup = triggerGroup ?? jobType.Name;
            AppSettingsJobNameCronExpressionKey = appSettingsJobNameCronExpressionKey ?? jobType.Name;
        }

        public JobKey GetJobKey {
            get {
                return new JobKey(JobName, JobGroup);
            }
        }

        public TriggerKey GetTriggerKey {
            get {
                return new TriggerKey(TriggerName, TriggerGroup);
            }
        }

        public virtual Task Execute(IJobExecutionContext context) {
            return Task.CompletedTask;
        }

        public virtual IJob GetJob(IServiceProvider serviceProvider) {
            return serviceProvider.GetService<WorkerJob>();
        }

        public async Task<IScheduler> ConfigureScheduler(IConfiguration config, IScheduler scheduler) {
            string appSettingName = $"JOB_SCHEDULER:{AppSettingsJobNameCronExpressionKey}";
            string triggerTimeCronExpression = config.GetSection(appSettingName).Value;
            
            JobBuilder jobBuilder = JobBuilder.Create(JobType);
            jobBuilder.WithIdentity(JobName, JobGroup);
            
            IJobDetail job = jobBuilder.Build();
            
            TriggerBuilder triggerBuilder = TriggerBuilder.Create();
            triggerBuilder.WithIdentity(TriggerName, TriggerGroup);
            triggerBuilder.WithCronSchedule(triggerTimeCronExpression);
            
            ITrigger trigger = triggerBuilder.Build();
            
            await scheduler.ScheduleJob(job, trigger);
            return scheduler;
        }

    }

}
