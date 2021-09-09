using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyra.Core.Decentralize
{
    /* The task scheduler for consensus network
     * 
     * every 100 ms: check block consensus timeout
     * every even 2s: check leader tasks (cons, svc queue)
     * every even 1m0s: 
     * every even 10m0s: check new player or wellcome them to join
     */
    public partial class ConsensusService
    {
        private IScheduler _sched;
        // Init the scheduler
        private async Task InitJobSchedulerAsync()
        {
            if (_sched != null)
                return;

            //Quartz.Logging.LogContext.SetCurrentLogProvider(SimpleLogger.Factory);

            // First we must get a reference to a scheduler
            ISchedulerFactory sf = new StdSchedulerFactory();
            _sched = await sf.GetScheduler();
            _sched.Context.Add("cs", this);

            // computer a time that is on the next round minute
            DateTimeOffset runTime = DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow);

            // define the jobs
            var jobGroup = "consensus service jobs";

            // Tell quartz to schedule the job using our trigger
            await CreateJobAsync(TimeSpan.FromSeconds(24), typeof(HeartBeater), "Heart Beat", jobGroup);
            //await CreateJobAsync(TimeSpan.FromMilliseconds(100), typeof(BlockAuthorizationMonitor), "Block Monitor", jobGroup);
            //await CreateJobAsync("0/2 * * * * ?", typeof(LeaderTaskMonitor), "Leader Monitor", jobGroup);

            //// 10 min view change, 30 min fetch balance.
            //await CreateJobAsync("0 0/10 * * * ?", typeof(NewPlayerMonitor), "Player Monitor", jobGroup);
            //await CreateJobAsync(TimeSpan.FromMinutes(30), typeof(FetchBalance), "Fetch Balance", jobGroup);

            // Start up the scheduler (nothing can actually run until the
            // scheduler has been started)
            await _sched.Start();
        }

        //private async Task CloseJobScheduler()
        //{
        //    // shut down the scheduler
        //    await _sched.Shutdown(true);
        //}



        // jobs
        [DisallowConcurrentExecution]
        private class HeartBeater : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                try
                {
                    //if (Neo.Settings.Default.LyraNode.Lyra.Mode == Data.Utils.NodeMode.Normal)
                    //{
                    //    var cs = context.Scheduler.Context.Get("cs") as ConsensusService;
                    //    await cs.HeartBeatAsync();
                    //}
                }
                catch(Exception)
                {

                }
            }
        }

        private async Task CreateJobAsync(TimeSpan ts, Type job, string name, string group)
        {
            await _sched.ScheduleJob(
                JobBuilder
                .Create(job)
                .WithIdentity(name, group)
                .Build(),

                TriggerBuilder
                .Create()
                .WithIdentity($"{name} trigger", group)
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(ts)
                    .RepeatForever())
                .Build()
            );
        }

        private async Task CreateJobAsync(string cronStr, Type job, string name, string group)
        {
            await _sched.ScheduleJob(
                JobBuilder
                .Create(job)
                .WithIdentity(name, group)
                .Build(),

                TriggerBuilder.Create()
                .WithIdentity($"{name} trigger", group)
                .StartNow()
                .WithCronSchedule(cronStr)
                .Build()
            );
        }
    }

   
}
