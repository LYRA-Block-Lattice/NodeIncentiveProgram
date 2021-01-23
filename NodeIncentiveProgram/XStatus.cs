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
    }
}
