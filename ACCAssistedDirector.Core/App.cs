using MvvmCross.ViewModels;
using ACCAssistedDirector.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using MvvmCross;
using ACCAssistedDirector.Core.Services;
using MvvmCross.IoC;
using ACCAssistedDirector.Core.Services.Interfaces;
using ACCAssistedDirector.Core.Assistant;
using AppAnalytics;
using ACCAssistedDirector.Core.Assistant.Interfaces;
using ACCAssistedDirector.Core.MessageHandling;
using Infrastructure.Networking;
using Infrastructure.FileIO;

namespace ACCAssistedDirector.Core {
    public class App : MvxApplication {

        public override void Initialize() {

            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IClientService, ACCUdpRemoteClient>();

            //Mvx.IoCProvider.LazyConstructAndRegisterSingleton<ICarEntryListService, CarEntryListService>();
            //Mvx.IoCProvider.LazyConstructAndRegisterSingleton<ITrackDataService, TrackDataService>();
            //Mvx.IoCProvider.LazyConstructAndRegisterSingleton<ICameraService, CameraService>();
            //Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IHUDService, HUDService>();
            //Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IReplayService, ReplayService>();
            //Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IDirectorAssistant, DirectorAssistant>();
            //Mvx.IoCProvider.RegisterType(typeof(ICSVHelperService<>), typeof(CSVHandler<>));

            RegisterAppStart<ClientConnectionViewModel>();
        }
    }
}