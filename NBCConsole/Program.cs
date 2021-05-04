using ACCAssistedDirector.Core.Assistant;
using ACCAssistedDirector.Core.Services.Interfaces;
using Application.ML.NaiveBayesClassifier;
using Infrastructure.FileIO;
using System;

namespace NBCConsole {
    class Program {
        static void Main(string[] args) {
            var csv = new CSVHandler<CamFeatureVector>();
            NBCModelBuilder nbc = new NBCModelBuilder(csv);
            nbc.LoadTrainingData("C:/Dev/ACCAssistedDirector/ACCAssistedDirector.Wpf/bin/Debug/netcoreapp3.1/Dataset/CamsAll.csv");
            nbc.LoadModel("C:/Users/gvann/Desktop/aaaa");
            nbc.Train();
            nbc.SaveModel("C:/Users/gvann/Desktop/aaaa");
            //NBCClassifier nbcClass = new NBCClassifier("C:/Dev/ACCAssistedDirector/NBCConsole/bin/Debug/netcoreapp3.1/CamModel.dat");
        }
    }
}
