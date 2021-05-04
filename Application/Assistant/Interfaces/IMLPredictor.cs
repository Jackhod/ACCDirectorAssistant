using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Assistant.Interfaces {
    public interface IMLPredictor {
        public float Predict(float[] featuresVector);
        public bool LoadModel(string path);
    }
}
