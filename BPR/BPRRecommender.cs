using NumSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BPR {
    public class BPRRecommender {

        private NDArray uFactors;
        private int numFeatures;

        public float Predict(float[] featureValues) {
            var features = new NDArray(featureValues);
            return np.dot(uFactors, features);
        }

        public bool LoadModel(string path) {

            if (!File.Exists(path)) return false;

            BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
            numFeatures = reader.ReadInt32();
            uFactors = new NDArray(typeof(float), numFeatures);
            for (int i = 0; i < numFeatures; i++) uFactors[i] = reader.ReadSingle();
            reader.Close();
            return true;                       
        }
    }
}
