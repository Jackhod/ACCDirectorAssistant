using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.Services {
    public class CarEntryListService : GameUpdatesReceiver, ICarEntryListService {
        
        public List<CarUpdateModel> CarEntryList { get; set; }
        public DateTime LastFocusChange { get; private set; }

        private ITrackDataService _trackData;
        private IClientService _clientService;
        private int updateCount = 0;
        private int focusedCarIndex;
        private DateTime _lastEntrylistRequest = DateTime.Now;
        private Dictionary<int, bool> updateChecks;

        #region events
        public event EntryListUpdatedDelegate OnEntryListUpdated;
        public event CarEntryUpdatedDelegate OnCarEntryUpdated;
        public event LastCarUpdatedDelegate OnLastCarUpdated;
        public event FocusedCarUpdatedDelegate OnFocusedCarUpdated;
        public event RemovedCarFromEntrylistDelegate OnRemovedCarFromEntrylist;
        #endregion

        public CarEntryListService(IClientService clientService, ITrackDataService trackDataService) : base(clientService) {

            CarEntryList = new List<CarUpdateModel>();
            updateChecks = new Dictionary<int, bool>();
            _trackData = trackDataService;
            _clientService = clientService;

            _clientService.MessageHandler.OnSetFocus += SetFocusedCar;
        }

        public void CancelService() {
            UnsubscribeFromGameUpdates();
            _clientService.MessageHandler.OnSetFocus -= SetFocusedCar;
            CarEntryList.Clear();
            CarEntryList = null;
            updateChecks.Clear();
            updateChecks = null;
        }
       
        protected override void OnEntrylistReceived(string sender, IEnumerable<ushort> carIds) {

            System.Diagnostics.Debug.WriteLine("entrylist received carentry");

            foreach (var car in CarEntryList)  OnRemovedCarFromEntrylist?.Invoke(car.CarInfo.CarIndex);

            CarEntryList.Clear();
            updateCount = 0;
            foreach(var id in carIds) {
                if (GetCarById(id) == null) {
                    System.Diagnostics.Debug.WriteLine("added " + id);
                    var carInfo = new CarModel(id);
                    CarEntryList.Add(new CarUpdateModel(carInfo));
                    if (!updateChecks.ContainsKey(id)) updateChecks.Add(id, true);
                }
            }           
        }

        protected override void OnEntryListUpdate(string sender, CarInfo carUpdate, IEnumerable<DriverInfo> drivers) {

            CarUpdateModel carEntry = CarEntryList.SingleOrDefault(x => x.CarInfo.CarIndex == carUpdate.CarIndex);
            if (carEntry == null) {
                CarModel carInfo = new CarModel(carUpdate.CarIndex);
                carEntry = new CarUpdateModel(carInfo);
                CarEntryList.Add(carEntry);
                if (!updateChecks.ContainsKey(carUpdate.CarIndex)) updateChecks.Add(carUpdate.CarIndex, true);
            }
            carEntry.CarInfo.Update(carUpdate);
            
            foreach(var d in drivers) {
                carEntry.CarInfo.AddDriver(d);
            }

            OnEntryListUpdated?.Invoke(carEntry);
        }

        protected override void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate carUpdate) {

            var carEntry = CarEntryList.FirstOrDefault(x => x.CarInfo.CarIndex == carUpdate.CarIndex);
            if (carEntry == null || carEntry.CarInfo.Drivers.Count != carUpdate.DriverCount) {
                if ((DateTime.Now - _lastEntrylistRequest).TotalSeconds > 1) {
                    _lastEntrylistRequest = DateTime.Now;
                    _clientService.MessageHandler.RequestEntryList();
                    System.Diagnostics.Debug.WriteLine($"CarUpdate {carUpdate.CarIndex}|{carUpdate.DriverIndex} not know, will ask for new EntryList");                  
                }
                return;
            }

            carEntry.Update(carUpdate, _trackData.TrackDataModel);
            OnCarEntryUpdated?.Invoke(carEntry.CarInfo.CarIndex);
            updateChecks[carUpdate.CarIndex] = true;

            updateCount++;
            if(updateCount == CarEntryList.Count) {
                EvaluateTrackPositions();
                UpdateGaps();
                CountCarsAround();
                foreach (var car in CarEntryList) OnCarEntryUpdated?.Invoke(car.CarInfo.CarIndex);
                RemoveOldCars();
                OnLastCarUpdated?.Invoke(); //all cars have been updated
                updateCount = 0;
            }
        }

        protected override void OnRealtimeUpdate(string sender, RealtimeUpdate update) {

            if (CarEntryList == null) return;

            foreach (var car in CarEntryList) { 
                if (car.CarInfo.CarIndex == update.FocusedCarIndex) {
                    if (!car.HasFocus) {
                        SetFocusedCar(update.FocusedCarIndex);
                    }
                    return;
                }
            }
        }

        public CarUpdateModel GetFocusedCar() {
            return CarEntryList.FirstOrDefault(c => c.CarInfo.CarIndex == focusedCarIndex);
        }

        public CarUpdateModel GetCarById(int carId) {
            return CarEntryList.FirstOrDefault(c => c.CarInfo.CarIndex == carId);
        }

        private void SetFocusedCar(int carId, bool isAutoDirector = false) {
            foreach (var car in CarEntryList) {
                if (car.CarInfo.CarIndex == carId) car.HasFocus = true;
                else car.HasFocus = false;
            }
            UpdateLastFocusChange();
            OnFocusedCarUpdated?.Invoke(isAutoDirector);           
        }

        private void EvaluateTrackPositions() {
            var sortedCars = CarEntryList.ToList(); //shallow copy of the car list 
            sortedCars.Sort((c1, c2) => 2 * c2.Laps.CompareTo(c1.Laps) + c2.SplinePosition.CompareTo(c1.SplinePosition));
            for (int i = 0; i < sortedCars.Count; i++) {
                sortedCars[i].TrackPosition = i + 1;
            }
        }

        private void UpdateGaps() {
            try {
                if (_trackData.TrackDataModel != null && _trackData.TrackDataModel.TrackMeters > 0) {                   
                    var sortedCars = CarEntryList.OrderBy(car => car.TrackPosition).ToList();

                    if (sortedCars.Count() > 1) {
                        for (int i = 1; i < sortedCars.Count; i++) {
                            var carAhead = sortedCars[i - 1];
                            var carBehind = sortedCars[i];

                            var distance = CalcMetersDistance(carAhead, carBehind, _trackData.TrackDataModel.TrackMeters);
                            carBehind.GapFrontMeters = distance;
                            carAhead.GapRearMeters = distance;

                            var carAheadKmh = carAhead.Kmh;
                            var carBehindKmh = carBehind.Kmh;
                            var combinedSpeedMS = (carAheadKmh + carBehindKmh) / 2f / 3.6f;
                            if (combinedSpeedMS > 0.0001f) {
                                carBehind.GapFrontSeconds = distance / combinedSpeedMS;
                                carAhead.GapRearSeconds = distance / combinedSpeedMS;
                                
                            } else {
                                carBehind.GapFrontSeconds = 999;
                                carAhead.GapRearSeconds = 999;
                            }
                        }

                        // then the first and last cars
                        var distance2 = CalcMetersDistance(sortedCars.First(), sortedCars.Last(), _trackData.TrackDataModel.TrackMeters);
                        sortedCars.First().GapFrontMeters = distance2;
                        sortedCars.Last().GapRearMeters = distance2;
              
                    } else {
                        foreach (var item in sortedCars) {
                            item.GapFrontMeters = _trackData.TrackDataModel.TrackMeters;
                            item.GapRearMeters = _trackData.TrackDataModel.TrackMeters;
                        }
                    }
                }

            } catch (Exception ex) {
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void CountCarsAround() {
            var trackMeters = _trackData.TrackDataModel.TrackMeters;
            foreach (var car in CarEntryList) {
                car.CarsAroundMe30m = CarEntryList.Where(c => Math.Abs(c.SplinePosition - car.SplinePosition) * trackMeters < 30 && c != car).Count();
                car.CarsAroundMe10m = CarEntryList.Where(c => Math.Abs(c.SplinePosition - car.SplinePosition) * trackMeters < 10 && c != car).Count();
            }
        }

        private void UpdateLastFocusChange() {
            foreach (var car in CarEntryList) {
                if (car.HasFocus && focusedCarIndex != car.CarInfo.CarIndex) {
                    focusedCarIndex = car.CarInfo.CarIndex;
                    LastFocusChange = DateTime.Now;
                }
            }
        }

        private float CalcMetersDistance(CarUpdateModel carAhead, CarUpdateModel carBehind, float trackMeters) {
            var cAheadSplinePos = carAhead.SplinePosition;
            var cBehingSplinePos = carBehind.SplinePosition;           
            if (cAheadSplinePos < cBehingSplinePos) cAheadSplinePos += 1f; //when the car ahead is at the beginning of the lap and the car behind is at the end of the lap
            var splineDistance = cAheadSplinePos - cBehingSplinePos;

            return splineDistance * trackMeters;
        }

        private void RemoveOldCars() {
            foreach(var carCheck in updateChecks.ToList()) {
                if (!carCheck.Value) { // the car has not been updated so it can be removed
                    CarEntryList.Remove(GetCarById(carCheck.Key));
                    OnRemovedCarFromEntrylist?.Invoke(carCheck.Key);
                }
                updateChecks[carCheck.Key] = false;
            }
        }
    }
}
