using Lyra.Core.Accounts;
using Lyra.Core.API;
using System;
using System.Linq;

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

            var lr = new LogReader(networkId);
            var hist = await lr.GetHistoryAsync();

            string lyrawalletfolder = Wallet.GetFullFolderName(networkId, "wallets");
            var walletStore = new SecuredWalletStore(lyrawalletfolder);
            var incWallet = Wallet.Open(walletStore, "incentive", "");
            var client = LyraRestClient.Create(networkId, "", "", "");
            var syncResult = await incWallet.Sync(client);
            if (syncResult != Lyra.Core.Blocks.APIResultCodes.Success)
            {
                Console.WriteLine("Can't sync wallet!");
                return;
            }

            Console.WriteLine($"Incentive wallet {incWallet.AccountId} balance: {incWallet.BaseBalance}\n");

            var latest = hist.OrderByDescending(x => x.TimeStamp)
                .FirstOrDefault();
            if(latest != null)
            {
                foreach(var node in latest.nodeStatus)
                {
                    Console.WriteLine($"Sending payment to {node.Key}");
                    //var result = await incWallet.Send(100m, node.Key);
                    //if(result.ResultCode != Lyra.Core.Blocks.APIResultCodes.Success)
                    //{
                    //    Console.WriteLine($"{result.ResultCode} Send to {node.Key}");
                    //    Console.WriteLine($"Retrying...");
                    //    result = await incWallet.Send(100m, node.Key);
                    //}
                        
                    //Console.WriteLine($"{result.ResultCode} Send to {node.Key}");
                }
            }
        }
    }
}
