using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services.Interfaces;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.ViewModels {
    public class HUDPanelViewModel : MvxViewModel {

        private MvxObservableCollection<HUDSelectorViewModel> _hudPages = new MvxObservableCollection<HUDSelectorViewModel>();
        public MvxObservableCollection<HUDSelectorViewModel> HUDPages
        {
            get { return _hudPages; }
            set
            {
                SetProperty(ref _hudPages, value);
                RaisePropertyChanged(() => HUDPages);
            }
        }

        private IClientService _clientService;
        private IHUDService _HUDService;

        public HUDPanelViewModel(IClientService clientService, IHUDService HUDService) {
            _clientService = clientService;
            _HUDService = HUDService;

            _HUDService.OnHUDPageReceived += OnHUDPageReceived;
            _HUDService.OnActiveHUDPageUpdated += OnActiveHUDPageUpdated;
        }

        public void PrepareToClose() {
            _hudPages.Clear();
            _hudPages = null;
            _HUDService.OnHUDPageReceived -= OnHUDPageReceived;
            _HUDService.OnActiveHUDPageUpdated -= OnActiveHUDPageUpdated;
            _HUDService.CancelService();
        }

        private void OnHUDPageReceived(string HUDPage) {
            var hudPage = new HUDSelectorViewModel(HUDPage, RequestHUDPageChange);
            _hudPages.Add(hudPage);
        }

        private void OnActiveHUDPageUpdated(string activeHUD) {
            foreach (var hudPage in _hudPages) hudPage.HudPageSelection(activeHUD);
        }

        private void RequestHUDPageChange(string requestedHudPage) {
            _clientService.MessageHandler.SetHudPage(requestedHudPage);
        }
    }
}
