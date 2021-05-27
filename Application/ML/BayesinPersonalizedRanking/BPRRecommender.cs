using Application.Assistant.Interfaces;
using NumSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Application.ML.BayesinPersonalizedRanking {
    class BPRRecommender : IMLPredictor {

        private NDArray uFactors;
        private int numFeatures;

        public float Predict(float[] featureValues) {
            var features = new NDArray(featureValues);
            return np.dot(uFactors, features).astype(typeof(float));
        }

        public bool LoadModel(string path) {
            try {
                if (!File.Exists(path)) return false;

                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
                numFeatures = reader.ReadInt32();
                uFactors = new NDArray(typeof(float), numFeatures);
                for (int i = 0; i < numFeatures; i++) uFactors[i] = reader.ReadSingle();
                reader.Dispose();
                return true;
            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}
