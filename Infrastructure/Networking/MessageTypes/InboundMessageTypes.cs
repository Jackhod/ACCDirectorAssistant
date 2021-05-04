using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Networking.MessageTypes {
    public enum InboundMessageTypes : byte {
        REGISTRATION_RESULT = 1,
        REALTIME_UPDATE = 2,
        REALTIME_CAR_UPDATE = 3,
        ENTRY_LIST = 4,
        ENTRY_LIST_CAR = 6,
        TRACK_DATA = 5,
        BROADCASTING_EVENT = 7
    }
}
