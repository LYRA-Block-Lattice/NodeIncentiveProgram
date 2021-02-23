using System;
using System.Collections.Generic;
using System.Text;

namespace NodeIncentiveProgram
{
    public class XStatus
    {
        public string NetworkId { get; set; }
        public string AccountId { get; set; }

        public int OfflineCount { get; set; }
        public bool FullyUpgraded { get; set; }
        public decimal PosVotes { get; set; }
        public bool IsPrimary { get; set; }
        public bool SharedIp { get; set; }
        public bool IsDbConsist { get; set; }

        public bool SuccessPaid { get; set; }
        public decimal PaidAmount { get; set; }

        public decimal GetRito()
        {
            var rito = 1m;

            if (NetworkId == "testnet")
            {
                rito *= 1.875m;
                return rito;
            }                

            for (var i = 0; i < OfflineCount; i++)
                rito *= 0.7m;

            if (!FullyUpgraded)
                rito *= 0.5m;

            if (IsPrimary)
                rito *= 1.3m;

            if (SharedIp)
                rito *= 0.1m;

            if (!IsDbConsist)
                rito *= 0.1m;

            if (PosVotes > 1000000)
                rito *= 2m;
            else if (PosVotes < 100000)
                rito *= 1.05m;
            else
                rito *= 1.3m;

            return rito > 0m ? rito : 0m;
        }
    }
}
