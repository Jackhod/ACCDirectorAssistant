using Domain.Models;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;

namespace ACCAssistedDirector.Core.ViewModels {
    public class CameraSelectorViewModel : MvxViewModel {

        private CameraModel _cameraModel;
        public CameraModel CameraModel
        {
            get { return _cameraModel; }
            set { SetProperty(ref _cameraModel, value); }
        }

        private string _label;
        public string Label
        {
            get { return _label; }
            set { SetProperty(ref _label, value); }
        }

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set { SetProperty(ref _selected, value); }
        }

        public string Camera { get; set; }
        public string CameraSet { get; set; }       
        public IMvxCommand SelectionCommand { get; set; }

        private readonly Action<string, string> _onClickCallback;

        public CameraSelectorViewModel(CameraModel cameraModel, string label, Action<string, string> onClickCallback) {
            _cameraModel = cameraModel;
            Label = label;
            _onClickCallback = onClickCallback;
            Camera = cameraModel.CameraName;
            CameraSet = cameraModel.CameraSetName;
            SelectionCommand = new MvxCommand(OnSelected);
        }

        private void OnSelected() {
            _onClickCallback?.Invoke(CameraSet, Camera);
            Selected = true;
        }

        public void CheckIfSelected(string cameraSet) {
            if (CameraSet != null && CameraSet.Equals(cameraSet)) {
                Selected = true;
            } else {
                Selected = false;
            }
        }

        public void CheckIfSelected (string cameraSet, string camera) {
            if (Camera!= null & CameraSet != null && Camera.Equals(camera) && CameraSet.Equals(cameraSet)) {
                Selected = true;
            } else {
                Selected = false;
            }
        }
    }
}
