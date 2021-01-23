using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lyra.Core.Accounts;
using Lyra.Core.API;

namespace NodeIncentiveProgram
{
    public class IncentivePayer
    {
        private string _networkId;
        public IncentivePayer(string networkId)
        {
            _networkId = networkId;
        }

        public async System.Threading.Tasks.Task RunPayAsync()
        {
            string lyrawalletfolder = Wallet.GetFullFolderName(_networkId, "wallets");
            var walletStore = new SecuredWalletStore(lyrawalletfolder);
            var incWallet = Wallet.Open(walletStore, "incentive", "");
            var client = LyraRestClient.Create(_networkId, "", "", "");
            var syncResult = await incWallet.Sync(client);
            if (syncResult != Lyra.Core.Blocks.APIResultCodes.Success)
            {
                Console.WriteLine("Can't sync wallet!");
                return;
            }

            Console.WriteLine($"Incentive wallet {incWallet.AccountId} balance: {incWallet.BaseBalance}\n");

            Console.WriteLine($"Getting history data from nebula...");
            var lr = new LogReader(_networkId);
            var hist = (await lr.GetHistoryAsync())
                .Where(x => x.TimeStamp > DateTime.UtcNow.AddDays(-1))
                .ToList();

            // first get all accounts
            var allAccounts = hist.SelectMany(x => x.nodeStatus.Keys).Distinct().ToList();
            Console.WriteLine($"Got {hist.Count()} history entry for {_networkId}. Unique account {allAccounts.Count()}");

            var count = hist.Count;
            var statusList = new List<XStatus>();
            foreach(var acct in allAccounts)
            {
                var lastState = hist.Last(x => x.nodeStatus.ContainsKey(acct))
                    .nodeStatus[acct];

                if (lastState == null)
                    continue;

                var lastBB = hist.Last().bb;
                var lastInBB = lastBB.ActiveNodes.FirstOrDefault(x => x.AccountID == acct);


                var xs = new XStatus
                {
                    NetworkId = _networkId,
                    AccountId = acct,
                    OfflineCount = count - hist.Count(x => x.nodeStatus.ContainsKey(acct)),
                    FullyUpgraded = lastState.Status.version == LyraGlobal.NodeAppName,
                    IsPrimary = lastBB.PrimaryAuthorizers.Contains(acct),
                    PosVotes = lastInBB == null ? 0 : lastInBB.Votes,
                    SharedIp = hist.Last(x => x.nodeStatus.ContainsKey(acct))
                        .bb.NodeAddresses.Count(x => x.Value == hist.Last(x => x.nodeStatus.ContainsKey(acct))
                        .bb.NodeAddresses[acct]) > 1
                };

                statusList.Add(xs);
            }

            var index = 1;
            Console.WriteLine("index NetworkId AccountId OfflineCount FullyUpgraded IsPrimary PosVotes ");
            foreach (var node in statusList)
            {
                Console.WriteLine($"No. {index} {Math.Round(node.GetRito() * 100,4):n} {node.NetworkId} {node.AccountId.Substring(0, 10)} {node.OfflineCount} {node.FullyUpgraded} {node.IsPrimary} {node.PosVotes} {node.SharedIp}");
                index++;
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
