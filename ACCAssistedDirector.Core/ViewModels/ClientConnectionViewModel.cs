using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.Navigation.EventArguments;
using MvvmCross.ViewModels;

namespace ACCAssistedDirector.Core.ViewModels {
    class ClientConnectionViewModel : MvxViewModel {

        #region properties

        private string _ipAddr;

        public string IPAddr
        {
            get { return _ipAddr; }
            set { SetProperty(ref _ipAddr, value); }
        }

        private int _port;

        public int Port
        {
            get { return _port; }
            set { SetProperty(ref _port, value); }
        }

        private string _displayName;

        public string DisplayName
        {
            get { return _displayName; }
            set { SetProperty(ref _displayName, value); }
        }

        private string _connectionPW;

        public string ConnectionPW
        {
            get { return _connectionPW; }
            set { SetProperty(ref _connectionPW, value); }
        }

        private string _commandPW;

        public string CommandPW
        {
            get { return _commandPW; }
            set { SetProperty(ref _commandPW, value); }
        }

        private int _updateIntervalMS;

        public int UpdateIntervalMS{

            get { return _updateIntervalMS; }
            set { SetProperty(ref _updateIntervalMS, value); }
        }

        #endregion

        public IMvxCommand ConnectCommand { get; set; }
        public IClientService Client { get; set;  }

        private bool _viewErrorMessage;
        public bool ViewErrorMessage
        {
            get { return _viewErrorMessage; }
            set { SetProperty(ref _viewErrorMessage, value); }
        }

        private bool _viewLoginCommands;
        public bool ViewLoginCommands
        {
            get { return _viewLoginCommands; }
            set { SetProperty(ref _viewLoginCommands, value); }
        }

        private readonly IMvxNavigationService _navigationService;

        public ClientConnectionViewModel(IMvxNavigationService navigationService, IClientService client) {

            ConnectCommand = new MvxCommand(Connect);
            IPAddr = "192.168.1.16";
            Port = 9000;
            DisplayName = "Your name";
            ConnectionPW = "asd";
            CommandPW = "";
            UpdateIntervalMS = 1000;

            _navigationService = navigationService;
            _navigationService.AfterNavigate += Boh;
            Client = client;
            Client.MessageHandler.OnConnectionStateChanged += OpenMainView;
        }

        public override void Prepare() {
            base.Prepare();
            _viewErrorMessage = false;
            _viewLoginCommands = false;

            string authString = null;
            try {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead("https://www.cusmilanoesport.it/data/acc_beta.txt");
                StreamReader reader = new StreamReader(stream);
                authString = reader.ReadToEnd();
            }catch(Exception ex) {
                _viewErrorMessage = true;
            }

            if (authString != null) {
                _viewLoginCommands = true;
                _viewErrorMessage = false;
            } else {
                _viewErrorMessage = true;
                _viewLoginCommands = false;
            }
        }        

        public void Connect(){                   
            Debug.WriteLine("connecting!");            
            Client.Init(_ipAddr, _port, _displayName, _connectionPW, _commandPW, _updateIntervalMS);
            Client.Connect();            
        }

        private async void OpenMainView(int connectionId, bool connectionSuccess, bool isReadonly, string error) {

            Debug.WriteLine("connected");
            if (connectionSuccess) {
                bool result = false;
                result = await _navigationService.Navigate<MainViewModel, object, bool>(new object());
                Debug.WriteLine(result);
                Connect();
            } else {
                Debug.WriteLine("connection failed");
            }
 

            //await _navigationService.Navigate<MainViewModel, ACCUdpRemoteClient>(Client);
        }

        private void Boh(object sender, IMvxNavigateEventArgs e) {
            Debug.WriteLine("back to connection");
        }
    }
}
