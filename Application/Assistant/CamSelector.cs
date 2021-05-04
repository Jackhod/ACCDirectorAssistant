using Domain.Enums;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ACCAssistedDirector.Core.Assistant {
    public class CamSelector {
        public Dictionary<CamTypeEnum, float> CamScores { get; private set; }
        
        Random R = new Random();     

        public TipModel<CameraModel> GetPreferredCamera(CarUpdateModel maxRankCar, CameraModel currentCam, float trackMeters, DateTime lastCameraChange, DateTime lastCameraSetChange, Dictionary<CamTypeEnum, DateTime> camTypeLastActive, IEnumerable<CameraModel> cameras, IEnumerable<TVCameraModel> TVCameraSets, float TVCamLearningProgress) {

            if (TVCamLearningProgress < 1) {
                var forcedCamSet = GetForcedCameraSet(currentCam, TVCameraSets); //If the learning process isn't over we just stick to the tv cameras
                return new TipModel<CameraModel>() { Tip = forcedCamSet, Score = 1f };
            }

            var OFFSET = 0.001f;
            var pitlaneFactor = maxRankCar.CarLocation == CarLocationEnum.Pitlane ? 0f : 1f; //prevents the camera to select a car in the pitlane          
            var cameraChangedBeforeSeconds = (float)(DateTime.Now - lastCameraChange).TotalSeconds;
            var camClippingFactor = Math.Min(Math.Max(cameraChangedBeforeSeconds - 3f, 0f), 7f) / 7f + OFFSET; //if the camera changed recently reduces the chances of a new change too soon             
            var camSetActiveSinceMinutes = Convert.ToSingle(Math.Max((DateTime.Now - lastCameraSetChange).TotalMinutes, 0.5) - 0.5);
            var lastCamSetSwitchSeconds = Convert.ToSingle((DateTime.Now - lastCameraSetChange).TotalSeconds);

            if (CamScores == null) CamScores = new Dictionary<CamTypeEnum, float>();
            foreach (var camType in Enum.GetValues(typeof(CamTypeEnum)).Cast<CamTypeEnum>()) {

                if (camType == CamTypeEnum.Unknown) continue;

                var isCurrentCam = camType == currentCam?.CamType;
                var thisCamLastActiveMinutes = (DateTime.Now - camTypeLastActive[camType]).TotalMinutes;
                float camScore = 0f;

                switch (camType) {
                    case CamTypeEnum.Tv1:
                        camScore = EvalTVCamScores(TVCameraSets, camType, maxRankCar, currentCam, trackMeters) * pitlaneFactor;
                        if (maxRankCar.CarsAroundMe30m > 1) camScore *= 1f - Math.Min(maxRankCar.CarsAroundMe30m - 1, 5) / 5f;
                        break;
                    case CamTypeEnum.Tv2:
                        camScore = EvalTVCamScores(TVCameraSets, camType, maxRankCar, currentCam, trackMeters) * pitlaneFactor;
                        if (maxRankCar.CarsAroundMe30m == 0) camScore *= 0.5f;
                        if (maxRankCar.CarsAroundMe30m == 1) camScore *= 0.75f;
                        break;
                    case CamTypeEnum.Helicam:
                        camScore = 0.8f * pitlaneFactor;
                        if (maxRankCar.CarsAroundMe30m > 2) camScore = Math.Min(maxRankCar.CarsAroundMe30m, 5) / 5f;
                        else camScore *= 0.002f;
                        break;
                    case CamTypeEnum.Onboard:
                        var onboardScore = 0f;
                        if (maxRankCar.GapFrontSeconds < 0.5 && maxRankCar.GapRearSeconds > 2f && maxRankCar.GapFrontMeters > 4f)
                            onboardScore = 1f - Math.Min(Math.Max(maxRankCar.GapFrontSeconds - 1f, 0f), 1.5f) / 1.5f;
                        camScore = Math.Max(onboardScore * pitlaneFactor, 0.21f);
                        if (thisCamLastActiveMinutes < 5.0 && !isCurrentCam) camScore *= Convert.ToSingle(thisCamLastActiveMinutes / 5.0);
                        break;
                    case CamTypeEnum.RearWing:
                        var rearViewScore = 0f;
                        if (maxRankCar.GapFrontSeconds > 1 && maxRankCar.GapRearSeconds < 2.0f && maxRankCar.GapRearMeters > 4)
                            rearViewScore = 1f - Math.Min(Math.Max(maxRankCar.GapRearSeconds - 1f, 0f), 1.0f) / 1.0f;
                        camScore = rearViewScore * pitlaneFactor;
                        if (thisCamLastActiveMinutes < 5.0 && !isCurrentCam) camScore *= Convert.ToSingle(thisCamLastActiveMinutes / 5.0);
                        break;
                    case CamTypeEnum.Pitlane:
                        camScore = -pitlaneFactor;
                        break;
                }
                if (thisCamLastActiveMinutes < 5.0 && !isCurrentCam) camScore *= Math.Min(Convert.ToSingle(thisCamLastActiveMinutes / 5.0), 1f);
                if (camClippingFactor < 1f) camScore *= camClippingFactor;

                if (isCurrentCam && maxRankCar.HasFocus) {
                    var camIsGettingOldFactor = 1f;
                    if (camType == CamTypeEnum.Tv1 || camType == CamTypeEnum.Tv2)
                        camIsGettingOldFactor = 1f - Math.Min(Math.Max(camSetActiveSinceMinutes - 0.5f, 0f) / 3f, 1f);
                    else
                        camIsGettingOldFactor = 1f - Math.Min(Math.Max(camSetActiveSinceMinutes - 0.3f, 0f) / 2f, 1f);
                    camScore *= camIsGettingOldFactor;
                }

                if (!isCurrentCam && maxRankCar.HasFocus) {
                    if (lastCamSetSwitchSeconds < 20) {
                        var camIsYoungFactor = 1f;
                        if (currentCam?.CamType == CamTypeEnum.Tv1 || currentCam?.CamType == CamTypeEnum.Tv2)
                            camIsYoungFactor = Math.Min(Math.Max(lastCamSetSwitchSeconds - 10f, 0f) / 30f, 1f);
                        else
                            camIsYoungFactor = Math.Min(Math.Max(lastCamSetSwitchSeconds - 5f, 0f) / 15f, 1f);
                        camScore = Math.Min(camIsYoungFactor + camScore, 1f);
                    }
                }

                if (!maxRankCar.HasFocus) {
                    if (camType == currentCam?.CamType)
                        camScore = 0f;
                }

                CamScores[camType] = camScore;
            }

            CameraModel preferredCamera;
            var winner = CamScores.OrderByDescending(x => x.Value).First();
            if (winner.Key == currentCam?.CamType || winner.Value < 0.1)
                preferredCamera = currentCam;
            else {
                var candidates = cameras.Where(x => x.CamType == winner.Key).ToArray();
                if (!candidates.Any()) preferredCamera = null;
                else preferredCamera = candidates[R.Next(candidates.Length)]; //selects a random camera of the winner type   

            }
             return new TipModel<CameraModel>() { Tip = preferredCamera, Score = 1f };
        }

        public float EvalTVCamScores(IEnumerable<TVCameraModel> TVCameraSets, CamTypeEnum cameraType, CarUpdateModel car, CameraModel currentCam, float trackMeters) {

            if (TVCameraSets == null) return 1f;

            //the camera that frames the car
            var potentialTVCam = TVCameraSets.FirstOrDefault(c => c.CamType == cameraType && c.SplinePosStart < car.SplinePosition && c.SplinePosEnd > car.SplinePosition);
            if (potentialTVCam == null) return 0.1f;

            //min score is when there are 2s left in the frame
            //max score is when there are 8s left in the frame
            var timeToEndCamera = (potentialTVCam.SplinePosEnd - car.SplinePosition) * trackMeters / (car.Kmh / 3.6f);
            var score = Math.Min(Math.Max(timeToEndCamera - 2, 0), 6) / 6f;

            if (currentCam == potentialTVCam) return Math.Max(0.8f, score);

            if (car.Kmh < 20) return 0.1f;

            return score;
        }

        public TVCameraModel GetForcedCameraSet(CameraModel currentCam, IEnumerable<TVCameraModel> TVCameraSets) {
            if (TVCameraSets != null) {
                foreach (var tvCam in TVCameraSets) {
                    if (tvCam.SplinePosEnd < 0f || tvCam.SplinePosStart < 0f) {
                        if (tvCam.CameraSetName == currentCam?.CameraSetName) return null;
                        else return tvCam;
                    }
                }
            }
            return null;
        }
    }
}
