using ACCAssistedDirector.Core.Assistant.Interfaces;
using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using Domain.Models;
using MvvmCross.ViewModels;
using System.Collections.Generic;

namespace ACCAssistedDirector.Core.ViewModels {
    public class DirectorAssistantViewModel : MvxViewModel {

        private MvxObservableCollection<DirectorTipViewModel> _directorTips = new MvxObservableCollection<DirectorTipViewModel>();
        public MvxObservableCollection<DirectorTipViewModel> DirectorTips
        {
            get { return _directorTips; }
            set
            {
                SetProperty(ref _directorTips, value);
                RaisePropertyChanged(() => DirectorTips);
            }
        }

        private bool _autoDirector;
        public bool AutoDirector
        {
            get { return _autoDirector; }
            set 
            { 
                SetProperty(ref _autoDirector, value);
                _directorAssistant.IsAutoPilotActive = value;
            }
        }

        private bool _displayTrainingMessage;
        public bool DisplayTrainingMessage
        {
            get { return _displayTrainingMessage; }
            set { SetProperty(ref _displayTrainingMessage, value); }
        }


        private readonly IDirectorAssistant _directorAssistant;
        private readonly IClientService _clientService;
        private CarEntryListViewModel _carEntryListVM;

        public DirectorAssistantViewModel(IDirectorAssistant directorAssistant, IClientService clientService, CarEntryListViewModel carEntryListVM) {
            _directorAssistant = directorAssistant;
            _clientService = clientService;
            _carEntryListVM = carEntryListVM;
            _displayTrainingMessage = false;
            AutoDirector = directorAssistant.IsAutoPilotActive;

            _directorAssistant.OnNewTipsGenerated += OnNewTipsGenerated;
            _directorAssistant.DirectorAssistantMLManager.OnStartedTraining += OnStartedTraining;
            _directorAssistant.DirectorAssistantMLManager.OnCompletedTraining += OnCompletedTraining;
            
        }

        public void PrepareToClose() {
            _directorTips.Clear();
            _directorTips = null;

            _directorAssistant.OnNewTipsGenerated -= OnNewTipsGenerated;
            _directorAssistant.DirectorAssistantMLManager.OnStartedTraining -= OnStartedTraining;
            _directorAssistant.DirectorAssistantMLManager.OnCompletedTraining -= OnCompletedTraining;

            _directorAssistant.CancelService();
        }

        private void OnNewTipsGenerated(List<DirectorTipModel> directorTips) {
            DirectorTips.Clear();

            foreach(var tip in directorTips) {
                DirectorTips.Add(new DirectorTipViewModel(tip, _clientService, OnRemoveTip, _carEntryListVM));
            }
        }       

        private void OnRemoveTip(DirectorTipViewModel directorTipVM) {
            DirectorTips.Remove(directorTipVM);
        }   
        
        private void OnStartedTraining() {
            DisplayTrainingMessage = true;
        }

        private void OnCompletedTraining() {
            DisplayTrainingMessage = false;
        }
    }
}
