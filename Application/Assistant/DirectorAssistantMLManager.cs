using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using ACCAssistedDirector.Core.Services.Interfaces;
using Application.Assistant.Interfaces;
using Application.ML.BayesinPersonalizedRanking;
using Application.ML.NaiveBayesClassifier;
using Domain.ACCUpdatesStructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACCAssistedDirector.Core.Assistant {

    public delegate void StartedTrainingDelegate();
    public delegate void CompletedTrainingDelegate();

    public class DirectorAssistantMLManager : GameUpdatesReceiver {

        public CarPersonalSelector CarPersonalSelector { get; private set; }
        public CamPersonalSelector CamPersonalSelector { get; private set; }

        private bool _trainingEnabled = true;
        private bool _recordData;
        private bool _isRecordingData = false;
        private bool _isWritingCamData = false;
        private bool _isWritingCarData = false;
        private bool _trainingUpdated = false;
        private int _carSelectionId = 0;

        public event StartedTrainingDelegate OnStartedTraining;
        public event CompletedTrainingDelegate OnCompletedTraining;

        //Dependencies
        private ICarEntryListService _carEntryListService;
        private ICameraService _cameraService;
        private ITrackDataService _trackDataService;
        private IMLModelBuilder _carSelectorModelBuilder;
        private IMLModelBuilder _camSelectorModelBuilder;
        private ICSVHelperService<CamFeatureVector> _camFeaturesCSVHelper;
        private ICSVHelperService<CarFeatures> _carFeaturesCSVHelper;

        public DirectorAssistantMLManager(
            CarPersonalSelector carPersonalSelector, 
            CamPersonalSelector camPersonalSelector,
            ICarEntryListService carEntryListService, 
            ICameraService cameraService, 
            ICSVHelperService<CamFeatureVector> camFeaturesCSVHelper,
            ICSVHelperService<CarFeatures> carFeaturesCSVHelper,
            IClientService clientService,
            bool recordData) : base(clientService) {

            CarPersonalSelector = carPersonalSelector;
            CamPersonalSelector = camPersonalSelector;
            _carEntryListService = carEntryListService;
            _cameraService = cameraService;
            _carFeaturesCSVHelper = carFeaturesCSVHelper;
            _camFeaturesCSVHelper = camFeaturesCSVHelper;
            _recordData = recordData;
        }

        public void Close() {
            UnsubscribeFromGameUpdates();
        }

        protected override void OnRealtimeUpdate(string sender, RealtimeUpdate realtimeUpdate) {

            var sessionTime = realtimeUpdate.SessionTime.TotalSeconds;
            var remainingTime = realtimeUpdate.SessionEndTime.TotalSeconds;

            if (_recordData && sessionTime > 0 && remainingTime > 0) {
                _isRecordingData = true;
                _trainingUpdated = false;
            } else { 
                _isRecordingData = false; 
            }

            if (!IsWriting() && _trainingEnabled && !_trainingUpdated && !IsWriting() && remainingTime == 0) { 
                UpdateTrainingsAsync();
                _trainingUpdated = true;
            }
        }

        private async Task UpdateTrainingsAsync() {
            Trace.WriteLine("Started training");
            OnStartedTraining?.Invoke();
            await Task.Run(() => UpdateCarTraining());
            await Task.Run(() => UpdateCamTraining());
            await Task.Delay(3000);
            OnCompletedTraining?.Invoke();
            Trace.WriteLine("Completed training");
        }

        private void UpdateCarTraining() {
            _carSelectorModelBuilder = new BPRModelBuilder(_carFeaturesCSVHelper);

            if (!Directory.Exists("Models")) Directory.CreateDirectory("Models");
            _carSelectorModelBuilder.LoadTrainingData("Dataset/CarsOnlySelection.csv");
            _carSelectorModelBuilder.LoadModel("Models");
            _carSelectorModelBuilder.Train();
            _carSelectorModelBuilder.SaveModel("Models");
        }

        private void UpdateCamTraining() {
            _camSelectorModelBuilder = new NBCModelBuilder(_camFeaturesCSVHelper);

            if (!Directory.Exists("Models")) Directory.CreateDirectory("Models");
            _camSelectorModelBuilder.LoadTrainingData("Dataset/CamsAll.csv");
            _camSelectorModelBuilder.LoadModel("Models");
            _camSelectorModelBuilder.Train();
            _camSelectorModelBuilder.SaveModel("Models");
        }
      
        public void CarDataUpdate() {

            if (!_isRecordingData) return;

            CarPersonalSelector.EvaluateFeatures(_carEntryListService);
        }

        public async Task CamDataUpdateAsync() {

            if (!_isRecordingData || _isWritingCamData) return;

            _isWritingCamData = true;
            await Task.Run(() => SaveCamsFeatures("Dataset/CamsAll.csv"));
            _isWritingCamData = false;
        }

        public async Task CarFocusUpdateAsync() {
            if (!_isRecordingData || _isWritingCarData) return;

            _isWritingCarData = true;
            if (CarPersonalSelector.carFeaturesDict != null) {
                await Task.Run(() => SaveCarsFeatures("Dataset/CarsOnlySelection.csv"));
                _carSelectionId++;
            }
            _isWritingCarData = false;
        }

        private void SaveCarsFeatures(string path) {

            if (!Directory.Exists("Dataset")) Directory.CreateDirectory("Dataset");

            CarPersonalSelector.CarSelection(_carEntryListService.GetFocusedCar().CarInfo.CarIndex, _carSelectionId);

            Trace.WriteLine("savecarsfeatures " + _carSelectionId);
            if (_carSelectionId == 0)
                _carFeaturesCSVHelper.WriteToFile(path, true, CarPersonalSelector.carFeaturesDict.Values.ToList());
            else
                _carFeaturesCSVHelper.AppendToFile(path, CarPersonalSelector.carFeaturesDict.Values.ToList());
        }

        private bool _firstSave = true; 
        private void SaveCamsFeatures(string path) {

            if (!Directory.Exists("Dataset")) Directory.CreateDirectory("Dataset");

            var featureVector = new CamFeatureVector() {
                CarsAround = _carEntryListService.GetFocusedCar().CarsAroundMe30m,
                GapRear = Math.Min(_carEntryListService.GetFocusedCar().GapRearSeconds, 5f),
                GapFront = Math.Min(_carEntryListService.GetFocusedCar().GapFrontSeconds, 5f),
                Label = _cameraService.CurrentCam.CamType
            };

            if (featureVector.CarsAround > 0 || featureVector.GapFront > 0 || featureVector.GapRear > 0) {
                if (_firstSave) {
                    _camFeaturesCSVHelper.WriteToFile(path, true, new List<CamFeatureVector>() { featureVector });
                    _firstSave = false;
                } else {
                    _camFeaturesCSVHelper.AppendToFile(path, new List<CamFeatureVector>() { featureVector });
                }
            }
        }

        private bool IsWriting() {
            return _isWritingCarData || _isWritingCamData;
        }

        public void DisableTraining() {
            _trainingEnabled = false;
        }

        public void EnableTraining() {
            _trainingEnabled = true;
        }

        public bool IsTrainingEnabled() {
            return _trainingEnabled;
        }
    }
}
