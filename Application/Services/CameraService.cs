using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ACCAssistedDirector.Core.Services {
    public class CameraService : UpdateReceiver, ICameraService {

        public Dictionary<string, List<CameraModel>> CameraSets { get; private set; }
        public Dictionary<string, List<TVCameraModel>> TVCameraSets { get; private set; }
        public Dictionary<CamTypeEnum, DateTime> CamTypeLastActive { get; set; }
        public float TVCamLearningProgress { get; set; }
        public DateTime LastCameraSetChange { get; private set; }
        public DateTime LastCameraChange { get; private set; }
        public CameraModel CurrentCam { get; private set; }

        #region events
        public event CamsReceivedDelegate OnCamsReceived;
        public event ActiveCamtypeUpdatedDelegate OnActiveCamTypeUpdated;
        public event ActiveCamUpdatedDelegate OnActiveCamUpdated;
        #endregion

        private ICarEntryListService carEntryListService;
        private ITrackDataService trackDataService;
        private CameraModel _previousCam;
        float lastFocusedCarSplinePosition = -1f;
        int lastFocusedCarSpeed = -1;
        int lastFocusedCarId = -1;

        public CameraService(IClientService clientService, ICarEntryListService carEntryListService, ITrackDataService trackDataService) :base(clientService) {
            this.carEntryListService = carEntryListService;
            this.trackDataService = trackDataService;

            clientService.MessageHandler.OnSetCamera += SetActiveCamera;
        }

        protected override void OnTrackDataUpdate(string sender, TrackData trackData) {
            if (CameraSets == null) {
                CameraSets = new Dictionary<string, List<CameraModel>>();
                TVCameraSets = new Dictionary<string, List<TVCameraModel>>();
                CamTypeLastActive = new Dictionary<CamTypeEnum, DateTime>();
                _previousCam = null;

                foreach (var camSet in trackData.CameraSets) {

                    if (camSet.Key.EndsWith("vr", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    CameraSets.Add(camSet.Key, new List<CameraModel>());

                    if (camSet.Key.StartsWith("set")) {
                        TVCameraModel lastTvCam = null;
                        foreach (var tvCamName in camSet.Value) {
                            var cam = new TVCameraModel(camSet.Key, tvCamName);
                            if (lastTvCam != null)
                                cam.PrevCam = lastTvCam;
                            lastTvCam = cam;
                            CameraSets[camSet.Key].Add(cam);
                            AddTVCamera(camSet.Key, cam);
                        }
                    } else {
                        foreach (var otherCamName in camSet.Value) {
                            var cam = new CameraModel(camSet.Key, otherCamName);
                            CameraSets[camSet.Key].Add(cam);
                        }
                    }
                }

                foreach (var camType in CameraSets.SelectMany(x => x.Value).Select(x => x.CamType).Distinct()) {
                    CamTypeLastActive.Add(camType, DateTime.Now.AddMinutes(-10));
                }

                TryLoadTVCameraDefs(trackDataService.TrackDataModel.TrackName);
                UpdateTVCamLearningProgress();
                OnCamsReceived?.Invoke();
            }
        }

        private void SetActiveCamera(string cameraSet, string camera, bool isAutoDirector = false) {
            try {
                if (CameraSets != null && CameraSets.ContainsKey(cameraSet)) {
                    var cam = CameraSets[cameraSet].Single(x => x.CameraName == camera);
                    if (!cam.IsActive) {
                        var lastCams = CameraSets.SelectMany(x => x.Value).Where(x => x.IsActive).ToList();
                        if (lastCams.Count() > 0) _previousCam = lastCams[0]; //should be just one anyway                       
                        foreach (var item in lastCams) item.IsActive = false;
                        cam.IsActive = true;
                        LastCameraChange = DateTime.Now;

                        if (cam.CamType != CurrentCam?.CamType) {
                            LastCameraSetChange = DateTime.Now;
                            OnActiveCamTypeUpdated?.Invoke(cam, isAutoDirector);
                        }
                        CurrentCam = cam;
                        OnActiveCamUpdated?.Invoke(cam, isAutoDirector);
                    }
                }
                if (CurrentCam != null)
                    CamTypeLastActive[CurrentCam.CamType] = DateTime.Now;
            } catch (Exception ex) {
                Debug.WriteLine(ex.StackTrace);
            }
        }

        protected override void OnRealtimeUpdate(string sender, RealtimeUpdate realtimeUpdate) {

            if (CurrentCam == null || CurrentCam.CameraName != realtimeUpdate.ActiveCamera || CurrentCam.CameraSetName != realtimeUpdate.ActiveCameraSet)
                SetActiveCamera(realtimeUpdate.ActiveCameraSet, realtimeUpdate.ActiveCamera);
        }

        protected override void OnRealtimeCarUpdate(string sender, RealtimeCarUpdate realtimeCarUpdate) {

            var focusedCar = carEntryListService.GetFocusedCar();

            if (focusedCar == null) return;

            var focusedCarId = focusedCar.CarInfo.CarIndex;

            if (realtimeCarUpdate.CarIndex != focusedCarId) return;          

            if (_previousCam != null && !_previousCam.IsActive && TVCamLearningProgress < 1f && lastFocusedCarId == focusedCarId) {
                TVCameraModel oldTVCam = null;
                TVCameraModel currentTVCam = null;

                if (_previousCam.CamType == CamTypeEnum.Tv1 || _previousCam.CamType == CamTypeEnum.Tv2) oldTVCam = _previousCam as TVCameraModel;
                if (CurrentCam.CamType == CamTypeEnum.Tv1 || CurrentCam.CamType == CamTypeEnum.Tv2) currentTVCam = CurrentCam as TVCameraModel;

                // Looks like we can update the end of the old cam
                // BUT we need to make sure it's the one before the current cam
                if (currentTVCam != null) {
                    if (currentTVCam.PrevCam == oldTVCam) {
                        // now we can learn where the new one begins
                        if (currentTVCam.SplinePosStart < 0f)
                            currentTVCam.SetEntry(focusedCar.SplinePosition, focusedCar.Kmh);

                        // Additionally, the old cam may learn the exit
                        if (oldTVCam.SplinePosEnd < 0f)
                            oldTVCam.SetExit(lastFocusedCarSplinePosition, lastFocusedCarSpeed);

                        var oldProgress = TVCamLearningProgress;
                        UpdateTVCamLearningProgress();
                        if (oldProgress != TVCamLearningProgress && TVCamLearningProgress == 1f)
                            SaveTVCameraDefs(TVCameraSets.SelectMany(x => x.Value), trackDataService.TrackDataModel.TrackName);
                    }
                }
            }

            lastFocusedCarSplinePosition = focusedCar.SplinePosition;
            lastFocusedCarSpeed = focusedCar.Kmh;
            lastFocusedCarId = focusedCarId;
        }       

        public void AddTVCamera(string camSet, TVCameraModel cam) {
            if (!TVCameraSets.ContainsKey(camSet)) TVCameraSets.Add(camSet, new List<TVCameraModel>());
            TVCameraSets[camSet].Add(cam);

            if (TVCameraSets[camSet].Count > 1)
                TVCameraSets[camSet].First().PrevCam = TVCameraSets[camSet].Last();
        }

        public void TryLoadTVCameraDefs(string track) {

            var tVCameraSets = TVCameraSets.SelectMany(x => x.Value);

            if (!System.IO.File.Exists($"CamDefs/{track}.json"))
                return;

            var json = System.IO.File.ReadAllText($"CamDefs/{track}.json");
            try {
                var camDefs = JsonSerializer.Deserialize<IEnumerable<TVCameraModel>>(json);
                foreach (var c in camDefs) Debug.WriteLine(c.CameraSetName + " " + c.CameraName);
                foreach (var item in tVCameraSets) {
                    var camDef = camDefs.FirstOrDefault(x => string.Equals(x.CameraSetName, item.CameraSetName) && string.Equals(x.CameraName, item.CameraName));
                    if (camDef != null)
                        item.UpdateModel(camDef);
                }
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void SaveTVCameraDefs(IEnumerable<TVCameraModel> tVCameraSets, string track) {
            var json = JsonSerializer.Serialize(tVCameraSets);
            if (!System.IO.Directory.Exists("CamDefs"))
                System.IO.Directory.CreateDirectory("CamDefs");
            System.IO.File.WriteAllText($"CamDefs/{track}.json", json);
        }

        public void UpdateTVCamLearningProgress() {

            int tvCams = 0;
            int finalizedTvCams = 0;

            foreach (var item in TVCameraSets.SelectMany(x => x.Value)) {
                if (item.SplinePosEnd > -1f && item.SplinePosStart > -1f) finalizedTvCams++;
                tvCams++;
            }

            if (tvCams > 0) TVCamLearningProgress = finalizedTvCams / (float)tvCams;
            else TVCamLearningProgress = 0;
            Debug.WriteLine("Learning process " + TVCamLearningProgress);
        }
    }
}
