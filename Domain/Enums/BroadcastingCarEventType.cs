using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums {
    public enum BroadcastingCarEventType {
        None = 0,
        GreenFlag = 1,
        SessionOver = 2,
        PenaltyCommMsg = 3,
        Accident = 4,
        LapCompleted = 5,
        BestSessionLap = 6,
        BestPersonalLap = 7
    };
}
