using ACCAssistedDirector.Core;
using ACCAssistedDirector.Core.Assistant.Interfaces;
using ACCAssistedDirector.Core.MessageHandling;
using ACCAssistedDirector.Core.Services;
using Domain.Models;
using Infrastructure.Analytics;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AppAnalytics {
    public static class EventsTracker {

        private static TelemetryClient _telemetryClient;

        private static IClientService _clientService;       
        private static ICameraService _cameraService;
        private static IDirectorAssistant _directorAssistant;
        private static ICarEntryListService _carEntryListService;

        private static CarFocusTracker _carFocusTracker;
        private static CamTracker _camTracker;

        private static int _carTrainingCycles;
        private static int _camTrainingCycles;

        public static void StartTracker() {   
            
            _telemetryClient = new TelemetryClient();
            //_telemetryClient.InstrumentationKey = "07e707d1-e795-41b7-a52e-524f18add175";
            _telemetryClient.InstrumentationKey = "346990ca-6964-447d-a9f2-581b5b7b1603"; //test  key
            _telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
        }

        public static void EndTracker() {
            Debug.WriteLine("tracker ended");
            if (_telemetryClient != null) {
                _telemetryClient.Flush(); 
                System.Threading.Thread.Sleep(1000);
            }
        }

        public static void TrackEvents(IClientService clientService, IDirectorAssistant directorAssistant, ICarEntryListService carEntryListService, ICameraService cameraService) {

            _carFocusTracker = new CarFocusTracker(_telemetryClient, directorAssistant);
            _camTracker = new CamTracker(_telemetryClient, directorAssistant);
            _carEntryListService = carEntryListService;
            _cameraService = cameraService;
            _directorAssistant = directorAssistant;

            _carEntryListService.OnFocusedCarUpdated += OnSetFocus;
            _cameraService.OnActiveCamTypeUpdated += OnSetCamera;
        }

        public static void OnSetFocus(bool autoDirectorChangedFocus) {

            if (_directorAssistant.DirectorTips == null || _directorAssistant.DirectorTips.Count == 0) return;

            var focusedCarIndex = _carEntryListService.GetFocusedCar().CarInfo.CarIndex;
            _carFocusTracker.OnSetFocus(focusedCarIndex, autoDirectorChangedFocus);
        }

        public static void OnSetCamera(CameraModel cam, bool autoDirectorChangedCamera) {

            if (_directorAssistant.DirectorTips == null || _directorAssistant.DirectorTips.Count == 0) return;

            var focusedCarIndex = _carEntryListService.GetFocusedCar().CarInfo.CarIndex;

            if (_carFocusTracker.IsSuggested(focusedCarIndex))
                _camTracker.OnSetCamera(cam, focusedCarIndex, autoDirectorChangedCamera);
            else
                _camTracker.OnSetCamera(cam, -1, autoDirectorChangedCamera);
        }
    }
}
