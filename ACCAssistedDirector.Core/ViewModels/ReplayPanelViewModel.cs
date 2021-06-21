using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using Domain.Models;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.ViewModels {
    public class ReplayPanelViewModel : MvxViewModel {

        private MvxObservableCollection<RaceEventViewModel> _raceEvents = new MvxObservableCollection<RaceEventViewModel>();
        public MvxObservableCollection<RaceEventViewModel> RaceEvents
        {
            get { return _raceEvents; }
            set
            {
                SetProperty(ref _raceEvents, value);
                RaisePropertyChanged(() => RaceEvents);
            }
        }

        private MvxObservableCollection<ComboBoxItemViewModel<CarUpdateModel>> _pilots;
        public MvxObservableCollection<ComboBoxItemViewModel<CarUpdateModel>> Pilots
        {
            get { return _pilots; }
            set { SetProperty(ref _pilots, value); }
        }

        private MvxObservableCollection<int> _replayDurations = new MvxObservableCollection<int> { 10, 20, 30 };
        public MvxObservableCollection<int> ReplayDurations
        {
            get { return _replayDurations; }
            set { SetProperty(ref _replayDurations, value); }
        }

        private MvxObservableCollection<int> _replayStartTimes = new MvxObservableCollection<int> { -10, -20, -30, -40, -50, -60, -70, -80, -90, -100, -110, -120 };
        public MvxObservableCollection<int> ReplayStartTimes
        {
            get { return _replayStartTimes; }
            set { SetProperty(ref _replayStartTimes, value); }
        }

        private ComboBoxItemViewModel<CarUpdateModel> _selectedReplayPilot;
        public ComboBoxItemViewModel<CarUpdateModel> SelectedReplayPilot
        {
            get { return _selectedReplayPilot; }
            set { SetProperty(ref _selectedReplayPilot, value); }
        }

        private int _selectedDuration;   
        public int SelectedDuration
        {
            get { return _selectedDuration; }
            set { SetProperty(ref _selectedDuration, value); }
        }

        private int _selectedStartTime;
        public int SelectedStartTime
        {
            get { return _selectedStartTime; }
            set { SetProperty(ref _selectedStartTime, value); }
        }


        public IMvxCommand QuickReplayCommand { get; set; }

        private IReplayService _replayService;
        private IClientService _clientService;
        private ICarEntryListService _carEntryListService;

        public ReplayPanelViewModel(IReplayService replayService, IClientService clientService, ICarEntryListService carEntryListService) {
            _clientService = clientService;
            _replayService = replayService;
            _carEntryListService = carEntryListService;

            _replayService.OnEventAdded += OnEventAdded;
            _replayService.OnEventRemoved += OnEventRemoved;
            _carEntryListService.OnLastCarUpdated += UpdatePilotList;

            QuickReplayCommand = new MvxCommand(QuickReplay);
        }     

        public void PrepareToClose() {
            _raceEvents.Clear();
            _pilots.Clear();
            _replayDurations.Clear();
            _replayStartTimes.Clear();
            _raceEvents = null;
            _pilots = null;
            _replayDurations = null;
            _replayStartTimes = null;

            _replayService.OnEventAdded -= OnEventAdded;
            _replayService.OnEventRemoved -= OnEventRemoved;
            _carEntryListService.OnLastCarUpdated -= UpdatePilotList;

            _replayService.CancelService();
        }

        private void OnEventAdded(BroadcastingEventModel evnt) {
            RaceEvents.Insert(0, new RaceEventViewModel(evnt, OnHighlightReplay, _replayService));
            RaisePropertyChanged(() => RaceEvents);
        }

        private void OnEventRemoved(BroadcastingEventModel evnt) {
            RaceEvents.Remove(RaceEvents.FirstOrDefault(re => re.RaceEvent == evnt));
            RaisePropertyChanged(() => RaceEvents);
        }

        private void OnHighlightReplay(BroadcastingEventModel raceEvent, int startTimeSeconds, int durationSeconds) {          
            _replayService.PlayHighlightReplay(durationSeconds, startTimeSeconds, raceEvent.CarId);
        }

        private void QuickReplay() {
            System.Diagnostics.Debug.WriteLine(Convert.ToInt32(SelectedReplayPilot.Item.CarInfo.CarIndex));
            _replayService.PlayQuickReplay(SelectedDuration, SelectedStartTime, Convert.ToInt32(SelectedReplayPilot.Item.CarInfo.CarIndex));
        }

        private void UpdatePilotList() {
            if (Pilots == null) Pilots = new MvxObservableCollection<ComboBoxItemViewModel<CarUpdateModel>>();

            //Adding drivers to the combo box
            foreach (var car in _carEntryListService.CarEntryList) {
                var pilot = Pilots.FirstOrDefault(c => c.Item.CarInfo.CarIndex == car.CarInfo.CarIndex && c.Item.CarInfo.RaceNumber == car.CarInfo.RaceNumber);
                if (pilot != null) pilot.DisplayName = car.CarInfo.DriverName;
                else Pilots.Add(new ComboBoxItemViewModel<CarUpdateModel>(car, car.CarInfo.DriverName));
            }

            //Removing old pilots
            foreach (var p in _pilots) {
                var cars = _carEntryListService.CarEntryList;
                var pilot = cars.FirstOrDefault(c => c.CarInfo.CarIndex == p.Item.CarInfo.CarIndex && c.CarInfo.RaceNumber == p.Item.CarInfo.RaceNumber);
                if (pilot == null) Pilots.Remove(p);
            }

            RaisePropertyChanged(() => Pilots);
        }
    }
}
