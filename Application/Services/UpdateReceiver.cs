using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Services {
    public class UpdateReceiver {

        protected IClientService _clientService;
        public UpdateReceiver(IClientService clientService) {
            _clientService = clientService;
            _clientService.MessageHandler.OnTrackDataUpdate += OnTrackDataUpdate;
            _clientService.MessageHandler.OnEntrylistReceived += OnEntrylistReceived;
            _clientService.MessageHandler.OnEntrylistUpdate += OnEntryListUpdate;
            _clientService.MessageHandler.OnRealtimeUpdate += OnRealtimeUpdate;
            _clientService.MessageHandler.OnRealtimeCarUpdate += OnRealtimeCarUpdate;
            _clientService.MessageHandler.OnBroadcastingEvent += OnBroadastingEvent;
        }

        protected virtual void OnTrackDataUpdate(string sender, TrackData trackData) { }
        protected virtual void OnEntrylistReceived (string sender, IEnumerable<ushort> carIds) { }
        protected virtual void OnEntryListUpdate(string sender, CarInfo carInfo, IEnumerable<DriverInfo> drivers) { }
        protected virtual void OnRealtimeUpdate (string sender, RealtimeUpdate realtimeUpdate) { }
        protected virtual void OnRealtimeCarUpdate (string sender, RealtimeCarUpdate realtimeCarUpdate) { }
        protected virtual void OnBroadastingEvent (string sender, BroadcastingEvent broadcastingEvent) { }
    }
}
