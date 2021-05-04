using Domain.ACCUpdatesStructs;
using System.Collections.Generic;

namespace Domain.Models {
    public class TrackDataModel {
        public string TrackName { get; internal set; }
        public int TrackId { get; internal set; }
        public float TrackMeters { get; internal set; }
        public Dictionary<string, List<string>> CameraSets { get; internal set; }
        public IEnumerable<string> HUDPages { get; internal set; }

        public void Update(TrackData trackData) {
            TrackName = trackData.TrackName;
            TrackId = trackData.TrackId;
            TrackMeters = trackData.TrackMeters;
            CameraSets = trackData.CameraSets;
            HUDPages = trackData.HUDPages;
        }
    }
}
