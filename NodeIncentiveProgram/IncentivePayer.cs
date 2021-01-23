﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Lyra.Core.Accounts;
using Lyra.Core.API;

namespace NodeIncentiveProgram
{
    public class IncentivePayer
    {
        public IncentivePayer()
        {

        }

        public async System.Threading.Tasks.Task RunPayAsync()
        {
            string lyrawalletfolder = Wallet.GetFullFolderName("mainnet", "wallets");
            var walletStore = new SecuredWalletStore(lyrawalletfolder);
            var incWallet = Wallet.Open(walletStore, "incentive", "");
            var client = LyraRestClient.Create("mainnet", "", "", "");
            var syncResult = await incWallet.Sync(client);
            if (syncResult != Lyra.Core.Blocks.APIResultCodes.Success)
            {
                Console.WriteLine("Can't sync wallet!");
                return;
            }

            Console.WriteLine($"Incentive wallet {incWallet.AccountId} balance: {incWallet.BaseBalance}\n");

            var incPay = new IncPayment();
            incPay.TimeStamp = DateTime.UtcNow;

            incPay.MainnetNodes = await GetStatusFromNetworkAsync("mainnet");
            incPay.TestnetNodes = await GetStatusFromNetworkAsync("testnet");

            var dbfn = @"C:\Users\Wizard\OneDrive\dev\lyra\Programs\IncProgram.db";
            using (var db = new LiteDatabase(dbfn))
            {
                var coll = db.GetCollection<IncPayment>("IncPay");                
                coll.Insert(incPay);                

                var index = 1;
                Console.WriteLine("index SuccessPaid Rito NetworkId AccountId OfflineCount FullyUpgraded IsPrimary PosVotes SharedIP");
                decimal totalPayment = 0m;
                decimal package = 500m;

                foreach(var nodes in new[] { incPay.MainnetNodes, incPay.TestnetNodes })
                foreach (var node in nodes)
                {
                    await PayNodeAsync(incWallet, node, package);
                    if(node.SuccessPaid)
                        totalPayment += node.PaidAmount;

                    var resultStr = node.SuccessPaid ? "Paid" : "Failed";
                    Console.WriteLine($"No. {index} {resultStr} {node.PaidAmount:n} LYR to {node.NetworkId} {node.AccountId.Substring(0, 10)}... {node.OfflineCount} {node.FullyUpgraded} {node.IsPrimary} {node.PosVotes} {node.SharedIp}");
                    index++;

                    coll.Update(incPay);
                }

                Console.WriteLine($"Total payment: {totalPayment:n} LYR");
            }            
        }

        private async System.Threading.Tasks.Task PayNodeAsync(Wallet wallet, XStatus node, decimal package)
        {
            try
            {
                var amount = package * node.GetRito();

                if (amount <= 0)
                {
                    amount = 1;
                }

                node.PaidAmount = amount;

                var result = await wallet.Send(amount, node.AccountId);
                if (result.ResultCode != Lyra.Core.Blocks.APIResultCodes.Success)
                {
                    await Task.Delay(10000);
                    result = await wallet.Send(amount, node.AccountId);
                }

                node.SuccessPaid = result.ResultCode == Lyra.Core.Blocks.APIResultCodes.Success;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error!!! {ex.Message}");
                node.SuccessPaid = false;
            }                       
        }

        private async System.Threading.Tasks.Task<List<XStatus>> GetStatusFromNetworkAsync(string networkId)
        {
            Console.WriteLine($"Getting history data from nebula...");
            var lr = new LogReader(networkId);
            var hist = (await lr.GetHistoryAsync())
                .Where(x => x.TimeStamp > DateTime.UtcNow.AddDays(-1))
                .ToList();

            // first get all accounts
            var allAccounts = hist.SelectMany(x => x.nodeStatus.Keys).Distinct().ToList();
            Console.WriteLine($"Got {hist.Count()} history entry for {networkId}. Unique account {allAccounts.Count()}");

            var count = hist.Count;
            var statusList = new List<XStatus>();
            foreach (var acct in allAccounts)
            {
                var lastState = hist.Last(x => x.nodeStatus.ContainsKey(acct))
                    .nodeStatus[acct];

                if (lastState == null)
                    continue;

                var lastBB = hist.Last().bb;
                var lastInBB = lastBB.ActiveNodes.FirstOrDefault(x => x.AccountID == acct);


                var xs = new XStatus
                {
                    NetworkId = networkId,
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

            return statusList;
        }
    }
}
