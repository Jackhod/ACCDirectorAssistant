using System;
using System.Collections.Generic;
using System.Text;
using NumSharp;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using ACCAssistedDirector.Core.Assistant;
using ACCAssistedDirector.Core.Services.Interfaces;
using System.Linq;
using Application.Assistant.Interfaces;
using Domain.Models;

namespace Application.ML.NaiveBayesClassifier {
    public class NBCModelBuilder : IMLModelBuilder {

        private List<CamFeatureVector> trainingData;
        private Dictionary<CamTypeEnum, NDArray> classifierDict;
        
        private int _numFeatures;
        private bool _modelLoaded;
        private int _trainingCycles;

        //data stored to update mean and variance of each feature
        private Dictionary<CamTypeEnum, long> numSamples;
        private Dictionary<CamTypeEnum, double[]> featuresDelta;
        private Dictionary<CamTypeEnum, double[]> featuresMsq;
        private Dictionary<CamTypeEnum, double[]> featuresMean;

        private ICSVHelperService<CamFeatureVector> _camFeaturesDataReader;
        private bool updateDataFound;

        public NBCModelBuilder(ICSVHelperService<CamFeatureVector> camFeaturesDataReader) {
            classifierDict = new Dictionary<CamTypeEnum, NDArray>();
            _camFeaturesDataReader = camFeaturesDataReader;
        }
       
        public void LoadTrainingData(string path) {
            trainingData = _camFeaturesDataReader.ReadFromFile(path).ToList();
        }

        public void Train() {
            try {
                var featuresDict = new Dictionary<CamTypeEnum, List<NDArray>>();

                foreach (var featureVector in trainingData) {
                    if (!featuresDict.ContainsKey(featureVector.Label)) {
                        featuresDict.Add(featureVector.Label, new List<NDArray>());
                    }
                    featuresDict[featureVector.Label].Add(np.array(new float[] { featureVector.CarsAround, featureVector.GapRear, featureVector.GapFront }));
                }

                foreach (var k in featuresDict.Keys) Console.WriteLine(k + " " + featuresDict[k].Count);

                if (_numFeatures == 0) { _numFeatures = typeof(CamFeatureVector).GetProperties().Count() - 1; } //one of the properties is the label            

                InitDictionaries(featuresDict.Keys);

                foreach (var camType in featuresDict.Keys) {

                    int c = 0;
                    foreach (var trainSample in featuresDict[camType]) {
                        for (int f = 0; f < _numFeatures; f++) {
                            featuresDelta[camType][f] = trainSample[f] - featuresMean[camType][f];
                            featuresMean[camType][f] += featuresDelta[camType][f] / (numSamples[camType] + c + 1);
                            featuresMsq[camType][f] += featuresDelta[camType][f] * (trainSample[f] - featuresMean[camType][f]);
                        }
                        c++;
                    }
                    numSamples[camType] += c;
                }

                foreach (var camType in classifierDict.Keys) {
                    for (int f = 0; f < _numFeatures; f++) {
                        classifierDict[camType][2 * f] = featuresMean[camType][f];
                        classifierDict[camType][2 * f + 1] = featuresMsq[camType][f] / (numSamples[camType] - 1);
                    }
                }

                Trace.WriteLine("");
                foreach (var k in classifierDict.Keys) Trace.WriteLine(k + " " + classifierDict[k].ToString());
                //SaveModel();
            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine("\r\n" + ex.StackTrace + "\r\n");
            }
        }

        private void InitDictionaries(IEnumerable<CamTypeEnum> keys) {

            if (!_modelLoaded) {
                numSamples = new Dictionary<CamTypeEnum, long>();
                featuresDelta = new Dictionary<CamTypeEnum, double[]>();
                featuresMsq = new Dictionary<CamTypeEnum, double[]>();
                featuresMean = new Dictionary<CamTypeEnum, double[]>();
            }

            foreach (var camType in keys) {
                classifierDict.Add(camType, np.array(new float[_numFeatures * 2]));

                //if (!_modelLoaded) {
                if(!numSamples.ContainsKey(camType)) 
                    numSamples.Add(camType, (long)0);

                if (!featuresDelta.ContainsKey(camType)) 
                    featuresDelta.Add(camType, new double[_numFeatures]);

                if (!featuresMsq.ContainsKey(camType)) 
                    featuresMsq.Add(camType, new double[_numFeatures]);

                if (!featuresMean.ContainsKey(camType)) 
                    featuresMean.Add(camType, new double[_numFeatures]);

                //}
            }
        }

