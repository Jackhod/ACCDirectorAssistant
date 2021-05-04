using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ACCUpdatesStructs {
    public struct RealtimeUpdate
    {
        public int EventIndex { get; set; }
        public int SessionIndex { get; set; }
        public SessionPhase Phase { get; set; }
        public TimeSpan SessionTime { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public TimeSpan TimeOfDay { get; set; }
        public float RainLevel { get; set; }
        public float Clouds { get; set; }
        public float Wetness { get; set; }
        public LapInfo BestSessionLap { get; set; }
        public ushort BestLapCarIndex { get; set; }
        public ushort BestLapDriverIndex { get; set; }
        public int FocusedCarIndex { get; set; }
        public string ActiveCameraSet { get; set; }
        public string ActiveCamera { get; set; }
        public bool IsReplayPlaying { get; set; }
        public float ReplaySessionTime { get; set; }
        public float ReplayRemainingTime { get; set; }
        public TimeSpan SessionRemainingTime { get; set; }
        public TimeSpan SessionEndTime { get; set; }
        public RaceSessionType SessionType { get; set; }
        public byte AmbientTemp { get; set; }
        public byte TrackTemp { get; set; }
        public string CurrentHudPage { get; set; }
    }
}
