﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var syncResult = await incWallet.SyncAsync(client);
            if (syncResult != Lyra.Core.Blocks.APIResultCodes.Success)
            {
                Console.WriteLine("Can't sync wallet!");
                return;
            }

            Console.WriteLine($"Incentive wallet {incWallet.AccountId} balance: {incWallet.BaseBalance}\n");

            // first lookup for failed payments
            //var dbfn = @"C:\Users\Wizard\OneDrive\dev\lyra\Programs\IncProgram.db";

            
                //var coll = db.GetCollection<IncPayment>("IncPay");

                /*async Task FixFailedPay()
                {
                    var lastPay = coll.FindOne(Query.All(Query.Descending));

                    Console.WriteLine($"Last payment time (UTC): {lastPay.TimeStamp}");

                    while (true)
                    {
                        foreach (var nodes in new[] { lastPay.MainnetNodes, lastPay.TestnetNodes })
                        {
                            if (nodes.Count(x => !x.SuccessPaid) == 0)
                            {
                                Console.WriteLine("No fix needed.");
                                return;
                            }

                            foreach (var node in nodes)
                            {
                                if (!node.SuccessPaid)
                                {
                                    Console.WriteLine($"fix failed pay for {node.AccountId} amont {node.PaidAmount}");
                                    await PayNodeAsync(incWallet, node, 0);
                                    coll.Update(lastPay);
                                }
                            }
                        }
                    }
                }*/

                //await FixFailedPay();

                var incPay = new IncPayment();
                incPay.TimeStamp = DateTime.UtcNow;

                incPay.MainnetNodes = await GetStatusFromNetworkAsync("mainnet");
                incPay.TestnetNodes = await GetStatusFromNetworkAsync("testnet");
          
                //coll.Insert(incPay);                

                var index = 1;
                Console.WriteLine("index SuccessPaid Rito NetworkId AccountId OfflineCount FullyUpgraded IsPrimary PosVotes SharedIP");
                decimal totalPayment = 0m;
                decimal package = 400m;

                foreach(var nodes in new[] { incPay.MainnetNodes, incPay.TestnetNodes })
                foreach (var node in nodes)
                {
                    await PayNodeAsync(incWallet, node, package);
                    if(node.SuccessPaid)
                        totalPayment += node.PaidAmount;

                    var resultStr = node.SuccessPaid ? "Paid" : "Failed";
                    Console.WriteLine($"No. {index} {resultStr} {node.PaidAmount:n} LYR to {node.NetworkId} {node.AccountId.Substring(0, 10)}... {node.OfflineCount} {node.FullyUpgraded} {node.IsPrimary} {node.PosVotes} {node.SharedIp}");
                    index++;

                    //coll.Update(incPay);
                }

                Console.WriteLine($"Total payment: {totalPayment:n} LYR");

                await Task.Delay(60 * 1000);

                //await FixFailedPay();
       
        }

        private async System.Threading.Tasks.Task PayNodeAsync(Wallet wallet, XStatus node, decimal package)
        {
            try
            {
                if(package != 0)
                {
                    var amount = package * node.GetRito();

                    if (amount < 1)
                    {
                        amount = 1;
                    }

                    node.PaidAmount = amount;
                }

                var result = await wallet.SendAsync(node.PaidAmount, node.AccountId);
                if (result.ResultCode != Lyra.Core.Blocks.APIResultCodes.Success)
                {
                    Console.WriteLine($"Failed to send: {result.ResultCode}");
                    await Task.Delay(10000);
                    await wallet.SyncAsync(null);
                    result = await wallet.SendAsync(node.PaidAmount, node.AccountId);
                    Console.WriteLine($"Retry result: {result.ResultCode}");
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
            Console.WriteLine($"Getting Billboard data from nebula...");
            //var lr = new LogReader(networkId);
            //var hist = (await lr.GetHistoryAsync())
            //    .Where(x => x.TimeStamp > DateTime.UtcNow.AddDays(-1))
            //    .ToList();

            var client = LyraRestClient.Create(networkId, "", "", "");
            var billboard = await client.GetBillBoardAsync();

            // first get all accounts
            var allAccounts = billboard.ActiveNodes.Select(a => a.AccountID);
            Console.WriteLine($"Unique account {allAccounts.Count()}");

            var statusList = new List<XStatus>();
            foreach (var acct in allAccounts)
            {
                var lastBB = billboard;
                var lastInBB = lastBB.ActiveNodes.FirstOrDefault(x => x.AccountID == acct);
                var state = billboard.ActiveNodes.First(a => a.AccountID == acct).State;
                var xs = new XStatus
                {
                    NetworkId = networkId,
                    AccountId = acct,
                    OfflineCount = 0,
                    FullyUpgraded = true,//CompareVersion(lastState.Status.version, LyraGlobal.NodeAppName),
                    IsPrimary = lastBB.PrimaryAuthorizers.Contains(acct),
                    PosVotes = lastInBB == null ? 0 : lastInBB.Votes,
                    SharedIp = false,
                    IsDbConsist = state == Lyra.Data.API.BlockChainState.Almighty || state == Lyra.Data.API.BlockChainState.Engaging,
                    //SharedIp = hist.Last(x => x.nodeStatus.ContainsKey(acct))
                    //    .bb.NodeAddresses.Count(x => x.Value == hist.Last(x => x.nodeStatus.ContainsKey(acct))
                    //    .bb.NodeAddresses[acct]) > 1
                };

                statusList.Add(xs);
            }

            return statusList;
        }

        private bool CompareVersion(string s1, string s2)
        {
            // LYRA Block Lattice 2.1.0.0
            // ommit smallest one
            return s1.Substring(0, s1.Length - 3) == s2.Substring(0, s2.Length - 3);
        }
    }
}
