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
using MvvmCross.IoC;
using Infrastructure.FileIO;
using MvvmCross.Navigation;

namespace ACCAssistedDirector.Core.ViewModels {
    public class MainViewModel : MvxViewModel<object, bool> {

        private static int instanceCount = 0;
        public int instanceid;

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

        private readonly IMvxNavigationService _navigationService;

        public MainViewModel(IMvxNavigationService navigationService, IClientService client) {

            instanceid = instanceCount;
            instanceCount += 1;

            Debug.WriteLine("MAINVIEW constructor " + instanceid);

            Client = client;
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<ICarEntryListService, CarEntryListService>();
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<ITrackDataService, TrackDataService>();
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<ICameraService, CameraService>();
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IHUDService, HUDService>();
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IReplayService, ReplayService>();
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IDirectorAssistant, DirectorAssistant>();
            Mvx.IoCProvider.RegisterType(typeof(ICSVHelperService<>), typeof(CSVHandler<>));

            _navigationService = navigationService;

            Client.MessageHandler.OnRealtimeUpdate += CheckSessionStatus;
        }

        public override void Prepare(object o)
        {

            Debug.WriteLine("MAINVIEW prepare " + instanceid);

            Client = Mvx.IoCProvider.Resolve<IClientService>();
            var trackDataService = Mvx.IoCProvider.Resolve<ITrackDataService>();

            var carEntryListService = Mvx.IoCProvider.Resolve<ICarEntryListService>();           
            CarEntryListVM = new CarEntryListViewModel(Client, carEntryListService);

            var cameraService = Mvx.IoCProvider.Resolve<ICameraService>();
            CameraPanelVM = new CameraPanelViewModel(Client, cameraService);

            var HUDService = Mvx.IoCProvider.Resolve<IHUDService>();
            HUDPanelVM = new HUDPanelViewModel(Client, HUDService);

            var replayService = Mvx.IoCProvider.Resolve<IReplayService>();
            ReplayPanelVM = new ReplayPanelViewModel(replayService, Client, carEntryListService);

            OptionsVM = new OptionsViewModel(CloseOptionsMenu);
            OptionsCommand = new MvxCommand(OpenOptionsMenu);

            var directorAssistant = Mvx.IoCProvider.Resolve<IDirectorAssistant>();
            DirectorAssistantVM = new DirectorAssistantViewModel(directorAssistant, Client, CarEntryListVM);
            TrackMapVM = new TrackMapViewModel(directorAssistant, carEntryListService);

            //Uncomment to enable azure tracking (also in app.xaml.cs and MainWindow.xaml.cs)
            //EventsTracker.TrackEvents(Client, directorAssistant, carEntryListService, cameraService);

            IsOptionsOpen = false;
        }

        public override async Task Initialize() {

            Debug.WriteLine("MAINVIEW initialize " + instanceid);

            await base.Initialize();           
        }

        private void OpenOptionsMenu() {
            IsOptionsOpen = true;
        }

        private void CloseOptionsMenu() {
            IsOptionsOpen = false;
        }

        private double _sessionTime = 0f;
        private void CheckSessionStatus(string sender, RealtimeUpdate realtimeUpdate) {
            var sessionTime = realtimeUpdate.SessionTime.TotalSeconds;

            if (sessionTime > _sessionTime) {
                _sessionTime = sessionTime;
            } else if (sessionTime < _sessionTime) { //new session or race restarted
                Debug.WriteLine("Race restarded or new session " + instanceid);
                _sessionTime = 0;

                Client.Shutdown();
                Client.Dispose();

                CarEntryListVM.PrepareToClose();
                _navigationService.Close(CarEntryListVM);

                HUDPanelVM.PrepareToClose();
                _navigationService.Close(HUDPanelVM);

                CameraPanelVM.PrepareToClose();
                _navigationService.Close(CameraPanelVM);

                DirectorAssistantVM.PrepareToClose();
                _navigationService.Close(DirectorAssistantVM);

                TrackMapVM.PrepareToClose();
                _navigationService.Close(TrackMapVM);

                ReplayPanelVM.PrepareToClose();
                _navigationService.Close(ReplayPanelVM);

                Mvx.IoCProvider.Resolve<ITrackDataService>().CancelService();

                _navigationService.Close(OptionsVM);

                Client.MessageHandler.OnRealtimeUpdate -= CheckSessionStatus;
                _navigationService.Close<bool>(this, false);
            }
        }
    }
}
