using ACCAssistedDirector.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Assistant.Interfaces {
    public interface IMLModelBuilder {
        public void Train();
        public void LoadTrainingData(string path);
        public void SaveModel(string path);
        public bool LoadModel(string path);
    }
}
