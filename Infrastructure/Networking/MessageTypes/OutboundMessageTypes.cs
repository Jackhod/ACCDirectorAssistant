using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Networking.MessageTypes {
    public enum OutboundMessageTypes : byte {
        REGISTER_COMMAND_APPLICATION = 1,
        UNREGISTER_COMMAND_APPLICATION = 9,

        REQUEST_ENTRY_LIST = 10,
        REQUEST_TRACK_DATA = 11,

        CHANGE_HUD_PAGE = 49,
        CHANGE_FOCUS = 50,
        INSTANT_REPLAY_REQUEST = 51,

        PLAY_MANUAL_REPLAY_HIGHLIGHT = 52, // TODO, but planned
        SAVE_MANUAL_REPLAY_HIGHLIGHT = 60  // TODO, but planned: saving manual replays gives distributed clients the possibility to see the play the same replay
    }
}
