using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ACCUpdatesStructs {
    public struct TrackData
    {
        public string TrackName { get; set; }
        public int TrackId { get; set; }
        public float TrackMeters { get; set; }
        public Dictionary<string, List<string>> CameraSets { get; set; }
        public IEnumerable<string> HUDPages { get; set; }
    }
}
