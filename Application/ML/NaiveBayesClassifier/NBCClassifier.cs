using ACCAssistedDirector.Core.Assistant;
using Application.Assistant.Interfaces;
using Domain.Models;
using NumSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Application.ML.NaiveBayesClassifier {
    public class NBCClassifier : IMLPredictor {

        private Dictionary<CamTypeEnum, NDArray> classifiers;

        //public float Predict(float[] featureVector) {
        //    var predictedCam = Classify(featureVector);
        //    return (float)predictedCam;
        //}

        //public CamTypeEnum Classify(float[] featureVector) {

        //    int numFeatures = featureVector.Length;
        //    float maxPosterior = 0;
        //    CamTypeEnum prediction = CamTypeEnum.Tv2;
        //    foreach(var camType in classifiers.Keys) {

        //        float posterior = 1f;

        //        for(int i = 0; i < numFeatures; i++) {
        //            float featureAvg = classifiers[camType][2 * i];
        //            float featureVar = classifiers[camType][2 * i + 1];

        //            var pfeatureCamType = 1/MathF.Sqrt(2*MathF.PI*featureVar) * MathF.Exp(-MathF.Pow((featureVector[i] - featureAvg),2) / (2 * featureVar));
        //            posterior *= pfeatureCamType;
        //        }

        //        if(posterior > maxPosterior) {
        //            maxPosterior = posterior;
        //            prediction = camType;
        //        }
        //    }

        //    return prediction;
        //}

        //TEST
        public float Predict(float[] featureVector) {
            var camType = (CamTypeEnum)featureVector[featureVector.Length - 1];
            float[] features = new float[featureVector.Length - 1];
            for (int i = 0; i < featureVector.Length - 1; i++) features[i] = featureVector[i];
            var predictedCam = Classify(camType, features);
            return (float)predictedCam;
        }

        //TEST
        public float Classify(CamTypeEnum camType, float[] featureVector) {

            if (!classifiers.ContainsKey(camType)) return -1;

            int numFeatures = featureVector.Length;
            float posterior = 1f;

            for (int i = 0; i < numFeatures; i++) {
                float featureAvg = classifiers[camType][2 * i];
                float featureVar = classifiers[camType][2 * i + 1];

                var pfeatureCamType = 1 / MathF.Sqrt(2 * MathF.PI * featureVar) * MathF.Exp(-MathF.Pow((featureVector[i] - featureAvg), 2) / (2 * featureVar));
                posterior *= pfeatureCamType;
            }

            return posterior;
        }

        public bool LoadModel(string path) {
            try {
                if (!File.Exists(path)) return false;

                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));

                classifiers = new Dictionary<CamTypeEnum, NDArray>();
                int numCamtypes = reader.ReadInt32();
                int numFeatures = reader.ReadInt32();

                for (int i = 0; i < numCamtypes; i++) {

                    CamTypeEnum cam = (CamTypeEnum)reader.ReadInt32();
                    classifiers.Add(cam, np.array(new float[numFeatures * 2]));

                    for (int j = 0; j < numFeatures * 2; j++) {
                        classifiers[cam][j] = reader.ReadSingle();
                    }
                }

                reader.Close();
                return true;
            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}
