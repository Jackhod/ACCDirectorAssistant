using ACCAssistedDirector.Core.Services;
using Domain.Models;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;

namespace ACCAssistedDirector.Core.ViewModels {
    public class RaceEventViewModel : MvxViewModel {

        public BroadcastingEventModel RaceEvent { get; set; }
        public string EventType => RaceEvent.Type;
        public string EventMsg => RaceEvent.Msg;
        public string Driver =>  RaceEvent.CarData.Drivers[RaceEvent.CarData.CurrentDriverIndex].LastName;

        public IMvxCommand ReplayCommand { get; set; }
        public IMvxCommand RemoveCommand { get; set; }

        private readonly Action<BroadcastingEventModel, float, float> _replayDelegate;

        private readonly IReplayService replayService;

        public RaceEventViewModel(BroadcastingEventModel raceEvent, Action<BroadcastingEventModel, float, float> replayDelegate, IReplayService replayService) {
            RaceEvent = raceEvent;
            _replayDelegate = replayDelegate;
            this.replayService = replayService;
            ReplayCommand = new MvxCommand(Replay);
            RemoveCommand = new MvxCommand(RemoveEvent);
        }

        private void Replay() {

            float startReplayTime = RaceEvent.TimeMs - (5 * 1000);
            float duration = 10f;

            if (EventType == "GreenFlag") {
                startReplayTime = RaceEvent.TimeMs - (7 * 1000);
                duration = 25f;
            }

            if (EventType == "PenaltyCommMsg") {
                startReplayTime = RaceEvent.TimeMs - (10 * 1000);
                duration = 6f;
            }

            if (EventType == "Accident") {
                startReplayTime = RaceEvent.TimeMs - (10 * 1000);
                duration = 10f;
            }
            _replayDelegate(RaceEvent, startReplayTime, duration);
        }

        private void RemoveEvent() { replayService.RemoveEvent(RaceEvent); }
    }
}
