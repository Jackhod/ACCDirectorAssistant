﻿using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using Domain.Models;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.ViewModels {
    public class ReplayPanelViewModel : MvxViewModel {

        private MvxObservableCollection<RaceEventViewModel> _raceEvents = new MvxObservableCollection<RaceEventViewModel>();
        public MvxObservableCollection<RaceEventViewModel> RaceEvents
        {
            get { return _raceEvents; }
            set
            {
                SetProperty(ref _raceEvents, value);
                RaisePropertyChanged(() => RaceEvents);
            }
        }

        private IReplayService replayService;
        private IClientService clientService;

        public ReplayPanelViewModel(IReplayService replayService, IClientService clientService) {
            this.clientService = clientService;
            this.replayService = replayService;
            replayService.OnEventAdded += OnEventAdded;
            replayService.OnEventRemoved += OnEventRemoved;
        }     

        private void OnEventAdded(BroadcastingEventModel evnt) {
            RaceEvents.Insert(0, new RaceEventViewModel(evnt, OnHighlightReplay, replayService));
            RaisePropertyChanged(() => RaceEvents);
        }

        private void OnEventRemoved(BroadcastingEventModel evnt) {
            RaceEvents.Remove(RaceEvents.FirstOrDefault(re => re.RaceEvent == evnt));
            RaisePropertyChanged(() => RaceEvents);
        }

        private void OnHighlightReplay(BroadcastingEventModel raceEvent, float replayStartTime, float durationSeconds) {
            clientService.MessageHandler.RequestInstantReplay(replayStartTime, durationSeconds * 1000.0f, raceEvent.CarId);
        }
    }
}
