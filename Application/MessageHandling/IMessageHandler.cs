using Domain.ACCUpdatesStructs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.MessageHandling {

    public delegate void ConnectionStateChangedDelegate(int connectionId, bool connectionSuccess, bool isReadonly, string error);
    public delegate void TrackDataUpdateDelegate(string sender, TrackData trackUpdate);
    public delegate void EntryListReceivedDelegate(string sender, IEnumerable<ushort> carIds);
    public delegate void EntryListUpdateDelegate(string sender, CarInfo car, IEnumerable<DriverInfo> drivers);
    public delegate void RealtimeUpdateDelegate(string sender, RealtimeUpdate update);
    public delegate void RealtimeCarUpdateDelegate(string sender, RealtimeCarUpdate carUpdate);
    public delegate void BroadcastingEventDelegate(string sender, BroadcastingEvent evt);

    public delegate void SetCameraDelegate(string cameraSet, string camera, bool isAutoDirector);
    public delegate void SetFocusDelegate(int carId, bool isAutoDirector);
    public delegate void SetHudPageDelegate(string requestedPage);
    public delegate void StartReplayDelegate();
    public delegate void SendMessageDelegate(byte[] payload);

    public interface IMessageHandler {

        public void Init(string connectionIdentifier, SendMessageDelegate sendMessageDelegate);
        public void SetFocus(int carIndex, bool instantFocus, bool isAutoDirector = false);
        public void SetCamera(string cameraSet, string camera, bool isAutoDirector = false);
        public void SetFocusAndCamera(int carIndex, string cameraSet, string camera, bool isAutoDirector = false);
        public void SetHudPage(string requestedHudPage);
        public void RequestInstantReplay(float replayStartTimeMS, float durationSeconds, int carId);
        public void RequestInstantReplay(float replayStartTimeMS, float durationSeconds);
        public void RequestEntryList();

        public event ConnectionStateChangedDelegate OnConnectionStateChanged;
        public event TrackDataUpdateDelegate OnTrackDataUpdate;
        public event EntryListReceivedDelegate OnEntrylistReceived;
        public event EntryListUpdateDelegate OnEntrylistUpdate;
        public event RealtimeUpdateDelegate OnRealtimeUpdate;
        public event RealtimeCarUpdateDelegate OnRealtimeCarUpdate;
        public event BroadcastingEventDelegate OnBroadcastingEvent;

        //events for sent messages
        public event SetCameraDelegate OnSetCamera;
        public event SetFocusDelegate OnSetFocus;
        public event SetHudPageDelegate OnSetHUDPage;
        public event StartReplayDelegate OnStartedReplay;
    }
}
