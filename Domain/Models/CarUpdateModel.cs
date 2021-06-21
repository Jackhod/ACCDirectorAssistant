using Domain.ACCUpdatesStructs;
using Domain.Enums;
using System;

namespace Domain.Models {
    public class CarUpdateModel {
        public CarModel CarInfo { get; private set; }
        public int Gear { get; private set; }
        public float WorldPosX { get; private set; }
        public float WorldPosY { get; private set; }
        public float Yaw { get; private set; }
        public CarLocationEnum CarLocation { get; private set; }
        public int Kmh { get; private set; }
        public int Position { get; private set; }
        public int TrackPosition { get; set; }
        public float SplinePosition { get; private set; }
        public int Delta { get; private set; }
        public string DeltaString { get; private set; }
        public LapModel BestSessionLap { get; private set; }
        public LapModel LastLap { get; private set; }
        public LapModel CurrentLap { get; private set; }
        public int Laps { get; private set; }
        public ushort CupPosition { get; private set; }
        public string LocationHint { get; private set; }

        //info for auto director
        public int SessionPersonalBestLap { get; private set; }
        public float Pressure { get; set; }
        public bool CrossedTheLineWithFocus { get; private set; }
        public float GapFrontMeters { get; set; }
        public float GapRearMeters { get; set; }
        public float ClosestDistance { get; set; }
        public float GapFrontSeconds { get; set; }
        public float GapRearSeconds { get; set; }
        public bool HasFocus { get; set; }
        //public List<CarFeature> Features { get; } = new List<CarFeature>();
        public int PredictedLaptime { get; private set; }
        public int SessionBestLap { get; set; }
        public int CarsAroundMe30m { get; set; }
        public int CarsAroundMe10m { get; set; }

        const float OFFSET = 0.001f;

        public CarUpdateModel(CarModel carInfo) {
            CarInfo = carInfo;
        }

        public void Update(RealtimeCarUpdate carUpdate, TrackDataModel trackData) {

            CarLocation = carUpdate.CarLocation;
            Delta = carUpdate.Delta;
            DeltaString = $"{TimeSpan.FromMilliseconds(Delta):ss\\.f}";

            Gear = carUpdate.Gear;
            Kmh = carUpdate.Kmh;
            Position = carUpdate.Position;
            CupPosition = carUpdate.CupPosition;
            SplinePosition = carUpdate.SplinePosition;
            WorldPosX = carUpdate.WorldPosX;
            WorldPosY = carUpdate.WorldPosY;
            Yaw = carUpdate.Yaw;
            Laps = carUpdate.Laps;

            CarInfo.UpdateDriverIndex(carUpdate.DriverIndex); 

            if (BestSessionLap == null && carUpdate.BestSessionLap != null)
                BestSessionLap = new LapModel();
            if (carUpdate.BestSessionLap != null)
                BestSessionLap.Update(carUpdate.BestSessionLap);

            if (LastLap == null && carUpdate.LastLap != null)
                LastLap = new LapModel();
            if (carUpdate.LastLap != null)
                LastLap.Update(carUpdate.LastLap);

            if (CurrentLap == null && carUpdate.CurrentLap != null)
                CurrentLap = new LapModel();
            if (carUpdate.CurrentLap != null)
                CurrentLap.Update(carUpdate.CurrentLap);

            if (CarLocation == CarLocationEnum.PitEntry)
                LocationHint = "IN";
            else if (CarLocation == CarLocationEnum.Pitlane)
                LocationHint = "PIT";
            else if (CarLocation == CarLocationEnum.PitExit)
                LocationHint = "OUT";
            else
                LocationHint = CurrentLap?.LapHint;

            int carUpdateIndex = carUpdate.CarIndex;

            if (carUpdateIndex != CarInfo.CarIndex) {
                System.Diagnostics.Debug.WriteLine($"Wrong {nameof(RealtimeCarUpdate)}.CarIndex {carUpdateIndex} for {nameof(CarUpdateModel)}.CarIndex {CarInfo.CarIndex}");
                return;
            }

            CrossedTheLineWithFocus = false;
            if (trackData != null) {
                if (carUpdate.SplinePosition * trackData.TrackMeters < 100 && HasFocus)
                    CrossedTheLineWithFocus = true;
                if (CrossedTheLineWithFocus && carUpdate.SplinePosition * trackData.TrackMeters > 100)
                    CrossedTheLineWithFocus = false;
            }

            SessionPersonalBestLap = carUpdate.BestSessionLap.LaptimeMS ?? -1;
            if (SessionPersonalBestLap > 0 && (SessionPersonalBestLap < SessionBestLap || SessionBestLap <= 0))
                SessionBestLap = SessionPersonalBestLap;

            if (SessionBestLap > 0)
                PredictedLaptime = SessionBestLap + carUpdate.Delta;
        }

        internal void SetFocused(int focusedCarIndex) {
            if (CarInfo.CarIndex == focusedCarIndex) {
                HasFocus = true;
            } else {
                HasFocus = false;
            }
        }
    }
}
