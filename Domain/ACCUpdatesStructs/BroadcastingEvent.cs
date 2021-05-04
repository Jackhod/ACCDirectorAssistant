using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ACCUpdatesStructs {
    public struct BroadcastingEvent
    {
        public BroadcastingCarEventType Type { get; set; }
        public string Msg { get; set; }
        public int TimeMs { get; set; }
        public int CarId { get; set; }
        public CarInfo CarData { get; set; }
    }
}
