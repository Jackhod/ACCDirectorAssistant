using ACCAssistedDirector.Core.Services;
using Application.Assistant.Interfaces;
using Application.ML.BayesinPersonalizedRanking;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.Assistant {
    public class CarPersonalSelector {

        private IMLPredictor bprRecommender;
        
        public Dictionary<int, CarFeatures> carFeaturesDict { get; set; }

        public bool Init() {
            bprRecommender = new BPRRecommender();
            var modelLoaded = bprRecommender.LoadModel(Path.GetFullPath("Models/CarModel.dat"));
            return modelLoaded;
        }

        public List<TipModel<CarUpdateModel>> GetPreferredCars(List<CarUpdateModel> cars, float currentFocusSeconds, int howMany) {

            List<TipModel<CarUpdateModel>> carScores = new List<TipModel<CarUpdateModel>>();
            foreach (var c in cars) {
                carScores.Add(new TipModel<CarUpdateModel>() { Tip = c, Score = EvaluateCar(c, cars.Count, currentFocusSeconds) });
            }
            return carScores.OrderByDescending(c => c.Score).ToList().GetRange(0, howMany);
        }

        public float EvaluateCar(CarUpdateModel car, int numCars, float currentFocusSeconds) {
            float score = 0.0f;

            var carFeatures = EvaluateCarFeatures(car, numCars, currentFocusSeconds/*, 0*/);
            score = bprRecommender.Predict(carFeatures.ToArray());
            return score;
        }

        public void EvaluateFeatures(ICarEntryListService carEntryListService) {
            carFeaturesDict = new Dictionary<int, CarFeatures>();
            var cars = carEntryListService.CarEntryList;
            var currentFocusSeconds = (float)(DateTime.Now - carEntryListService.LastFocusChange).TotalSeconds;
            foreach (var car in cars) carFeaturesDict.Add(car.CarInfo.CarIndex, EvaluateCarFeatures(car, cars.Count, currentFocusSeconds));
            //return carFeaturesDict;
        }

        private CarFeatures EvaluateCarFeatures(CarUpdateModel car, int numCars, float currentFocusSeconds) {

            var carFeatures = new CarFeatures();

            //proximity
            float closerGap = Math.Min(car.GapFrontSeconds, car.GapRearSeconds);
            carFeatures.Proximity = 1 - Math.Min(closerGap, 1.5f) / 1.5f;

            //cars around
            carFeatures.CarsAroundMidRange = Math.Min(car.CarsAroundMe30m, 7) / (float)Math.Min(numCars, 7);
            carFeatures.CarsAroundCloseRange = Math.Min(car.CarsAroundMe10m, 3) / (float)MathF.Min(numCars, 3);

            //position
            var pos = (car.Position + car.TrackPosition) / 2f;
            carFeatures.Pos = 1 - (pos / (float)numCars);
            
            //pace
            if (car.Delta < 0) carFeatures.Pace = (Math.Min(-car.Delta, 1500) / 1500) * car.SplinePosition;
            else carFeatures.Pace = 0f;

            return carFeatures;
        }

        public void CarSelection(int carIndex, int selectionID) {
            if (carFeaturesDict == null) return;
            foreach(var car in carFeaturesDict) {
                car.Value.SelectionID = selectionID;
                if (car.Key == carIndex) car.Value.Selected = 1;
                else car.Value.Selected = 0;
            }
        }
    }
}
