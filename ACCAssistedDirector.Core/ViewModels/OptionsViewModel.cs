using ACCAssistedDirector.Core.MessageHandling;
using Infrastructure.Networking;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.ViewModels {
    public class OptionsViewModel : MvxViewModel {

        public IMvxCommand DisconnectCommand { get; set; }
        public IMvxCommand CloseCommand { get; set; }

        private readonly IMvxNavigationService _navigationService;
        private ACCUdpRemoteClient Client { get; set; } 

        Action CloseCallback { get; set; }

        public OptionsViewModel(Action closeCallback) {
            CloseCallback = closeCallback;
            DisconnectCommand = new MvxCommand(Disconnect);
            CloseCommand = new MvxCommand(Close);
            _navigationService = Mvx.IoCProvider.Resolve<IMvxNavigationService>();
        }

        private void Close() {
            CloseCallback?.Invoke();
        }

        private async void Disconnect() {
            Mvx.IoCProvider.Resolve<IClientService>().Shutdown();
            await _navigationService.Navigate<ClientConnectionViewModel>();
        }
    }
}
