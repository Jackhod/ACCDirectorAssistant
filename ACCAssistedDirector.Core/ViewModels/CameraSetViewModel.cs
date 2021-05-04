using Domain.Models;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.ViewModels {
    public class CameraSetViewModel : MvxViewModel {

        private MvxObservableCollection<CameraSelectorViewModel> _cameraSetSelectors = new MvxObservableCollection<CameraSelectorViewModel>();
        public MvxObservableCollection<CameraSelectorViewModel> CameraSelectors
        {
            get { return _cameraSetSelectors; }
            set
            {
                SetProperty(ref _cameraSetSelectors, value);
                RaisePropertyChanged(() => CameraSelectors);
            }
        }

        private string _cameraSetDisplayName;
        public string CameraSetDisplayName
        {
            get { return _cameraSetDisplayName; }
            set { SetProperty(ref _cameraSetDisplayName, value); }
        }

        public string CameraSetName { get; private set; }

        public CameraSetViewModel(string setName, List<CameraModel> cams, Action<string, string> selectionCamCallback) {
            CameraSetName = setName;
            CameraSetDisplayName = setName.ToUpper();

            foreach(var cam in cams) {
                var camSelector = CameraSelectors.SingleOrDefault(x => x.Camera.Equals(cam.CameraName));
                if(camSelector == null) {
                    camSelector = new CameraSelectorViewModel(cam, cam.CameraName, selectionCamCallback);
                    CameraSelectors.Add(camSelector);
                }
            }
        }

        public void CameraSelection(string cameraSet, string camera) {
            foreach (var camSelector in _cameraSetSelectors) camSelector.CheckIfSelected(cameraSet, camera);
        }
    }
}
