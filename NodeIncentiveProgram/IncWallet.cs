using Lyra.Core.Accounts;
using Lyra.Core.API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NodeIncentiveProgram
{
    public class IncWallet
    {
        private string _network;
        private string _name;
        private string _password;

        private Wallet incWallet;
        public IncWallet(string network, string name, string password = null)
        {
            _network = network;
            _name = name;
            _password = password;
        }

        public async Task OpenAsync()
        {
            Console.WriteLine($"Opening wallet for {_network}");

            string lyrawalletfolder = Wallet.GetFullFolderName(_network, "wallets");
            var walletStore = new SecuredWalletStore(lyrawalletfolder);
            incWallet = Wallet.Open(walletStore, _name, _password);
            var client = LyraRestClient.Create(_network, "", "", "");
            var syncResult = await incWallet.SyncAsync(client);
            if (syncResult != Lyra.Core.Blocks.APIResultCodes.Success)
            {
                Console.WriteLine("Can't sync wallet!");
                return;
            }

            Console.WriteLine($"Incentive wallet {incWallet.AccountId} balance: {incWallet.BaseBalance}\n");

        }

        internal async Task<bool> PayAsync(string accountID, int amount)
        {
            var result = await incWallet.SendAsync(amount, accountID);
            return result.ResultCode == Lyra.Core.Blocks.APIResultCodes.Success;
        }
    }
}
