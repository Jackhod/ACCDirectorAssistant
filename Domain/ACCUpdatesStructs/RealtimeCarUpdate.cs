using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ACCUpdatesStructs {
    public struct RealtimeCarUpdate
    {
        public int CarIndex { get; set; }
        public int DriverIndex { get; set; }
        public int Gear { get; set; }
        public float WorldPosX { get; set; }
        public float WorldPosY { get; set; }
        public float Yaw { get; set; }
        public CarLocationEnum CarLocation { get; set; }
        public int Kmh { get; set; }
        public int Position { get; set; }
        public int TrackPosition { get; set; }
        public float SplinePosition { get; set; }
        public int Delta { get; set; }
        public LapInfo BestSessionLap { get; set; }
        public LapInfo LastLap { get; set; }
        public LapInfo CurrentLap { get; set; }
        public int Laps { get; set; }
        public ushort CupPosition { get; set; }
        public byte DriverCount { get; set; }
    }
}
