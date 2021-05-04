using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using Domain.Models;
using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace ACCAssistedDirector.Core.ViewModels {
    public class CameraPanelViewModel : MvxViewModel {


        //the camera sets that the game does not let the user select the camera for 
        private MvxObservableCollection<CameraSelectorViewModel> _upperCameraSetSelectors = new MvxObservableCollection<CameraSelectorViewModel>();
        public MvxObservableCollection<CameraSelectorViewModel> UpperCameraSetSelectors
        {
            get { return _upperCameraSetSelectors; }
            set
            {
                SetProperty(ref _upperCameraSetSelectors, value);
                RaisePropertyChanged(() => UpperCameraSetSelectors);
            }
        }
        private List<string> upperCameraSetSelectorsNames = new List<string>() { "Helicam", "set1", "set2", "pitlane" };

        //the other camera sets
        private MvxObservableCollection<CameraSetViewModel> _bottomCameraSetSelectors = new MvxObservableCollection<CameraSetViewModel>();
        public MvxObservableCollection<CameraSetViewModel> BottomCameraSetSelectors
        {
            get { return _bottomCameraSetSelectors; }
            set
            {
                SetProperty(ref _bottomCameraSetSelectors, value);
                RaisePropertyChanged(() => BottomCameraSetSelectors);
            }
        }

        private readonly IClientService _clientService;
        private readonly ICameraService _cameraService;

        public CameraPanelViewModel (IClientService clientService, ICameraService cameraService) {
            _cameraService = cameraService;
            _clientService = clientService;
            cameraService.OnCamsReceived += AddCams;
            cameraService.OnActiveCamUpdated += CameraSelection;
        }

        private void AddCams() {
            var camSets = _cameraService.CameraSets;

            foreach(var camSet in camSets) {
                //initializing the upper camera selectors
                if (upperCameraSetSelectorsNames.Find(n => n.Equals(camSet.Key)) != null) {
                    var upperCamSel = new CameraSelectorViewModel(camSet.Value[0], camSet.Key, RequestCameraChange); //just set the camera of the selector to the first available because it doesn't matter
                    UpperCameraSetSelectors.Add(upperCamSel); 
                }
                //creating all other selectors
                else {
                    var bottomCameraSet = BottomCameraSetSelectors.SingleOrDefault(x => x.CameraSetName.Equals(camSet.Key));
                    if (bottomCameraSet == null) {
                        BottomCameraSetSelectors.Add(new CameraSetViewModel(camSet.Key, camSet.Value, RequestCameraChange));
                    }
                }
            }
        }       

        private void CameraSelection(CameraModel activeCam, bool autoDirectorChangedCamera) {
            foreach (var camSetSelector in _upperCameraSetSelectors) camSetSelector.CheckIfSelected(activeCam.CameraSetName);
            foreach (var camSet in _bottomCameraSetSelectors) camSet.CameraSelection(activeCam.CameraSetName, activeCam.CameraName);
        }

        private void RequestCameraChange(string requestCameraSet, string requestCamera) {
            _clientService.MessageHandler.SetCamera(requestCameraSet, requestCamera);
        }
    }
}
