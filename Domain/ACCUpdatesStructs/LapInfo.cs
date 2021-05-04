using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ACCUpdatesStructs {
    public class LapInfo
    {
        public int? LaptimeMS { get; set; }
        public List<int?> Splits { get; } = new List<int?>();
        public ushort CarIndex { get; set; }
        public ushort DriverIndex { get; set; }
        public bool IsInvalid { get; set; }
        public bool IsValidForBest { get; set; }
        public LapType Type { get; set; }

        public override string ToString()
        {
            return $"{LaptimeMS, 5}|{string.Join("|", Splits)}";
        }
    }
}
