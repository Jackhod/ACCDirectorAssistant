using ACCAssistedDirector.Core.Assistant;
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
    public class DirectorTipViewModel : MvxViewModel {

        public DirectorTipModel DirectorTip { get; set; }

        private int _carNumber;
        public int CarNumber
        {
            get { return _carNumber; }
            set { SetProperty(ref _carNumber, value); }
        }

        private string _buttonCarLabel;
        public string ButtonCarLabel
        {
            get { return _buttonCarLabel; }
            set { SetProperty(ref _buttonCarLabel, value); }
        }

        private string _buttonCamLabel;
        public string ButtonCamLabel
        {
            get { return _buttonCamLabel; }
            set { SetProperty(ref _buttonCamLabel, value); }
        }

        public IMvxCommand SelectionCarCommand { get; set; }
        public IMvxCommand SelectionCamCommand { get; set; }
        public IMvxCommand RemoveCommand { get; set; }

        private IClientService _clientService;
        private CarEntryListViewModel _carEntryListVM;
        private Action<DirectorTipViewModel> _removeTipDelegate;

        public DirectorTipViewModel(DirectorTipModel directorTip, IClientService clientService, Action<DirectorTipViewModel> removeTipDelegate, CarEntryListViewModel carEntryListVM) {
            DirectorTip = directorTip;
            _clientService = clientService;
            _removeTipDelegate = removeTipDelegate;
            _carEntryListVM = carEntryListVM;
            RemoveCommand = new MvxCommand(RemoveTip);
            InitializeTip();
        }

        private void SelectCarTip() {
            var carTip = DirectorTip.CarTip.Tip;

            if(carTip != null) {
                var carIndex = carTip.CarInfo.CarIndex;
                var carEntry = _carEntryListVM.Cars.FirstOrDefault(c => c.CarIndex == carIndex);
                carEntry.Selected = true;
                System.Diagnostics.Trace.WriteLine("cartip");
                _clientService.MessageHandler.SetFocus(carTip.CarInfo.CarIndex, _carEntryListVM.InstantFocus);
            }
        }

        private void SelectCamTip() {

            var carTip = DirectorTip.CarTip.Tip;
            var camTip = DirectorTip.CamTips[0].Tip;

            if (camTip != null) {
                _clientService.MessageHandler.SetFocusAndCamera(carTip.CarInfo.CarIndex, camTip.CameraSetName, camTip.CameraName);
            } else {
                _clientService.MessageHandler.SetFocus(carTip.CarInfo.CarIndex, true);
            }
        }

        private void RemoveTip() {
            _removeTipDelegate(this);
        }

        private void InitializeTip() {
            if (DirectorTip.CarTip == null) return;

            var carTip = DirectorTip.CarTip.Tip;
            var camTip = DirectorTip.CamTips[0].Tip;

            SelectionCarCommand = new MvxCommand(SelectCarTip);
            SelectionCamCommand = new MvxCommand(SelectCamTip);

            CarNumber = carTip.CarInfo.RaceNumber;
            ButtonCarLabel = carTip.CarInfo.Drivers[carTip.CarInfo.CurrentDriverIndex].FirstName.Substring(0, 1) + ". " + carTip.CarInfo.Drivers[carTip.CarInfo.CurrentDriverIndex].LastName;

            if (camTip != null) ButtonCamLabel = camTip.CameraName;
            else ButtonCamLabel = "Select";
        }
    }
}
