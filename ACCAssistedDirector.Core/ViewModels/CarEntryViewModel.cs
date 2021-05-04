using ACCAssistedDirector.Core.Services;
using Domain.Enums;
using Domain.Models;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;

namespace ACCAssistedDirector.Core.ViewModels {
    public class CarEntryViewModel : MvxViewModel {

        private readonly CarUpdateModel _carUpdate;

        #region Displayed properties
        private int _displayedPosition;
        public int DisplayedPosition
        {
            get { return _displayedPosition; }
            set { SetProperty(ref _displayedPosition, value); }
        }

        private string _driver;
        public string Driver
        {
            get { return _driver; }
            set { SetProperty(ref _driver, value); }
        }

        private int _raceNumber;
        public int RaceNumber
        {
            get { return _raceNumber; }
            set { SetProperty(ref _raceNumber, value); }
        }

        private float _previousGap;
        private string _gap;
        public string Gap
        {
            get { return _gap; }
            set { SetProperty(ref _gap, value); }
        }

        private int _laps;
        public int Laps
        {
            get { return _laps; }
            set { SetProperty(ref _laps, value); }
        }

        private CarLocationEnum _carLocation;
        public CarLocationEnum CarLocation
        {
            get { return _carLocation; }
            set { SetProperty(ref _carLocation, value); }
        }

        private int _previousDelta;
        private string _delta;
        public string Delta
        {
            get { return _delta; }
            set { SetProperty(ref _delta, value); }
        }

        private string _currentLap;
        public string CurrentLap
        {
            get { return _currentLap; }
            set { SetProperty(ref _currentLap, value); }
        }

        private string _lastLap;
        public string LastLap
        {
            get { return _lastLap; }
            set { SetProperty(ref _lastLap, value); }
        }

        private string _bestLap;
        public string BestLap
        {
            get { return _bestLap; }
            set { SetProperty(ref _bestLap, value); }
        }
        #endregion

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set { SetProperty(ref _selected, value); }
        }

        private bool _focused;
        public bool Focused
        {
            get { return _focused; }
            set { SetProperty(ref _focused, value); }
        }

        private bool _gapVisible;
        public bool GapVisible
        {
            get { return _gapVisible; }
            set { SetProperty(ref _gapVisible, value); }
        }
        
        public int CarIndex => _carUpdate.CarInfo.CarIndex;
        public int Position => _carUpdate.Position;
        public int TrackPosition => _carUpdate.TrackPosition;

        public IMvxCommand SelectionCommand { get; set; }

        private readonly Action<int> _onClickCallback;
        private int _currentDriverIndex = -1;

        public CarEntryViewModel(CarUpdateModel car, Action<int> onClickCallback) {
            _carUpdate = car;
            _onClickCallback = onClickCallback;
            SelectionCommand = new MvxCommand(OnSelected);

            RaceNumber = car.CarInfo.RaceNumber;

            var carEntryListService = Mvx.IoCProvider.Resolve<ICarEntryListService>();
            carEntryListService.OnCarEntryUpdated += UpdateCarEntry;
            carEntryListService.OnFocusedCarUpdated += FucusedCarUpdate;
        }

        public void UpdateCarEntry() {
            UpdateDriver();
            UpdateGap();
            UpdateLaps();
            UpdateLocation();
            UpdateDelta();
            UpdateTimings();
        }

        private void UpdateTimings() {
            CurrentLap = _carUpdate.CurrentLap?.LaptimeString;

            LastLap = _carUpdate.LastLap?.LaptimeString;

            BestLap = _carUpdate.BestSessionLap?.LaptimeString;
        }

        private void UpdateDelta() {
            if (_carUpdate.Delta != _previousDelta) {
                Delta = (_carUpdate.Delta < 0 ? "-" : "+") + $"{TimeSpan.FromMilliseconds(_carUpdate.Delta):ss\\.fff}";
                _previousDelta = _carUpdate.Delta;
            }
        }

        private void UpdateLocation() {
            if (_carUpdate.CarLocation != _carLocation) CarLocation = _carUpdate.CarLocation;
        }

        private void UpdateLaps() {
            if (_carUpdate.Laps != _laps) Laps = _carUpdate.Laps;
        }

        private void UpdateGap() {
            if (_carUpdate.GapFrontSeconds != _previousGap) {
                Gap = $"{_carUpdate.GapFrontSeconds:F1} s";
                _previousGap = _carUpdate.GapFrontSeconds;
            }
        }

        public void UpdateDisplayedPosition(bool displayTrackPosition) {

            if (displayTrackPosition) {
                if (_displayedPosition != _carUpdate.TrackPosition) DisplayedPosition = _carUpdate.TrackPosition;
            } else {
                if(_displayedPosition != _carUpdate.Position) DisplayedPosition = _carUpdate.Position;                  
            }
            GapVisible = DisplayedPosition > 1;
        }

        private void UpdateDriver() {
            if (_carUpdate.CarInfo.CurrentDriverIndex != _currentDriverIndex) {
                Driver = _carUpdate.CarInfo.Drivers[_carUpdate.CarInfo.CurrentDriverIndex].DisplayName;
                _currentDriverIndex = _carUpdate.CarInfo.CurrentDriverIndex;
            }
        }

        private void UpdateCarEntry(int carIndex) {
            if (_carUpdate.CarInfo.CarIndex == carIndex) UpdateCarEntry();
        }

        private void FucusedCarUpdate(bool autoDirectorChangedFocus) {
            if (_carUpdate.HasFocus) {
                Focused = true;
                Selected = false;
            } else {
                Focused = false;
                Selected = false;
            }
        }

        public void OnSelected() {
            Selected = true;
            _onClickCallback?.Invoke(CarIndex);          
        }
    }
}
