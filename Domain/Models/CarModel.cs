using Domain.ACCUpdatesStructs;
using Domain.Enums;
using System.Collections.Generic;

namespace Domain.Models {
    public class CarModel {
        public ushort CarIndex { get; }
        public byte CarModelType { get; private set; }
        public string TeamName { get; private set; }
        public int RaceNumber { get; private set; }
        public byte CupCategory { get; private set; }
        public int CurrentDriverIndex { get; private set; }
        public List<DriverModel> Drivers { get; } = new List<DriverModel>();
        public NationalityEnum Nationality { get; private set; }
        public string DriverName => Drivers[CurrentDriverIndex].DisplayName;

        public CarModel(ushort carIndex) {
            CarIndex = carIndex;
        }

        public void Update(CarInfo carInfo) {
            CarModelType = carInfo.CarModelType;
            TeamName = carInfo.TeamName;
            RaceNumber = carInfo.RaceNumber;
            CupCategory = carInfo.CupCategory;
            CurrentDriverIndex = carInfo.CurrentDriverIndex;
            Nationality = carInfo.Nationality;
        }

        public void UpdateDriverIndex(int driverIndex) {
            CurrentDriverIndex = driverIndex;
        }

        public void AddDriver(DriverInfo driverInfo) {
            Drivers.Add(new DriverModel(driverInfo));
        }
    }
}
