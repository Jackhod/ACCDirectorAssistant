using Domain.ACCUpdatesStructs;

namespace Domain.Models {
    public class BroadcastingEventModel {
        public string Type { get; internal set; }
        public string Msg { get; internal set; }
        public int TimeMs { get; internal set; }
        public int CarId { get; internal set; }
        public CarModel CarData { get; internal set; }

        public BroadcastingEventModel(BroadcastingEvent broadcastingEvent, CarModel carModel) {
            Type = broadcastingEvent.Type.ToString();
            Msg = broadcastingEvent.Msg;
            TimeMs = broadcastingEvent.TimeMs;
            CarId = broadcastingEvent.CarId;
            CarData = carModel;
        }

        public BroadcastingEventModel(string type, string msg, int timeMs, int carId, CarModel carData) {
            Type = type;
            Msg = msg;
            TimeMs = timeMs;
            CarId = carId;
            CarData = carData;
        }
    }
}
