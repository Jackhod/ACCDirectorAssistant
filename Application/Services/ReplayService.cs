using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using Domain.Enums;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ACCAssistedDirector.Core.Services {
    public class ReplayService : GameUpdatesReceiver, IReplayService{

        public List<BroadcastingEventModel> Events { get; set; }
        public int MaxNumEvents { get; set; } = 5;

        public event EventAddedDelegate OnEventAdded;
        public event EventRemovedDelegate OnEventRemoved;

        private ICarEntryListService carEntryListService;

        private DateTime time;
        private DateTime start;
        private int sessionTimeMS;

        public ReplayService(IClientService clientService, ICarEntryListService carEntryListService) : base(clientService) {
            Events = new List<BroadcastingEventModel>();         
            this.carEntryListService = carEntryListService;
            start = DateTime.Now;
        }

        public void CancelService() {
            Events.Clear();
            Events = null;

            UnsubscribeFromGameUpdates();
        }

        protected override void OnRealtimeUpdate(string sender, RealtimeUpdate realtimeUpdate) {
            sessionTimeMS = Convert.ToInt32(realtimeUpdate.SessionTime.TotalMilliseconds);
            time = DateTime.Now;
        }

        bool aa = false;
        protected override void OnBroadastingEvent(string sender, BroadcastingEvent broadcastingEvent) {

            if (broadcastingEvent.Type == BroadcastingCarEventType.GreenFlag || broadcastingEvent.Type == BroadcastingCarEventType.PenaltyCommMsg || broadcastingEvent.Type == BroadcastingCarEventType.Accident) {
                var evnt = new BroadcastingEventModel(broadcastingEvent, carEntryListService.GetCarById(broadcastingEvent.CarId).CarInfo);
                Events.Insert(0, evnt);
                while (Events.Count > MaxNumEvents) {
                    RemoveEvent(Events.Last());
                }
                OnEventAdded?.Invoke(evnt);
            }

            //Debug.WriteLine(Convert.ToInt32((DateTime.Now - time).TotalMilliseconds) + sessionTimeMS - broadcastingEvent.TimeMs);
            //Debug.WriteLine(Convert.ToInt32((DateTime.Now - start).TotalMilliseconds) - broadcastingEvent.TimeMs);

            if (!aa) {
                _clientService.MessageHandler.RequestInstantReplay(broadcastingEvent.TimeMs - 5000, 4000, broadcastingEvent.CarId);
                aa = true;
            }
        }

        public void RemoveEvent(BroadcastingEventModel evnt) {
            Events.Remove(evnt);
            OnEventRemoved?.Invoke(evnt);
        }

        //private void OnOvertakeEvent(int carId1, int carId2) { //overtake events are not sent by the game so we'll have to handle them separately
        //    var evnt = new BroadcastingEventModel(
        //        "Overtaking", 
        //        carEntryListService.GetCarById(carId1).CarInfo, 
        //        (float)(DateTime.Now - DateTime.).TotalSeconds)
        //}
    }
}