        public void SaveModel(string directoryPath) {

            BinaryWriter writer;

            //writing the model to be used by the classifier
            try {
                writer = new BinaryWriter(File.Open(directoryPath + "/CamModel.dat", FileMode.Create));
                
                writer.Write(classifierDict.Keys.Count);
                writer.Write(_numFeatures);

                foreach (var classifier in classifierDict) {
                    writer.Write((int)classifier.Key);
                    for (int i = 0; i < _numFeatures * 2; i++) {
                        writer.Write((float)classifier.Value[i]);
                    }
                }

                writer.Dispose();
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }

            //writing the data to be used by this model builder to update the model
            try {
                writer = new BinaryWriter(File.Open(directoryPath + "/CamModelUpdateData.dat", FileMode.Create));
                writer.Write(classifierDict.Keys.Count);
                writer.Write(_numFeatures);

                foreach (var elem in numSamples) {
                    writer.Write((int)elem.Key);
                    writer.Write(elem.Value);
                }

                foreach (var elem in featuresDelta) {
                    writer.Write((int)elem.Key);
                    foreach (var val in elem.Value) writer.Write(val);
                }

                foreach (var elem in featuresMsq) {
                    writer.Write((int)elem.Key);
                    foreach (var val in elem.Value) writer.Write(val);
                }

                foreach (var elem in featuresMean) {
                    writer.Write((int)elem.Key);
                    foreach (var val in elem.Value) writer.Write(val);
                }

                _trainingCycles += 1;
                writer.Write(_trainingCycles);

                writer.Dispose();
            }catch(Exception ex) {
                System.Diagnostics.Trace.Write(ex.StackTrace);
            }
        }

        public bool LoadModel(string directoryPath) {
            try {

                if (File.Exists(directoryPath + "/CamModelUpdateData.dat")) {
                    BinaryReader reader = new BinaryReader(File.Open(directoryPath + "/CamModelUpdateData.dat", FileMode.Open));

                    numSamples = new Dictionary<CamTypeEnum, long>();
                    featuresDelta = new Dictionary<CamTypeEnum, double[]>();
                    featuresMsq = new Dictionary<CamTypeEnum, double[]>();
                    featuresMean = new Dictionary<CamTypeEnum, double[]>();

                    int numCamtypes = reader.ReadInt32();
                    int numFeatures = reader.ReadInt32();

                    for (int i = 0; i < numCamtypes; i++) {
                        CamTypeEnum cam = (CamTypeEnum)reader.ReadInt32();
                        long n = reader.ReadInt64();
                        numSamples.Add(cam, n);
                    }

                    for (int i = 0; i < numCamtypes; i++) {
                        CamTypeEnum cam = (CamTypeEnum)reader.ReadInt32();
                        featuresDelta.Add(cam, new double[numFeatures]);
                        for (int f = 0; f < numFeatures; f++)
                            featuresDelta[cam][f] = reader.ReadDouble();
                    }

                    for (int i = 0; i < numCamtypes; i++) {
                        CamTypeEnum cam = (CamTypeEnum)reader.ReadInt32();
                        featuresMsq.Add(cam, new double[numFeatures]);
                        for (int f = 0; f < numFeatures; f++)
                            featuresMsq[cam][f] = reader.ReadDouble();
                    }

                    for (int i = 0; i < numCamtypes; i++) {
                        CamTypeEnum cam = (CamTypeEnum)reader.ReadInt32();
                        featuresMean.Add(cam, new double[numFeatures]);
                        for (int f = 0; f < numFeatures; f++)
                            featuresMean[cam][f] = reader.ReadDouble();
                    }

                    _trainingCycles = reader.ReadInt32();

                    reader.Dispose();
                    _modelLoaded = true;
                    return true;
                }

            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                _trainingCycles = 0;
                _modelLoaded = false;
                return false;
            }

            _trainingCycles = 0;
            _modelLoaded = false;
            return false;
        }
    }
}
