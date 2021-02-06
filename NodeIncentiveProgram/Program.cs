using System;

namespace NodeIncentiveProgram
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {              
            Console.WriteLine($"Beginning incentive program...");

            var inc = new IncentivePayer();
            await inc.RunPayAsync();
        }
    }
}
