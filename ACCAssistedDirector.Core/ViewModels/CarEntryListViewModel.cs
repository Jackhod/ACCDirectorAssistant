using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using Domain.Models;
using MvvmCross.ViewModels;
using System.Linq;

namespace ACCAssistedDirector.Core.ViewModels {
    public class CarEntryListViewModel : MvxViewModel {

        private MvxObservableCollection<CarEntryViewModel> _cars = new MvxObservableCollection<CarEntryViewModel>();
        public MvxObservableCollection<CarEntryViewModel> Cars
        {
            get { return _cars; }
            set
            {
                SetProperty(ref _cars, value);
                RaisePropertyChanged(() => Cars);
            }
        }

        private bool _instantFocus;
        public bool InstantFocus
        {
            get { return _instantFocus; }
            set { SetProperty(ref _instantFocus, value); }
        }

        private bool _radioButtonSelection = true; //true: order by track position, false: order by position
        public bool RadioButtonSelection
        {
            get { return _radioButtonSelection; }
            set
            {
                SetProperty(ref _radioButtonSelection, value);
                SortEntries();
            }
        }

        private IClientService _clientService;
        private ICarEntryListService _carEntryListService;

        public delegate void CarEntryUpdateDelegate(CarUpdateModel carUpdate);
        public event CarEntryUpdateDelegate OnCarEntryUpdate;

        public CarEntryListViewModel(IClientService clientService, ICarEntryListService carEntryListService) {
            InstantFocus = true;
            _clientService = clientService;
            _carEntryListService = carEntryListService;

            carEntryListService.OnEntryListUpdated += EntryListUpdated;
            carEntryListService.OnLastCarUpdated += SortEntries;
        }

        private void EntryListUpdated(CarUpdateModel car) {
            CarEntryViewModel carEntry = _cars.SingleOrDefault(c => c.CarIndex == car.CarInfo.CarIndex);
            if (carEntry == null) {
                carEntry = new CarEntryViewModel(car, RequestDriverChange);
                _cars.Add(carEntry);
            }
            carEntry.UpdateCarEntry();
        }

        private void RequestDriverChange(int carIndex) {
            _clientService.MessageHandler.SetFocus(carIndex, InstantFocus);
        }

        private void SortEntries() {
            if (_radioButtonSelection) {
                SortByTrackPosition();
            } else {
                SortByPosition();
            }
        }

        private void SortByPosition() {

            for (int i = 0; i < _cars.Count; i++) {
                int index = _cars.IndexOf(_cars.FirstOrDefault(c => c.Position == i + 1));
                if (index != i && index >= 0) _cars.Move(index, i);
            }

            foreach (var c in _cars) c.UpdateDisplayedPosition(_radioButtonSelection);
        }

        private void SortByTrackPosition() {

            for (int i = 0; i < _cars.Count; i++) {
                int index = _cars.IndexOf(_cars.FirstOrDefault(c => c.TrackPosition == i + 1));
                if (index != i && index >= 0) _cars.Move(index, i);
            }

            foreach (var c in _cars) c.UpdateDisplayedPosition(_radioButtonSelection);
        }
    }
}
