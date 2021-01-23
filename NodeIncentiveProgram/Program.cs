using System;

namespace NodeIncentiveProgram
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("need arguments.");
                return;
            }
                
            var networkId = args[0];
            Console.WriteLine($"Beginning incentive program for {networkId}...");

            var inc = new IncentivePayer(networkId);
            await inc.RunPayAsync();
        }
    }
}
