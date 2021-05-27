using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services.Interfaces;
using Domain.ACCUpdatesStructs;
using System;
using System.Collections.Generic;

namespace ACCAssistedDirector.Core.Services {
    public class HUDService : GameUpdatesReceiver, IHUDService {

        public Dictionary<string, Boolean> HUDPages { get; set; } //hud page name, active/not active

        public event HUDPagesReceivedDelegate OnHUDPageReceived;
        public event ActiveHUDPageUpdateDelegate OnActiveHUDPageUpdated;

        private IClientService _clientService;
        private string currentHUDPage = null;

        public HUDService(IClientService clientService) : base(clientService) {
            HUDPages = new Dictionary<string, bool>();
            _clientService = clientService;
            _clientService.MessageHandler.OnSetHUDPage += OnSetHUDPage;
        }

        public void CancelService() {
            HUDPages.Clear();
            HUDPages = null;
            _clientService.MessageHandler.OnSetHUDPage -= OnSetHUDPage;
            UnsubscribeFromGameUpdates();
        }

        protected override void OnTrackDataUpdate(string sender, TrackData trackUpdate) {

            System.Diagnostics.Debug.WriteLine("ontrackdataupdate hudservice");

            //add pages that are not already in the dictionary
            foreach (var hudPage in trackUpdate.HUDPages) {
                if (!HUDPages.ContainsKey(hudPage)) {
                    HUDPages.Add(hudPage, false);
                    OnHUDPageReceived?.Invoke(hudPage);
                }
            }
        }

        protected override void OnRealtimeUpdate(string sender, RealtimeUpdate update) {
            if (!update.CurrentHudPage.Equals(currentHUDPage)) UpdateActiveHUDPage(update.CurrentHudPage);
        }

        private void OnSetHUDPage(string HUDPage) {
            UpdateActiveHUDPage(HUDPage);
        }

        private void UpdateActiveHUDPage(string HUDPage) {
            if (!HUDPages.ContainsKey(HUDPage)) return;

            HUDPages[HUDPage] = true;
            if (currentHUDPage != null) HUDPages[currentHUDPage] = false;
            currentHUDPage = HUDPage;
            OnActiveHUDPageUpdated?.Invoke(currentHUDPage);
        }
    }
}
