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
            var networkId = "testnet";
            Console.WriteLine("Hello World!");

            var lr = new LogReader(networkId);
            var hist = await lr.GetHistoryAsync();

            string lyrawalletfolder = Wallet.GetFullFolderName(networkId, "wallets");
            var walletStore = new SecuredWalletStore(lyrawalletfolder);
            var incWallet = Wallet.Open(walletStore, "incentive", "");
            var client = LyraRestClient.Create(networkId, "", "", "");
            await incWallet.Sync(client);

            var latest = hist.OrderByDescending(x => x.TimeStamp)
                .FirstOrDefault();
            if(latest != null)
            {
                foreach(var node in latest.nodeStatus)
                {
                    var result = await incWallet.Send(100m, node.Key);
                    Console.WriteLine($"{result.ResultCode} Send to {node.Key}");
                }
            }
        }
    }
}
