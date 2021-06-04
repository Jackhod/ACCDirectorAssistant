using Domain.Enums;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACCAssistedDirector.Core.Assistant {

    public enum CarFeaturesEnum { Proximity, Pack, Position, FocusFast, FocusSlow, Pace }

    public class CarSelector {
        public float MaxPressure { get; set; }
        public float AvgPressure { get; set; }
        Dictionary<CarFeaturesEnum, float> WeightDict { get; set; }
        
        CarUpdateModel maxRankCar;

        public CarSelector() {

            WeightDict = Enum.GetValues(typeof(CarFeaturesEnum)).Cast<CarFeaturesEnum>().ToDictionary(x => x, x => 1f);
        }

        public List<TipModel<CarUpdateModel>> GetPreferredCar(List<CarUpdateModel> trackPositionCarList, /*float trackMeters,*/ float currentFocusSeconds, int howMany) {

            List<TipModel<CarUpdateModel>> carScores = new List<TipModel<CarUpdateModel>>();
            foreach(var car in trackPositionCarList) {
                carScores.Add(new TipModel<CarUpdateModel>() { Tip = car, Score = EvaluateCarFeatureValues(car, trackPositionCarList, /*trackMeters,*/ currentFocusSeconds, WeightDict) });
            }
            return carScores.OrderByDescending(c => c.Score).ToList().GetRange(0, howMany);
        }
     
        public float EvaluateCarFeatureValues(CarUpdateModel car, IList<CarUpdateModel> trackPositionCarList, /*float trackMeters,*/ float currentFocusSeconds, IDictionary<CarFeaturesEnum, float> categoryWeights) {
            var OFFSET = 0.001f; // we'll use a (tiny) offset so even a zero doesn't eliminate our information by a zero division; 
            var ONE = 1f + OFFSET;

            var pressure = 1f;
            foreach (var feature in WeightDict.Keys) {

                float value;

                // Special case: Sitting in the pits or so
                if (car.CarLocation == CarLocationEnum.Pitlane) {
                    value = OFFSET;
                } else {
                    switch (feature) {
                        case CarFeaturesEnum.Proximity: {
                                // 1 Proximity - how close is this car to the next one, linearly scaled to 2.5 seconds
                                Random rnd = new Random();
                                float frontBias = 1f;
                                float rearBias = 0.95f;
                                if (rnd.Next(0, 2) == 0) {
                                    frontBias = 0.95f;
                                    rearBias = 1f;
                                }
                                var closestDistance = Math.Min(car.GapFrontSeconds * frontBias, car.GapRearSeconds * rearBias);
                                value = ONE - Math.Min(closestDistance, 2.5f) / 2.5f;
                            }
                            break;
                        case CarFeaturesEnum.Pack: {
                                // PackFactor - how many cars are within ~10 and 30 meters?
                                //car.CarsAroundMe30m = trackPositionCarList.Where(c => Math.Abs(c.SplinePosition - car.SplinePosition) * trackMeters < 30 && c != car).Count();
                                //car.CarsAroundMe10m = trackPositionCarList.Where(c => Math.Abs(c.SplinePosition - car.SplinePosition) * trackMeters < 10 && c != car).Count();
                                value = Math.Min(car.CarsAroundMe30m + car.CarsAroundMe10m, 7) / 7f + OFFSET;
                            }
                            break;
                        case CarFeaturesEnum.Position: {
                                // Race Position
                                var pos = (car.Position + car.TrackPosition) / 2f;
                                value = ONE - (pos / (float)trackPositionCarList.Count);
                            }
                            break;
                        //case CarFeaturesEnum.FocusFast: {
                        //        // 10 Focus seconds - how long since the last focus switch? This is the short time thing which allows (or denies) to quickly jump
                        //        // action if there is something happening like a contact, closing in or whatever
                        //        // First 5s are super critical, we won't really allow a jump there so we scale to 10f - then it's neutral to jump over
                        //        if (!car.HasFocus) {
                        //            value = Math.Min(currentFocusSeconds, 10f) / 10f;
                        //        } else {
                        //            // While we have focus, we signal that it's uncritical to stick with us
                        //            value = 1f;
                        //        }
                        //    }
                        //    break;
                        //case CarFeaturesEnum.FocusSlow: {
                        //        // Opposed to the "Fast" variant, this is about how long we want to generally stick to a car.
                        //        // To not confuse the user, we basically want to aim for at least 30 seconds = 50%
                        //        if (!car.HasFocus) {
                        //            value = Math.Min(currentFocusSeconds, 60f) / 60f;
                        //        } else {
                        //            // While we have focus, we signal that's it's uncritical to stick with us.
                        //            // though we could gradually lower the value when it's becoming way too long
                        //            if (currentFocusSeconds > 5 * 60) {
                        //                // we'll sloooowly reduce this so other cars may get into the focus. After a total of 15minutes the focus will forcibly go away (but most likely earlier)
                        //                value = ONE - Math.Min((Math.Max(currentFocusSeconds - 5f, 0f) * 60f) / 10f * 60f, 0f);
                        //            } else {
                        //                value = 1f;
                        //            }
                        //        }
                        //    }
                        //    break;
                        case CarFeaturesEnum.Pace: {
                                // For pace, we either look at the delta (to express how much we're pushing and maybe hunting) or at the predicted laptime
                                if (car.PredictedLaptime <= 0) {
                                    value = ONE;
                                } else {
                                    var splinePosFactor = car.SplinePosition;
                                    if (car.CrossedTheLineWithFocus)
                                        splinePosFactor = 1f;
                                    splinePosFactor = (splinePosFactor + 1f) / 2f;
                                    if (car.PredictedLaptime < car.SessionBestLap) {
                                        value = 1f * splinePosFactor;
                                    } else {
                                        value = (1f - Math.Min(car.PredictedLaptime - car.SessionBestLap, 1500) / 1500) * splinePosFactor;
                                    }
                                }
                            }
                            break;
                        default: {
                                // Using a 1f default for unhandled categories will make them do (and break) nothing
                                value = 1f;
                            }
                            break;
                    }
                }
                pressure *= value;
            }
            return pressure;
        }

#region TEST
        public List<TipModel<CarUpdateModel>> GetTestPreferredCar(List<CarUpdateModel> trackPositionCarList, /*float trackMeters,*/ float currentFocusSeconds, int howMany) {

            List<TipModel<CarUpdateModel>> carScores = new List<TipModel<CarUpdateModel>>();
            foreach (var car in trackPositionCarList) {
                carScores.Add(new TipModel<CarUpdateModel>() { Tip = car, Score = EvaluateTestCarFeatureValues(car, trackPositionCarList, /*trackMeters,*/ currentFocusSeconds, WeightDict) });
            }
            return carScores.OrderByDescending(c => c.Score).ToList().GetRange(0, howMany);
        }

        public float EvaluateTestCarFeatureValues(CarUpdateModel car, IList<CarUpdateModel> trackPositionCarList, /*float trackMeters,*/ float currentFocusSeconds, IDictionary<CarFeaturesEnum, float> categoryWeights) {
            var OFFSET = 0.001f; // we'll use a (tiny) offset so even a zero doesn't eliminate our information by a zero division; 
            var ONE = 1f + OFFSET;

            var pressure = 1f;
            foreach (var feature in WeightDict.Keys) {

                float value;

                // Special case: Sitting in the pits or so
                if (car.CarLocation == CarLocationEnum.Pitlane) {
                    value = OFFSET;
                } else {
                    switch (feature) {
                        case CarFeaturesEnum.Proximity: {
                                // 1 Proximity - how close is this car to the next one, linearly scaled to 2.5 seconds
                                Random rnd = new Random();
                                float frontBias = 1f;
                                float rearBias = 0.95f;
                                if (rnd.Next(0, 2) == 0) {
                                    frontBias = 0.95f;
                                    rearBias = 1f;
                                }
                                var closestDistance = Math.Min(car.GapFrontSeconds * frontBias, car.GapRearSeconds * rearBias);
                                value = ONE - Math.Min(closestDistance, 2.5f) / 2.5f;
                            }
                            break;
                        case CarFeaturesEnum.Pack: {
                                // PackFactor - how many cars are within ~10 and 30 meters?
                                //car.CarsAroundMe30m = trackPositionCarList.Where(c => Math.Abs(c.SplinePosition - car.SplinePosition) * trackMeters < 30 && c != car).Count();
                                //car.CarsAroundMe10m = trackPositionCarList.Where(c => Math.Abs(c.SplinePosition - car.SplinePosition) * trackMeters < 10 && c != car).Count();
                                value = Math.Min(car.CarsAroundMe30m + car.CarsAroundMe10m, 7) / 7f + OFFSET;
                            }
                            break;
                        case CarFeaturesEnum.Position: {
                                // Race Position
                                var pos = (car.Position + car.TrackPosition) / 2f;
                                value = ONE - (pos / (float)trackPositionCarList.Count);
                            }
                            break;
                        case CarFeaturesEnum.FocusFast: {
                                // 10 Focus seconds - how long since the last focus switch? This is the short time thing which allows (or denies) to quickly jump
                                // action if there is something happening like a contact, closing in or whatever
                                // First 5s are super critical, we won't really allow a jump there so we scale to 10f - then it's neutral to jump over
                                if (!car.HasFocus) {
                                    value = Math.Min(currentFocusSeconds, 10f) / 10f;
                                } else {
                                    // While we have focus, we signal that it's uncritical to stick with us
                                    value = 1f;
                                }
                            }
                            break;
                        case CarFeaturesEnum.FocusSlow: {
                                // Opposed to the "Fast" variant, this is about how long we want to generally stick to a car.
                                // To not confuse the user, we basically want to aim for at least 30 seconds = 50%
                                if (!car.HasFocus) {
                                    value = Math.Min(currentFocusSeconds, 60f) / 60f;
                                } else {
                                    // While we have focus, we signal that's it's uncritical to stick with us.
                                    // though we could gradually lower the value when it's becoming way too long
                                    if (currentFocusSeconds > 5 * 60) {
                                        // we'll sloooowly reduce this so other cars may get into the focus. After a total of 15minutes the focus will forcibly go away (but most likely earlier)
                                        value = ONE - Math.Min((Math.Max(currentFocusSeconds - 5f, 0f) * 60f) / 10f * 60f, 0f);
                                    } else {
                                        value = 1f;
                                    }
                                }
                            }
                            break;
                        case CarFeaturesEnum.Pace: {
                                // For pace, we either look at the delta (to express how much we're pushing and maybe hunting) or at the predicted laptime
                                if (car.PredictedLaptime <= 0) {
                                    value = ONE;
                                } else {
                                    var splinePosFactor = car.SplinePosition;
                                    if (car.CrossedTheLineWithFocus)
                                        splinePosFactor = 1f;
                                    splinePosFactor = (splinePosFactor + 1f) / 2f;
                                    if (car.PredictedLaptime < car.SessionBestLap) {
                                        value = 1f * splinePosFactor;
                                    } else {
                                        value = (1f - Math.Min(car.PredictedLaptime - car.SessionBestLap, 1500) / 1500) * splinePosFactor;
                                    }
                                }
                            }
                            break;
                        default: {
                                // Using a 1f default for unhandled categories will make them do (and break) nothing
                                value = 1f;
                            }
                            break;
                    }
                }
                pressure *= value;
            }
            return pressure;
        }

#endregion
    }
}
