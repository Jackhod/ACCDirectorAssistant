using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using MvvmCross.Commands;
//using ACCAssistedDirector.Core.AutomaticDirector;
using ACCAssistedDirector.Core.Assistant;
using MvvmCross;
using ACCAssistedDirector.Core.Services;
using ACCAssistedDirector.Core.Services.Interfaces;
using ACCAssistedDirector.Core.Assistant.Interfaces;
using AppAnalytics;
using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using Domain.Models;

namespace ACCAssistedDirector.Core.ViewModels {
    public class MainViewModel : MvxViewModel {

        IClientService Client { get; set; }
        public CarEntryListViewModel CarEntryListVM { get; set; }
        public HUDPanelViewModel HUDPanelVM { get; set; }
        public CameraPanelViewModel CameraPanelVM { get; set; }
        public static DirectorAssistantViewModel DirectorAssistantVM { get; set; }
        public static TrackMapViewModel TrackMapVM { get; set; }
        public ReplayPanelViewModel ReplayPanelVM { get; set; }
        public OptionsViewModel OptionsVM { get; set; }

        public IMvxCommand OptionsCommand { get; set; }
        private bool _isOptionsOpen;
        public bool IsOptionsOpen
        {
            get { return _isOptionsOpen; }
            set { SetProperty(ref _isOptionsOpen, value); }
        }

        public override void Prepare()
        {
            Client = Mvx.IoCProvider.Resolve<IClientService>();
            var trackDataService = Mvx.IoCProvider.Resolve<ITrackDataService>();

            var carEntryListService = Mvx.IoCProvider.Resolve<ICarEntryListService>();           
            CarEntryListVM = new CarEntryListViewModel(Client, carEntryListService);

            var cameraService = Mvx.IoCProvider.Resolve<ICameraService>();
            CameraPanelVM = new CameraPanelViewModel(Client, cameraService);

            var HUDService = Mvx.IoCProvider.Resolve<IHUDService>();
            HUDPanelVM = new HUDPanelViewModel(Client, HUDService);

            var replayService = Mvx.IoCProvider.Resolve<IReplayService>();
            ReplayPanelVM = new ReplayPanelViewModel(replayService, Client);

            OptionsVM = new OptionsViewModel(CloseOptionsMenu);
            OptionsCommand = new MvxCommand(OpenOptionsMenu);

            var directorAssistant = Mvx.IoCProvider.Resolve<IDirectorAssistant>();
            DirectorAssistantVM = new DirectorAssistantViewModel(directorAssistant, Client, CarEntryListVM);
            TrackMapVM = new TrackMapViewModel(directorAssistant, carEntryListService);

            //EventsTracker.TrackEvents(Client, directorAssistant, carEntryListService, cameraService);

            IsOptionsOpen = false;
        }

        public override async Task Initialize() {
            await base.Initialize();           
        }

        private void OpenOptionsMenu() {
            IsOptionsOpen = true;
        }

        private void CloseOptionsMenu() {
            IsOptionsOpen = false;
        }
    }
}
