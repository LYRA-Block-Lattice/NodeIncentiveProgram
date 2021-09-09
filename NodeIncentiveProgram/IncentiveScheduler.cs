using Lyra.Core.API;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeIncentiveProgram
{
    /* The task scheduler for incentive program
     * 
     * 
     */
    public partial class IncentiveScheduler
    {
        private IScheduler _sched;
        private string _network;

        public IncentiveScheduler(string network)
        {
            _network = network;
        }
        // Init the scheduler
        public async Task InitJobSchedulerAsync()
        {
            if (_sched != null)
                return;

            //Quartz.Logging.LogContext.SetCurrentLogProvider(SimpleLogger.Factory);

            // First we must get a reference to a scheduler
            ISchedulerFactory sf = new StdSchedulerFactory();
            _sched = await sf.GetScheduler();
            _sched.Context.Add("ic", this);

            // computer a time that is on the next round minute
            DateTimeOffset runTime = DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow);

            // define the jobs
            var jobGroup = "inc jobs";

            // Tell quartz to schedule the job using our trigger
            await CreateJobAsync(TimeSpan.FromMinutes(2), typeof(PayNodes), "Pay staking nodes", jobGroup);
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
        private class PayNodes : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                try
                {
                    var ic = context.Scheduler.Context.Get("ic") as IncentiveScheduler;
                    var client = LyraRestClient.Create(ic._network, "Win", "IncProg", "1.0");
                    var bb = await client.GetBillBoardAsync();

                    if(bb.ActiveNodes.Any())
                    {
                        var wallet = new IncWallet(ic._network, "incentive", "");
                        await wallet.OpenAsync();

                        var amount = 10;

                        foreach(var node in bb.ActiveNodes)
                        {
                            Console.WriteLine($"Pay {amount} to {node.AccountID}");

                            await wallet.PayAsync(node.AccountID, amount);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error paying: {ex}");
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
