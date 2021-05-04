using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ACCAssistedDirector.Core.ViewModels {
    public class HUDSelectorViewModel : MvxViewModel {

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

        public Action<string> RequestHudPageCallback { get; }

        public IMvxCommand SelectionCommand { get; set; }

        public HUDSelectorViewModel(string label, Action<string> requestHudPageCallback) {
            _label = label;
            SelectionCommand = new MvxCommand(OnSelected);
            RequestHudPageCallback = requestHudPageCallback;
        }

        private void OnSelected() {
            RequestHudPageCallback?.Invoke(_label);
            Selected = true;
        }

        public void HudPageSelection(string activeHudPage) {
            if (_label.Equals(activeHudPage)) {
                Selected = true;
            } else {
                Selected = false;
            }
        }
    }
}
