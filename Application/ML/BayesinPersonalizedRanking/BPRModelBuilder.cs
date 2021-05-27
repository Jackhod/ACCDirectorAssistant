using ACCAssistedDirector.Core.Assistant;
using ACCAssistedDirector.Core.Services.Interfaces;
using Application.Assistant.Interfaces;
using NumSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Application.ML.BayesinPersonalizedRanking {
    class BPRModelBuilder : IMLModelBuilder {

        public List<float[]> TrainData { get; set; }
        public List<float[]> EvaluationData { get; set; }
        public NDArray UFactors { get; set; }
        public float LearningRate { get; set; } = 0.05f;
        public float URegularization { get; set; } = 0.002f;
        public int NumFeatures { get; set; }

        private bool _modelLoaded;
        private int _columnID;
        private int _columnChoice;
        private Dictionary<int, List<NDArray>> itemsDict; // each entry has a list with the cars the user has choosen from, one of them is the selected one
        private Dictionary<int, int> selectionDict; // for each selection ID, the index of the selected car
        private ICSVHelperService<CarFeatures> _carFeaturesDataReader;
        private int _trainingCycles;

        public BPRModelBuilder(ICSVHelperService<CarFeatures> carFeaturesDataReader) {
            _carFeaturesDataReader = carFeaturesDataReader;
        //    //TrainData = trainData;
        //    //NumFeatures = TrainData[0].Length - 2; // last 2 columns are ID and choose
        //    //columnID = NumFeatures;
        //    //columnChoice = NumFeatures + 1;

            //    System.Diagnostics.Debug.WriteLine("start training... ");
            //    //if (!ReadModel()) {
            //    //    UFactors = new NDArray(typeof(float), NumFeatures);
            //    //    Random rnd = new Random();
            //    //    for (int i = 0; i < NumFeatures; i++) UFactors[i] = rnd.NextDouble();
            //    //}
        }

        public void Train() {
            Initialize(TrainData, true);

            foreach (var sample in Draw()) {
                Step(sample);
            }

            System.Diagnostics.Trace.WriteLine(UFactors.ToString());

            //SaveModel();
        }

        public void LoadTrainingData(string path) {
            TrainData = new List<float[]>();

            //var reader = new StreamReader("Dataset/CarsOnlySelection.csv");
            //var csv = new CsvReader(reader, CultureInfo.CurrentCulture);

            //System.Diagnostics.Debug.WriteLine("Reading car training data...");
            //csv.Read();
            //int numColumn = csv.Parser.Count;
            //csv.ReadHeader();

            //System.Diagnostics.Debug.WriteLine(numColumn);
            //while (csv.Read()) {
            //    var record = new float[numColumn];
            //    for (int i = 0; i < numColumn; i++) record[i] = csv.GetField<float>(i);
            //    TrainData.Add(record);
            //}
            var records = _carFeaturesDataReader.ReadFromFile(path).ToList();

            foreach(var r in records) {
                TrainData.Add(r.ToArrayLabeled());
            }

            NumFeatures = TrainData[0].Length - 2; // last 2 columns are ID and choose
            _columnID = NumFeatures;
            _columnChoice = NumFeatures + 1;
        }

        public float Evaluate(List<float[]> evalData) {
            EvaluationData = evalData;
            Initialize(EvaluationData, false);
            int correctAnswers = 0;
            int answers = 0;

            Random rand = new Random();
            var itemsDictRandomized = itemsDict.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value);

            foreach (var item in itemsDictRandomized) {
                float max = -1f;
                int maxId = -1;
                foreach (var car in item.Value) {
                    float u_dot_c = np.dot(UFactors, car);
                    if (u_dot_c > max) {
                        max = u_dot_c;
                        maxId = item.Value.IndexOf(car);
                    }
                }
                if (maxId == selectionDict[item.Key]) correctAnswers++;
                answers++;
            }
            return (float)correctAnswers / answers;
        }

        private void Step(List<NDArray> sample) {
            var estimate = np.dot(UFactors, np.subtract(sample[0], sample[1])); // Xij = Xi - Xj, where Xi and Xj are the estimated values with the user factors
            var z = 1 / (1 + np.exp(estimate));
            var updateU = (z * np.subtract(sample[0], sample[1])) - np.multiply(URegularization, UFactors);
            UFactors = np.add(UFactors, np.multiply(LearningRate, updateU));
        }

        private IEnumerable<List<NDArray>> Draw() {
            var ret = new List<NDArray>();

            var selectionSamples = CreateSelectionSamples();

            Random rand = new Random();
            selectionSamples = selectionSamples.OrderBy(x => rand.Next()).ToList();

            foreach (var choice in selectionSamples) {
                ret.Clear();

                var items = itemsDict[choice[0]];
                ret.Add(items[choice[1]]); //the item chosen by the user
                ret.Add(items[choice[2]]); //the item not chosen by the user
                yield return ret;
            }
        }

        private List<int[]> CreateSelectionSamples() {
            List<int[]> selectionSamples = new List<int[]>();
            foreach (var selection in selectionDict) {
                var items = itemsDict[selection.Key];

                int selectionId = selection.Key;
                int iIdx = selection.Value;
                for (int jIdx = 0; jIdx < items.Count; jIdx++) {
                    if (jIdx != iIdx) selectionSamples.Add(new int[] { selectionId, iIdx, jIdx });
                }
            }
            return selectionSamples;
        }

        private void Initialize(List<float[]> data, bool initUfactor) {

            if (!_modelLoaded) {
                UFactors = new NDArray(typeof(float), NumFeatures);
                Random rnd = new Random();
                for (int i = 0; i < NumFeatures; i++) UFactors[i] = rnd.NextDouble();
            }

            itemsDict = new Dictionary<int, List<NDArray>>();
            selectionDict = new Dictionary<int, int>();
            int idx = 0;
            foreach (var item in data) {

                var featureValues = new float[NumFeatures];
                Array.Copy(item, 0, featureValues, 0, NumFeatures);

                if (!itemsDict.ContainsKey((int)item[_columnID])) {
                    itemsDict.Add((int)item[_columnID], new List<NDArray>());
                    idx = 0;
                }
                itemsDict[(int)item[_columnID]].Add(new NDArray(featureValues));

                if (item[_columnChoice] > 0) selectionDict[(int)item[_columnID]] = idx;

                idx++;
            }
        }

        public void SaveModel(string directoryPath) {
            try {

                BinaryWriter writer = new BinaryWriter(File.Open(directoryPath + "/CarModel.dat", FileMode.Create));
                
                //writing the model file
                writer.Write(NumFeatures);               
                for (int i = 0; i < NumFeatures; i++) 
                    writer.Write((float)UFactors[i]);
                _trainingCycles += 1;
                writer.Write(_trainingCycles);   
                
                writer.Dispose();

            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }
        }

        public bool LoadModel(string directoryPath) {
            if (File.Exists(directoryPath + "/CarModel.dat")) {
                try {

                    BinaryReader reader = new BinaryReader(File.Open(directoryPath + "/CarModel.dat", FileMode.Open));
                    
                    //reading the model file
                    NumFeatures = reader.ReadInt32();
                    UFactors = new NDArray(typeof(float), NumFeatures);
                    for (int i = 0; i < NumFeatures; i++) UFactors[i] = reader.ReadSingle();
                    _trainingCycles = reader.ReadInt32();

                    reader.Dispose();
                    System.Diagnostics.Debug.WriteLine("updating model: " + UFactors.ToString());
                    _modelLoaded = true;
                    return true;

                }catch(Exception ex) {
                    System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                    _trainingCycles = 0;
                    _modelLoaded = false;
                    return false;
                }
            }
            _trainingCycles = 0;
            _modelLoaded = false;
            return false;                       
        }
    }
}
