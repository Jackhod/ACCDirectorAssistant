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
    public class DirectorAssistant : UpdateReceiver, IDirectorAssistant {

        public List<DirectorTipModel> DirectorTips { get; set; }
        public DirectorAssistantMLManager DirectorAssistantMLManager { get; set; }
        public CarSelector CarSelector { get; set; } //car algorithmic selector
        public CamSelector CamSelector { get; set; } //cam algorithmic selector
        public CarPersonalSelector CarPersonalSelector { get; set; } //car machine learning selector       
        public CamPersonalSelector CamPersonalSelector { get; set; } //cam machine learning selector
        public bool IsAutoPilotActive { get; set; }

        //Dependencies
        private ICarEntryListService carEntryListService;
        private ICameraService cameraService;
        private ITrackDataService trackDataService;
        private IClientService clientService;

        private int carSelectionId = 0;
        private int camSelectionId = 0;

        private bool _carPersonalSelectorReady = false;
        private bool _camPersonalSelectorReady = false;
        private bool _isRace = true;

        public event NewTipsGeneratedDelegate OnNewTipsGenerated;

        private ICSVHelperService<List<int>> _statsCSVHelper;

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

            this.carEntryListService = carEntryListService;
            this.cameraService = cameraService;
            this.trackDataService = trackDataService;
            this.clientService = clientService;

            carEntryListService.OnLastCarUpdated += OnLastCarUpdated;
            carEntryListService.OnFocusedCarUpdated += OnSetFocus;
            cameraService.OnActiveCamTypeUpdated += OnSetCamera;

            _carPersonalSelectorReady = CarPersonalSelector.Init();
            _camPersonalSelectorReady = CamPersonalSelector.Init();

            //TESTING
            DirectorAssistantMLManager.OnCompletedTraining += WriteStats;
        }

        private void OnLastCarUpdated() {

            if (_isRace) {
                DirectorAssistantMLManager.CarDataUpdate();

                var focusedCar = carEntryListService.GetFocusedCar();

                GenerateSuggestions();

            }
        }

        
        private void OnSetFocus(bool autoDirectorChangedFocus) {
            if(_isRace && !autoDirectorChangedFocus) DirectorAssistantMLManager.CarFocusUpdateAsync();

            
        }
       
        
        private void OnSetCamera(CameraModel cam, bool autoDirectorChangedCamera) {
            if (_isRace && !autoDirectorChangedCamera) DirectorAssistantMLManager.CamDataUpdateAsync();

            
        }

        //TESTING
        private bool writingStats = false;
        private int _totalFocusChanges = 0;
        private List<int> _suggestedFocusChanges = new List<int>() { 0, 0, 0, 0, 0 };
        private void FocusSuggestionsStats() {
            _totalFocusChanges += 1;
            var focusedCarIndex = carEntryListService.GetFocusedCar().CarInfo.CarIndex;
            for (int i = 0; i < 5; i++) {
                var t = DirectorTips[i];
                if (t.CarTip.Tip.CarInfo.CarIndex == focusedCarIndex) {
                    for (int j = i; j < 5; j++) {
                        _suggestedFocusChanges[j] += 1;
                    }
                }
            }
            System.Diagnostics.Trace.WriteLine("focus: " + _suggestedFocusChanges[0] + " " + 
                                                           _suggestedFocusChanges[1] + " " +
                                                           _suggestedFocusChanges[2] + " " +
                                                           _suggestedFocusChanges[3] + " " +
                                                           _suggestedFocusChanges[4] + " " + _totalFocusChanges);

        }

        //TESTING
        private int _totalCamChanges = 0;
        private List<int> _suggestedCamChanges = new List<int>() { 0, 0 };
        private void CamSuggestionsStats(CameraModel cam) {
            _totalCamChanges += 1;
            var focusedCarIndex = carEntryListService.GetFocusedCar().CarInfo.CarIndex;
            
            foreach (var t in DirectorTips) {
                var tippedCar = t.CarTip.Tip;
                if (tippedCar.CarInfo.CarIndex == focusedCarIndex) { //find the tipped car
                    for(int i = 0; i < t.CamTips.Count; i++) { //check the tipped cams
                        var camTip = t.CamTips[i].Tip;
                        if(camTip.CamType == cam.CamType) {
                            for (int j = i; j < 2; j++) _suggestedCamChanges[j] += 1;
                        }
                    }

                    System.Diagnostics.Trace.WriteLine("cam: " + _suggestedCamChanges[0] + " " +
                                                                 _suggestedCamChanges[1] + " " + _totalCamChanges);
                }
            }
        }

        //TESTING
        public void WriteStats() {
            writingStats = true;           
            List<int> focusStats = new List<int>();
            focusStats.AddRange(_suggestedFocusChanges);
            focusStats.Add(_totalFocusChanges);
            string sFocusStats = string.Join(",", focusStats);
            File.AppendAllText("C:/Users/gvann/Desktop/SuggestionStats/FocusStats.txt", sFocusStats + "\n");

            List<int> cameraStats = new List<int>();
            cameraStats.AddRange(_suggestedCamChanges);
            cameraStats.Add(_totalCamChanges);
            string sCameraStats = string.Join(",", cameraStats);
            File.AppendAllText("C:/Users/gvann/Desktop/SuggestionStats/CameraStats.txt", sCameraStats + "\n");

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

            //TESTING
            SelectTestCarAndCam();

            if (IsAutoPilotActive) SelectBestCarAndCam(DirectorTips);

            OnNewTipsGenerated?.Invoke(DirectorTips);
        }    
        
        private List<TipModel<CarUpdateModel>> GenerateCarSuggestions() {
            List<TipModel<CarUpdateModel>> suggestedCars = new List<TipModel<CarUpdateModel>>();

            var cars = carEntryListService.CarEntryList;
            //var trackMeters = trackDataService.TrackDataModel.TrackMeters;
            var carUpdatesOrdered = cars.OrderBy(c => c.TrackPosition).ToList();
            var currentFocusSeconds = (float)(DateTime.Now - carEntryListService.LastFocusChange).TotalSeconds;

            if (_carPersonalSelectorReady) {
                //we get the suggestions from the machine learning car selector
                suggestedCars.AddRange(CarPersonalSelector.GetPreferredCars(carUpdatesOrdered, currentFocusSeconds, 5));

                //adding the focused car to the list the get its cam suggestion
                AddFocusedCarTip(suggestedCars);

                //We get the suggestion from the algorithmic car selector and add it if it is not already in the suggestions list
                //AddAlgorithmicCarTip(suggestedCars, 1);
            } else {
                //suggestedCars.AddRange(CarSelector.GetPreferredCar(carUpdatesOrdered, /*trackMeters,*/ currentFocusSeconds, 5));
                AddAlgorithmicCarTip(suggestedCars, 5);
                AddFocusedCarTip(suggestedCars);
            }

            //Debug.WriteLine("");
            //foreach(var c in suggestedCars) {
            //    Debug.WriteLine(c.Tip.CarInfo.CarIndex + " " + c.Score);
            //}

            return suggestedCars;
        }

        private void AddFocusedCarTip(List<TipModel<CarUpdateModel>> suggestedCars) {
            var focusedCarTip = new TipModel<CarUpdateModel>() { Tip = carEntryListService.GetFocusedCar(), Score = .0f };
            foreach (var carTip in suggestedCars) {
                if (focusedCarTip.Tip == carTip.Tip) return;
            }
            suggestedCars.Add(focusedCarTip);
        }

        private void AddAlgorithmicCarTip(List<TipModel<CarUpdateModel>> suggestedCars, int howMany) {
            var carUpdatesOrdered = carEntryListService.CarEntryList.OrderBy(c => c.TrackPosition).ToList();
            var currentFocusSeconds = (float)(DateTime.Now - carEntryListService.LastFocusChange).TotalSeconds;
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
                //cams.Add(CamPersonalSelector.GetCam(cameraService, car));
                cams.AddRange(CamPersonalSelector.GetPreferredCams(cameraService, car, 2));
            } else {
                //Gets the suggestion from the algorithmic camera selector
                cams.Add(CamSelector.GetPreferredCamera(
                    car,
                    cameraService.CurrentCam,
                    trackDataService.TrackDataModel.TrackMeters,
                    cameraService.LastCameraChange,
                    cameraService.LastCameraSetChange,
                    cameraService.CamTypeLastActive,
                    cameraService.CameraSets.SelectMany(x => x.Value),
                    cameraService.TVCameraSets.SelectMany(x => x.Value),
                    cameraService.TVCamLearningProgress
                    ));
            }
            return cams;
        }

        private void SelectBestCarAndCam(List<DirectorTipModel> directorTips) {

            var currentFocusSeconds = (float)(DateTime.Now - carEntryListService.LastFocusChange).TotalSeconds;
            var currentCamSeconds = (float)(DateTime.Now - cameraService.LastCameraSetChange).TotalSeconds; 

            CarUpdateModel bestCar = carEntryListService.GetFocusedCar();
            CameraModel bestCam = cameraService.CurrentCam;
            float bestCarScore;           
            if (_carPersonalSelectorReady) {
                bestCarScore = CarPersonalSelector.EvaluateCar(bestCar, carEntryListService.CarEntryList.Count, currentFocusSeconds);

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
            var isCameraChange = bestCam != null && cameraService.CurrentCam.CamType != bestCam.CamType && currentCamSeconds > 10;

            if (isFocusChange && isCameraChange) {
                clientService.MessageHandler.SetFocusAndCamera(bestCar.CarInfo.CarIndex, bestCam.CameraSetName, bestCam.CameraName, true);
            } else if (isFocusChange) {
                clientService.MessageHandler.SetFocus(bestCar.CarInfo.CarIndex, true, true);
            } else if (isCameraChange) {
                clientService.MessageHandler.SetCamera(bestCam.CameraSetName, bestCam.CameraName, true);
            }
        }
        
        //TEST 
        private void SelectTestCarAndCam() {
            var currentFocusSeconds = (float)(DateTime.Now - carEntryListService.LastFocusChange).TotalSeconds;
            var currentCamSeconds = (float)(DateTime.Now - cameraService.LastCameraSetChange).TotalSeconds;
            var cars = carEntryListService.CarEntryList;
            var trackMeters = trackDataService.TrackDataModel.TrackMeters;
            var carUpdatesOrdered = cars.OrderBy(c => c.TrackPosition).ToList();

            CarUpdateModel bestCar = CarSelector.GetTestPreferredCar(carUpdatesOrdered, /*trackMeters,*/ currentFocusSeconds, 1).Single().Tip;
            CameraModel bestCam = CamSelector.GetPreferredCamera(
                    bestCar,
                    cameraService.CurrentCam,
                    trackDataService.TrackDataModel.TrackMeters,
                    cameraService.LastCameraChange,
                    cameraService.LastCameraSetChange,
                    cameraService.CamTypeLastActive,
                    cameraService.CameraSets.SelectMany(x => x.Value),
                    cameraService.TVCameraSets.SelectMany(x => x.Value),
                    cameraService.TVCamLearningProgress
                    ).Tip;
            

            var isFocusChange = bestCar != null && !bestCar.HasFocus;
            var isCameraChange = bestCam != null && cameraService.CurrentCam.CamType != bestCam.CamType;

            if (isFocusChange && isCameraChange) {
                Debug.WriteLine("changed both");
                clientService.MessageHandler.SetFocusAndCamera(bestCar.CarInfo.CarIndex, bestCam.CameraSetName, bestCam.CameraName, false);
                //TESTING
                if (DirectorTips != null && !writingStats) FocusSuggestionsStats();
                if (DirectorTips != null && !writingStats) CamSuggestionsStats(bestCam);
            } else if (isFocusChange) {
                Debug.WriteLine("changed focus");
                clientService.MessageHandler.SetFocus(bestCar.CarInfo.CarIndex, true, false);
                //TESTING
                if (DirectorTips != null && !writingStats) FocusSuggestionsStats();
            } else if (isCameraChange) {
                Debug.WriteLine("changed cam");
                clientService.MessageHandler.SetCamera(bestCam.CameraSetName, bestCam.CameraName, false);
                //TESTING
                if (DirectorTips != null && !writingStats) CamSuggestionsStats(bestCam);
            }
        }

        //Check if the current session is a race
        protected override void OnRealtimeUpdate(string sender, RealtimeUpdate realtimeUpdate) {
            _isRace = realtimeUpdate.SessionType == Domain.Enums.RaceSessionType.Race;

            if (!_isRace && DirectorAssistantMLManager.IsTrainingEnabled()) DirectorAssistantMLManager.DisableTraining();

            if (_isRace && !DirectorAssistantMLManager.IsTrainingEnabled()) DirectorAssistantMLManager.EnableTraining();
        }
    }
}
