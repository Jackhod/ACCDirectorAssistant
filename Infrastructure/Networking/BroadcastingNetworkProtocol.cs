using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using Domain.Enums;
using Infrastructure.Networking.MessageTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Infrastructure.Networking {      
    public class BroadcastingNetworkProtocol : IMessageHandler {

        public const int BROADCASTING_PROTOCOL_VERSION = 4;
        private string ConnectionIdentifier { get; set; }       
        public int ConnectionId { get; private set; }
        public float TrackMeters { get; private set; }
     
        public event ConnectionStateChangedDelegate OnConnectionStateChanged;
        public event TrackDataUpdateDelegate OnTrackDataUpdate;
        public event EntryListReceivedDelegate OnEntrylistReceived;
        public event EntryListUpdateDelegate OnEntrylistUpdate;
        //public event DriverInfoReceivedDelegate OnDriverInfoReceived;
        public event RealtimeUpdateDelegate OnRealtimeUpdate;
        public event RealtimeCarUpdateDelegate OnRealtimeCarUpdate;
        public event BroadcastingEventDelegate OnBroadcastingEvent;

        //events for sent messages
        public event SetCameraDelegate OnSetCamera;
        public event SetFocusDelegate OnSetFocus;
        public event SetHudPageDelegate OnSetHUDPage;
        public event StartReplayDelegate OnStartedReplay;

        private SendMessageDelegate Send { get; set; }

        #region EntryList handling
        // To avoid huge UDP pakets for longer entry lists, we will first receive the indexes of cars and drivers,
        // cache the entries and wait for the detailled updates
        //List<CarInfo> _entryListCars = new List<CarInfo>();
        #endregion

        #region optional failsafety - detect when we have a desync and need a new entry list

        //DateTime lastEntrylistRequest = DateTime.Now;

        #endregion

        public void Init(string connectionIdentifier, SendMessageDelegate sendMessageDelegate) {
            if (string.IsNullOrEmpty(connectionIdentifier))
                throw new ArgumentNullException(nameof(connectionIdentifier), $"No connection identifier set; we use this to distinguish different connections. Using the remote IP:Port is a good idea");

            if (sendMessageDelegate == null)
                throw new ArgumentNullException(nameof(sendMessageDelegate), $"The protocol class doesn't know anything about the network layer; please put a callback we can use to send data via UDP");

            ConnectionIdentifier = connectionIdentifier;
            Send = sendMessageDelegate;
        }

        #region Data reception

        internal void ProcessMessage(BinaryReader br) {
            // Any message starts with an 1-byte command type
            var messageType = (InboundMessageTypes)br.ReadByte();
            switch (messageType) {
                case InboundMessageTypes.REGISTRATION_RESULT: {
                        ConnectionId = br.ReadInt32();
                        var connectionSuccess = br.ReadByte() > 0;
                        var isReadonly = br.ReadByte() == 0;
                        var errMsg = ReadString(br);

                        OnConnectionStateChanged?.Invoke(ConnectionId, connectionSuccess, isReadonly, errMsg);

                        // In case this was successful, we will request the initial data
                        RequestEntryList();
                        RequestTrackData();
                    }
                    break;
                case InboundMessageTypes.ENTRY_LIST: {

                        var connectionId = br.ReadInt32();
                        var carEntryCount = br.ReadUInt16();
                        ushort[] carIds = new ushort[carEntryCount];
                        for (int i = 0; i < carEntryCount; i++) {
                            carIds[i] = br.ReadUInt16();
                        }
                        OnEntrylistReceived?.Invoke(ConnectionIdentifier, carIds);
                    }
                    break;
                case InboundMessageTypes.ENTRY_LIST_CAR: {

                        var carId = br.ReadUInt16();

                        var carInfo = new CarInfo();
                        carInfo.CarIndex = carId;
                        carInfo.CarModelType = br.ReadByte(); // Byte sized car model
                        carInfo.TeamName = ReadString(br);
                        carInfo.RaceNumber = br.ReadInt32();
                        carInfo.CupCategory = br.ReadByte(); // Cup: Overall/Pro = 0, ProAm = 1, Am = 2, Silver = 3, National = 4
                        carInfo.CurrentDriverIndex = br.ReadByte();
                        carInfo.Nationality = (NationalityEnum)br.ReadUInt16();

                        // Now the drivers on this car:
                        var driversOnCarCount = br.ReadByte();
                        DriverInfo[] drivers = new DriverInfo[driversOnCarCount];
                        for (int di = 0; di < driversOnCarCount; di++) {
                            var driverInfo = new DriverInfo();

                            driverInfo.FirstName = ReadString(br);
                            driverInfo.LastName = ReadString(br);
                            driverInfo.ShortName = ReadString(br);
                            driverInfo.Category = (DriverCategory)br.ReadByte(); // Platinum = 3, Gold = 2, Silver = 1, Bronze = 0
                            driverInfo.Nationality = (NationalityEnum)br.ReadUInt16();
                            drivers[di] = driverInfo;
                        }

                        OnEntrylistUpdate?.Invoke(ConnectionIdentifier, carInfo, drivers);
                    }
                    break;
                case InboundMessageTypes.REALTIME_UPDATE: {
                        RealtimeUpdate update = new RealtimeUpdate();
                        update.EventIndex = (int)br.ReadUInt16();
                        update.SessionIndex = (int)br.ReadUInt16();
                        update.SessionType = (RaceSessionType)br.ReadByte();
                        update.Phase = (SessionPhase)br.ReadByte();
                        var sessionTime = br.ReadSingle();
                        update.SessionTime = TimeSpan.FromMilliseconds(sessionTime);
                        var sessionEndTime = br.ReadSingle();
                        update.SessionEndTime = TimeSpan.FromMilliseconds(sessionEndTime);

                        update.FocusedCarIndex = br.ReadInt32();
                        update.ActiveCameraSet = ReadString(br);
                        update.ActiveCamera = ReadString(br);
                        update.CurrentHudPage = ReadString(br);

                        update.IsReplayPlaying = br.ReadByte() > 0;
                        if (update.IsReplayPlaying) {
                            update.ReplaySessionTime = br.ReadSingle();
                            update.ReplayRemainingTime = br.ReadSingle();
                        }

                        update.TimeOfDay = TimeSpan.FromMilliseconds(br.ReadSingle());
                        update.AmbientTemp = br.ReadByte();
                        update.TrackTemp = br.ReadByte();
                        update.Clouds = br.ReadByte() / 10.0f;
                        update.RainLevel = br.ReadByte() / 10.0f;
                        update.Wetness = br.ReadByte() / 10.0f;

                        update.BestSessionLap = ReadLap(br);

                        OnRealtimeUpdate?.Invoke(ConnectionIdentifier, update);
                    }
                    break;
                case InboundMessageTypes.REALTIME_CAR_UPDATE: {
                        RealtimeCarUpdate carUpdate = new RealtimeCarUpdate();

                        carUpdate.CarIndex = br.ReadUInt16();
                        carUpdate.DriverIndex = br.ReadUInt16(); // Driver swap will make this change
                        carUpdate.DriverCount = br.ReadByte();
                        carUpdate.Gear = br.ReadByte() - 2; // -2 makes the R -1, N 0 and the rest as-is
                        carUpdate.WorldPosX = br.ReadSingle();
                        carUpdate.WorldPosY = br.ReadSingle();
                        carUpdate.Yaw = br.ReadSingle();
                        carUpdate.CarLocation = (CarLocationEnum)br.ReadByte(); // - , Track, Pitlane, PitEntry, PitExit = 4
                        carUpdate.Kmh = br.ReadUInt16();
                        carUpdate.Position = br.ReadUInt16(); // official P/Q/R position (1 based)
                        carUpdate.CupPosition = br.ReadUInt16(); // official P/Q/R position (1 based)
                        carUpdate.TrackPosition = br.ReadUInt16(); // position on track (1 based)
                        carUpdate.SplinePosition = br.ReadSingle(); // track position between 0.0 and 1.0
                        carUpdate.Laps = br.ReadUInt16();

                        carUpdate.Delta = br.ReadInt32(); // Realtime delta to best session lap
                        carUpdate.BestSessionLap = ReadLap(br);
                        carUpdate.LastLap = ReadLap(br);
                        carUpdate.CurrentLap = ReadLap(br);

                        OnRealtimeCarUpdate?.Invoke(ConnectionIdentifier, carUpdate);
                    }
                    break;
                case InboundMessageTypes.TRACK_DATA: {
                        var connectionId = br.ReadInt32();
                        var trackData = new TrackData();

                        trackData.TrackName = ReadString(br);
                        trackData.TrackId = br.ReadInt32();
                        trackData.TrackMeters = br.ReadInt32();
                        TrackMeters = trackData.TrackMeters > 0 ? trackData.TrackMeters : -1;

                        trackData.CameraSets = new Dictionary<string, List<string>>();

                        var cameraSetCount = br.ReadByte();
                        for (int camSet = 0; camSet < cameraSetCount; camSet++) {
                            var camSetName = ReadString(br);
                            trackData.CameraSets.Add(camSetName, new List<string>());

                            var cameraCount = br.ReadByte();
                            for (int cam = 0; cam < cameraCount; cam++) {
                                var cameraName = ReadString(br);
                                trackData.CameraSets[camSetName].Add(cameraName);
                            }
                        }

                        var hudPages = new List<string>();
                        var hudPagesCount = br.ReadByte();
                        for (int i = 0; i < hudPagesCount; i++) {
                            hudPages.Add(ReadString(br));
                        }
                        trackData.HUDPages = hudPages;

                        OnTrackDataUpdate?.Invoke(ConnectionIdentifier, trackData);
                    }
                    break;
                case InboundMessageTypes.BROADCASTING_EVENT: {
                        BroadcastingEvent evt = new BroadcastingEvent() {
                            Type = (BroadcastingCarEventType)br.ReadByte(),
                            Msg = ReadString(br),
                            TimeMs = br.ReadInt32(),
                            CarId = br.ReadInt32(),
                        };

                        OnBroadcastingEvent?.Invoke(ConnectionIdentifier, evt);
                    }
                    break;
                default:
                    break;
            }
        }

        private static LapInfo ReadLap(BinaryReader br) {
            var lap = new LapInfo();
            lap.LaptimeMS = br.ReadInt32();

            lap.CarIndex = br.ReadUInt16();
            lap.DriverIndex = br.ReadUInt16();

            var splitCount = br.ReadByte();
            for (int i = 0; i < splitCount; i++)
                lap.Splits.Add(br.ReadInt32());

            lap.IsInvalid = br.ReadByte() > 0;
            lap.IsValidForBest = br.ReadByte() > 0;

            var isOutlap = br.ReadByte() > 0;
            var isInlap = br.ReadByte() > 0;

            if (isOutlap)
                lap.Type = LapType.Outlap;
            else if (isInlap)
                lap.Type = LapType.Inlap;
            else
                lap.Type = LapType.Regular;

            // Now it's possible that this is "no" lap that doesn't even include a 
            // first split, we can detect this by comparing with int32.Max
            while (lap.Splits.Count < 3) {
                lap.Splits.Add(null);
            }

            // "null" entries are Int32.Max, in the C# world we can replace this to null
            for (int i = 0; i < lap.Splits.Count; i++)
                if (lap.Splits[i] == Int32.MaxValue)
                    lap.Splits[i] = null;

            if (lap.LaptimeMS == Int32.MaxValue)
                lap.LaptimeMS = null;

            return lap;
        }

        private static string ReadString(BinaryReader br) {
            var length = br.ReadUInt16();
            var bytes = br.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }
                             
        public void RequestEntryList() {
            using (var ms = new MemoryStream())
            using (var br = new BinaryWriter(ms)) {
                br.Write((byte)OutboundMessageTypes.REQUEST_ENTRY_LIST); // First byte is always the command type
                br.Write((int)ConnectionId);

                Send(ms.ToArray());
            }
        }

        private void RequestTrackData() {
            using (var ms = new MemoryStream())
            using (var br = new BinaryWriter(ms)) {
                br.Write((byte)OutboundMessageTypes.REQUEST_TRACK_DATA); // First byte is always the command type
                br.Write((int)ConnectionId);

                Send(ms.ToArray());
            }
        }

        #endregion

        #region user commands

        private int _selectedCarIndex;

        public void SetFocus(int carIndex, bool instantFocus, bool isAutoDirector = false) {
            if (instantFocus) {
                SetFocusInternal(Convert.ToUInt16(carIndex), null, null);
                _selectedCarIndex = -1;
                OnSetFocus?.Invoke(carIndex, isAutoDirector);
            } else {
                _selectedCarIndex = carIndex;
            }
        }

        public void SetCamera(string cameraSet, string camera, bool isAutoDirector = false) {
            if (_selectedCarIndex != -1) {
                SetFocusAndCamera(_selectedCarIndex, cameraSet, camera);
                _selectedCarIndex = -1;
            } else {
                SetFocusInternal(null, cameraSet, camera);
                OnSetCamera?.Invoke(cameraSet, camera, isAutoDirector);
            }

        }

        public void SetFocusAndCamera(int carIndex, string cameraSet, string camera, bool isAutoDirector = false) {
            SetFocusInternal(Convert.ToUInt16(carIndex), cameraSet, camera);
            OnSetFocus?.Invoke(carIndex, isAutoDirector);
            OnSetCamera?.Invoke(cameraSet, camera, isAutoDirector);
        }

        private void SetFocusInternal(UInt16? carIndex, string cameraSet, string camera) {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms)) {
                bw.Write((byte)OutboundMessageTypes.CHANGE_FOCUS); // First byte is always the command type
                bw.Write((int)ConnectionId);

                if (!carIndex.HasValue) {
                    bw.Write((byte)0); // No change of focused car
                } else {
                    bw.Write((byte)1);
                    bw.Write((UInt16)(carIndex.Value));
                }

                if (string.IsNullOrEmpty(cameraSet) || string.IsNullOrEmpty(camera)) {
                    bw.Write((byte)0); // No change of camera set or camera
                } else {
                    bw.Write((byte)1);
                    WriteString(bw, cameraSet);
                    WriteString(bw, camera);
                }

                Send(ms.ToArray());
            }
        }

        public void SetHudPage(string requestedHudPage) {
            RequestHUDPageInternal(requestedHudPage);
            OnSetHUDPage?.Invoke(requestedHudPage);
        }

        public void RequestInstantReplay(float replayStartTime, float duration, int carId) {
            RequestInstantReplayInternal(replayStartTime, duration * 1000.0f, carId);
            OnStartedReplay?.Invoke();
        }

        public void RequestInstantReplayInternal(float startSessionTime, float durationMS, int initialFocusedCarIndex = -1, string initialCameraSet = "", string initialCamera = "") {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms)) {
                bw.Write((byte)OutboundMessageTypes.INSTANT_REPLAY_REQUEST); // First byte is always the command type
                bw.Write((int)ConnectionId);

                bw.Write((float)startSessionTime);
                bw.Write((float)durationMS);
                bw.Write((int)initialFocusedCarIndex);

                WriteString(bw, initialCameraSet);
                WriteString(bw, initialCamera);

                Send(ms.ToArray());
            }
        }

        public void RequestHUDPageInternal(string hudPage) {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms)) {
                bw.Write((byte)OutboundMessageTypes.CHANGE_HUD_PAGE); // First byte is always the command type
                bw.Write((int)ConnectionId);

                WriteString(bw, hudPage);

                Send(ms.ToArray());
            }
        }

        #endregion

        #region ACC instance connection

        internal void RequestConnection(string displayName, string connectionPassword, int msRealtimeUpdateInterval, string commandPassword) {
            using (var ms = new MemoryStream())
            using (var br = new BinaryWriter(ms)) {
                br.Write((byte)OutboundMessageTypes.REGISTER_COMMAND_APPLICATION); // First byte is always the command type
                br.Write((byte)BROADCASTING_PROTOCOL_VERSION);

                WriteString(br, displayName);
                WriteString(br, connectionPassword);
                br.Write(msRealtimeUpdateInterval);
                WriteString(br, commandPassword);

                Send(ms.ToArray());
            }
        }

        internal void Disconnect() {
            using (var ms = new MemoryStream())
            using (var br = new BinaryWriter(ms)) {
                br.Write((byte)OutboundMessageTypes.UNREGISTER_COMMAND_APPLICATION); // First byte is always the command type
                br.Write((int)ConnectionId);
                Send(ms.ToArray());
            }
        }

        #endregion

        private static void WriteString(BinaryWriter bw, string s) {
            var bytes = Encoding.UTF8.GetBytes(s);
            bw.Write(Convert.ToUInt16(bytes.Length));
            bw.Write(bytes);
        }
    }
}
