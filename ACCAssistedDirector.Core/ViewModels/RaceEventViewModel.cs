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

        private readonly Action<BroadcastingEventModel, int, int> _replayDelegate;

        private readonly IReplayService replayService;

        public RaceEventViewModel(BroadcastingEventModel raceEvent, Action<BroadcastingEventModel, int, int> replayDelegate, IReplayService replayService) {
            RaceEvent = raceEvent;
            _replayDelegate = replayDelegate;
            this.replayService = replayService;
            ReplayCommand = new MvxCommand(Replay);
            RemoveCommand = new MvxCommand(RemoveEvent);
        }

        private void Replay() {

            int eventTime = (int) (RaceEvent.TimeMs * 0.001f);
            int startTimeSeconds = eventTime - 5;
            int duration = 10;

            if (EventType == "GreenFlag") {
                startTimeSeconds = eventTime -7;
                duration = 25;
            }

            if (EventType == "PenaltyCommMsg") {
                startTimeSeconds = eventTime -10;
                duration = 6;
            }

            if (EventType == "Accident") {
                startTimeSeconds = eventTime - 10;
                duration = 10;
            }
            _replayDelegate(RaceEvent, startTimeSeconds, duration);
        }

        private void RemoveEvent() { replayService.RemoveEvent(RaceEvent); }
    }
}
