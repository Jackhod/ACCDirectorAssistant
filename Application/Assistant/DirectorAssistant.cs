using ACCAssistedDirector.Core.Assistant.Interfaces;
using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using ACCAssistedDirector.Core.Services.Interfaces;
using Domain.ACCUpdatesStructs;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACCAssistedDirector.Core.Assistant {
    public class DirectorAssistant : GameUpdatesReceiver, IDirectorAssistant {

        public List<DirectorTipModel> DirectorTips { get; set; }
        public DirectorAssistantMLManager DirectorAssistantMLManager { get; set; }
        public CarSelector CarSelector { get; set; } //car algorithmic selector
        public CamSelector CamSelector { get; set; } //cam algorithmic selector
        public CarPersonalSelector CarPersonalSelector { get; set; } //car machine learning selector       
        public CamPersonalSelector CamPersonalSelector { get; set; } //cam machine learning selector
        public bool IsAutoPilotActive { get; set; }

        //Dependencies
        private ICarEntryListService _carEntryListService;
        private ICameraService _cameraService;
        private ITrackDataService _trackDataService;
        private IClientService _clientService;
        private ICSVHelperService<CarFeatures> _carFeaturesCSVHelper;
        private ICSVHelperService<CamFeatureVector> _camFeaturesCSVHelper;

        private bool _carPersonalSelectorReady = false;
        private bool _camPersonalSelectorReady = false;
        private bool _isRace = true;

        public event NewTipsGeneratedDelegate OnNewTipsGenerated;

        public DirectorAssistant(
            ICarEntryListService carEntryListService, 
            ICameraService cameraService, 
            ITrackDataService trackDataService, 
            IClientService clientService, 
            ICSVHelperService<CarFeatures> carFeaturesCSVHelper, 
            ICSVHelperService<CamFeatureVector> camFeaturesCSVHelper) : base(clientService) {

            IsAutoPilotActive = false;
            
            CarSelector = new CarSelector();
            CamSelector = new CamSelector();
            CarPersonalSelector = new CarPersonalSelector();          
            CamPersonalSelector = new CamPersonalSelector();
            DirectorAssistantMLManager = new DirectorAssistantMLManager(CarPersonalSelector, CamPersonalSelector, carEntryListService, cameraService, camFeaturesCSVHelper, carFeaturesCSVHelper, clientService, true);

            _carEntryListService = carEntryListService;
            _cameraService = cameraService;
            _trackDataService = trackDataService;
            _clientService = clientService;
            _carFeaturesCSVHelper = carFeaturesCSVHelper;
            _camFeaturesCSVHelper = camFeaturesCSVHelper;

            _carEntryListService.OnLastCarUpdated += OnLastCarUpdated;
            _carEntryListService.OnFocusedCarUpdated += OnSetFocus;
            _cameraService.OnActiveCamTypeUpdated += OnSetCamera;

            _carPersonalSelectorReady = CarPersonalSelector.Init();
            _camPersonalSelectorReady = CamPersonalSelector.Init();
        }

        public void CancelService() {
            if (DirectorTips != null) {
                DirectorTips.Clear();
                DirectorTips = null;
            }

            _carEntryListService.OnLastCarUpdated -= OnLastCarUpdated;
            _carEntryListService.OnFocusedCarUpdated -= OnSetFocus;
            _cameraService.OnActiveCamTypeUpdated -= OnSetCamera;

            UnsubscribeFromGameUpdates();

            DirectorAssistantMLManager.Close();
        }

        protected override void OnRealtimeUpdate(string sender, RealtimeUpdate realtimeUpdate) {

            _isRace = realtimeUpdate.SessionType == Domain.Enums.RaceSessionType.Race;

            if (!_isRace && DirectorAssistantMLManager.IsTrainingEnabled()) DirectorAssistantMLManager.DisableTraining();

            if (_isRace && !DirectorAssistantMLManager.IsTrainingEnabled()) DirectorAssistantMLManager.EnableTraining();             
        }

        private void OnLastCarUpdated() {

            if (_isRace) {
                DirectorAssistantMLManager.CarDataUpdate();

                var focusedCar = _carEntryListService.GetFocusedCar();

                GenerateSuggestions();
            }
        }
       
        private void OnSetFocus(bool autoDirectorChangedFocus) {
            if(_isRace && !autoDirectorChangedFocus) DirectorAssistantMLManager.CarFocusUpdateAsync();            
        }
              
        private void OnSetCamera(CameraModel cam, bool autoDirectorChangedCamera) {
            if (_isRace && !autoDirectorChangedCamera) DirectorAssistantMLManager.CamDataUpdateAsync();           
        }

        private void GenerateSuggestions() {
            
            DirectorTips = new List<DirectorTipModel>();
            var carSuggestions = GenerateCarSuggestions();
            
            foreach (var carSugg in carSuggestions) {
                var camSugg = GenerateCamSuggestions(carSugg.Tip);
                var directorTip = new DirectorTipModel() { CarTip = carSugg, CamTips = new List<TipModel<CameraModel>>() };
                foreach (var cam in camSugg) directorTip.CamTips.Add(cam);
                DirectorTips.Add(directorTip);
            }

            if (IsAutoPilotActive) SelectBestCarAndCam(DirectorTips);

            OnNewTipsGenerated?.Invoke(DirectorTips);
        }    
        
        private List<TipModel<CarUpdateModel>> GenerateCarSuggestions() {
            List<TipModel<CarUpdateModel>> suggestedCars = new List<TipModel<CarUpdateModel>>();

            var cars = _carEntryListService.CarEntryList;
            var carUpdatesOrdered = cars.OrderBy(c => c.TrackPosition).ToList();
            var currentFocusSeconds = (float)(DateTime.Now - _carEntryListService.LastFocusChange).TotalSeconds;

            if (_carPersonalSelectorReady) {
                //we get the suggestions from the machine learning car selector
                suggestedCars.AddRange(CarPersonalSelector.GetPreferredCars(carUpdatesOrdered, currentFocusSeconds, 5));

                //We get the suggestion from the algorithmic car selector and add it if it is not already in the suggestions list
                AddAlgorithmicCarTip(suggestedCars, 1);

                //adding the focused car to the list the get its cam suggestion
                AddFocusedCarTip(suggestedCars);                
            } else {
                AddAlgorithmicCarTip(suggestedCars, 5);
                AddFocusedCarTip(suggestedCars);
            }

            return suggestedCars;
        }

        private void AddFocusedCarTip(List<TipModel<CarUpdateModel>> suggestedCars) {
            var focusedCarTip = new TipModel<CarUpdateModel>() { Tip = _carEntryListService.GetFocusedCar(), Score = .0f };
            foreach (var carTip in suggestedCars) {
                if (focusedCarTip.Tip == carTip.Tip) return;
            }
            suggestedCars.Add(focusedCarTip);
        }

        private void AddAlgorithmicCarTip(List<TipModel<CarUpdateModel>> suggestedCars, int howMany) {
            var carUpdatesOrdered = _carEntryListService.CarEntryList.OrderBy(c => c.TrackPosition).ToList();
            var currentFocusSeconds = (float)(DateTime.Now - _carEntryListService.LastFocusChange).TotalSeconds;
            var algCarTips = CarSelector.GetPreferredCar(carUpdatesOrdered, currentFocusSeconds, howMany);
            
            foreach (var algTip in algCarTips) {
                foreach (var carTip in suggestedCars) {
                    if (algTip.Tip == carTip.Tip) return;
                }
                suggestedCars.Add(algTip);
            }            
        }

        private List<TipModel<CameraModel>> GenerateCamSuggestions(CarUpdateModel car) {
            var cams = new List<TipModel<CameraModel>>();

            if (_camPersonalSelectorReady) {
                //Gets the suggestion from the machine learning camera selector
                cams.AddRange(CamPersonalSelector.GetPreferredCams(_cameraService, car, 1));
            } else {
                //Gets the suggestion from the algorithmic camera selector
                cams.Add(CamSelector.GetPreferredCamera(
                    car,
                    _cameraService.CurrentCam,
                    _trackDataService.TrackDataModel.TrackMeters,
                    _cameraService.LastCameraChange,
                    _cameraService.LastCameraSetChange,
                    _cameraService.CamTypeLastActive,
                    _cameraService.CameraSets.SelectMany(x => x.Value),
                    _cameraService.TVCameraSets.SelectMany(x => x.Value),
                    _cameraService.TVCamLearningProgress
                    ));
            }
            return cams;
        }

        private void SelectBestCarAndCam(List<DirectorTipModel> directorTips) {

            var currentFocusSeconds = (float)(DateTime.Now - _carEntryListService.LastFocusChange).TotalSeconds;
            var currentCamSeconds = (float)(DateTime.Now - _cameraService.LastCameraSetChange).TotalSeconds; 

            CarUpdateModel bestCar = _carEntryListService.GetFocusedCar();
            CameraModel bestCam = _cameraService.CurrentCam;
            float bestCarScore;           
            if (_carPersonalSelectorReady) {
                bestCarScore = CarPersonalSelector.EvaluateCar(bestCar, _carEntryListService.CarEntryList.Count, currentFocusSeconds);

            } else {
                bestCarScore = 0f;
            }

            //If a car stays in focus for more than a minute we slowly decrease its score
            if(currentFocusSeconds > 60) bestCarScore *= 1 - Math.Min(currentFocusSeconds, 120f) / 120f;

            foreach (var tip in directorTips) {
                var car = tip.CarTip.Tip;
                var cam = tip.CamTips[0].Tip;
                var carTimeWeightedScore = tip.CarTip.Score; //if the car is in focus for less than a minute the value doesn't change

                if (!car.HasFocus) {
                    //Focus fast
                    carTimeWeightedScore *= Math.Min(currentFocusSeconds, 10f) / 10f;

                    //Focus slow
                    carTimeWeightedScore *= Math.Min(currentFocusSeconds, 30f) / 30f;                         
                }else if(currentFocusSeconds > 60) {
                    carTimeWeightedScore *= 1 - Math.Min(currentFocusSeconds, 120f) / 120f;
                }

                if(carTimeWeightedScore >= bestCarScore) {
                    bestCarScore = carTimeWeightedScore;
                    bestCar = car;
                    bestCam = cam;
                }
            }

            var isFocusChange = bestCar != null && !bestCar.HasFocus;
            var isCameraChange = bestCam != null && _cameraService.CurrentCam.CamType != bestCam.CamType && currentCamSeconds > 10;

            if (isFocusChange && isCameraChange) {
                _clientService.MessageHandler.SetFocusAndCamera(bestCar.CarInfo.CarIndex, bestCam.CameraSetName, bestCam.CameraName, true);
            } else if (isFocusChange) {
                _clientService.MessageHandler.SetFocus(bestCar.CarInfo.CarIndex, true, true);
            } else if (isCameraChange) {
                _clientService.MessageHandler.SetCamera(bestCam.CameraSetName, bestCam.CameraName, true);
            }
        }
    }
}
