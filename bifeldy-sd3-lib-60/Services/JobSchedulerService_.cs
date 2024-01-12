/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Job Buat Bikin Job .. Wkwkwk
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.Extensions.Configuration;

using Quartz;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IJobSchedulerService : IJob { }

    public sealed class CJobSchedulerService : IJobSchedulerService {

        private readonly IConfiguration _config;

        private int RandomId;

        private IScheduler _workerScheduler = null;
        private List<WorkerJob> _workers = new List<WorkerJob>();

        public CJobSchedulerService(IConfiguration config) : base() {
            RandomId = new Random().Next(1, 1000);

            _config = config;
        }

        public void AddWorkers(IScheduler workerScheduler, List<WorkerJob> workers) {
            _workerScheduler = workerScheduler;
            _workers = workers;
        }

        ITrigger getNewTrigger(string name, string group, string schedule) {
            return TriggerBuilder.Create()
                .WithIdentity(name, group)
                .WithCronSchedule(schedule)
                .Build();
        }

        async Task RescheduleJob() {
            Console.WriteLine("Rescheduling worker jobs");
            if (_workerScheduler != null) {
                foreach (WorkerJob worker in _workers) {
                    IJobDetail job = await _workerScheduler.GetJobDetail(worker.GetJobKey);
                    Console.WriteLine($"--Rescheduling worker {job.Key}");
                    ITrigger trigger = await _workerScheduler.GetTrigger(worker.GetTriggerKey);
                    if (trigger != null) {
                        ICronTrigger cronTrigger = (ICronTrigger) trigger;
                        string newExpression = _config.GetSection(worker.AppSettingsJobNameCronExpressionKey).Value;
                        if (cronTrigger.CronExpressionString != newExpression) {
                            Console.WriteLine($"--Rescheduling job for trigger {trigger.Key.Name}");
                            ITrigger newTrigger = getNewTrigger(trigger.Key.Name, trigger.Key.Group, newExpression);
                            await _workerScheduler.RescheduleJob(trigger.Key, newTrigger);
                        }
                    }
                }

            }
            else Console.WriteLine("Could not reschedule worker as the scheduler is null");
        }

        public Task Execute(IJobExecutionContext context) {
            try {
                Console.WriteLine($"Running JobScheduleManager Job {RandomId} at " + DateTime.Now.ToString());
                var triggerTime = _config.GetSection($"JOB_SCHEDULER:JOB_SERVICE").Value;
                Console.WriteLine($"Current JobScheduleManager trigger time: {triggerTime}");

                Task task = Task.Run(async () => await RescheduleJob());
                task.Wait();

                if (_workerScheduler != null && !_workerScheduler.IsStarted) {
                    Console.WriteLine("Starting worker scheduler as it has not been started yet");
                    _workerScheduler.Start();
                }

                return Task.CompletedTask;
            }
            catch (Exception e) {
                Console.WriteLine("Received an error when executing JobScheduleManager job. " + e.Message);
                return Task.FromException(e);
            }
        }

    }

}
