using System;
using System.Collections.Generic;
using System.Text;

namespace NodeIncentiveProgram
{
    public class IncPayment
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        
        public List<XStatus> MainnetNodes { get; set; }
        public List<XStatus> TestnetNodes { get; set; }
    }
}
