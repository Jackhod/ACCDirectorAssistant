using ACCAssistedDirector.Core.Services;
using Application.Assistant.Interfaces;
using Application.ML.NaiveBayesClassifier;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.Assistant {
    public class CamPersonalSelector {
        Random R;

        IMLPredictor nbcClassifier;

        public bool Init() {
            //bprRecommender = new BPRRecommender(Path.GetFullPath("Models/CamModel.dat"));
            R = new Random();
            nbcClassifier = new NBCClassifier();
            var modelLoaded = nbcClassifier.LoadModel("Models/CamModel.dat");
            return modelLoaded;
        }

        public TipModel<CameraModel> GetCam(ICameraService camService, CarUpdateModel car) {
            var featureVector = new CamFeatureVector() {
                CarsAround = car.CarsAroundMe30m,
                GapRear = Math.Min(car.GapRearSeconds, 5f),
                GapFront = Math.Min(car.GapFrontSeconds,5f)
            };

            //var camType = nbcClassifier.Classify(featureVector);
            var camType = (CamTypeEnum)nbcClassifier.Predict(featureVector.ToArray());
            var cameras = camService.CameraSets.SelectMany(x => x.Value);
            var candidates = cameras.Where(x => x.CamType == camType).ToArray();
            
            if (candidates.Any()) return new TipModel<CameraModel>() { Tip = candidates[R.Next(candidates.Length)], Score = 1f };
            else return null;
        }

        public List<TipModel<CameraModel>> GetPreferredCams(ICameraService camService, CarUpdateModel car, int howMany) {
            List<TipModel<CameraModel>> camScores = new List<TipModel<CameraModel>>();

            var featureVector = new CamFeatureVector() {
                CarsAround = car.CarsAroundMe30m,
                GapRear = Math.Min(car.GapRearSeconds, 5f),
                GapFront = Math.Min(car.GapFrontSeconds, 5f)
            };
            var features = featureVector.ToArray();
            var camAndFeatures = new float[features.Length + 1];
            var cameras = camService.CameraSets.SelectMany(x => x.Value);

            var camTypes = Enum.GetValues(typeof(CamTypeEnum)).Cast<CamTypeEnum>();
            foreach (var camType in camTypes) {                               
                features.CopyTo(camAndFeatures, 0);
                camAndFeatures[camAndFeatures.Length - 1] = (float)camType;
               
                var candidates = cameras.Where(x => x.CamType == camType).ToArray();
                camScores.Add(new TipModel<CameraModel>() { Tip = candidates[R.Next(candidates.Length)], Score = nbcClassifier.Predict(camAndFeatures) });
            }
            return camScores.OrderByDescending(c => c.Score).ToList().GetRange(0, howMany);
        }
    }
}
