using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using NumSharp;

namespace BPR {
    public class BPRModelBuilder {

        public List<float[]> TrainData {get; set;}
        public List<float[]> EvaluationData { get; set; }
        public NDArray UFactors { get; set; }
        public float LearningRate { get; set; } = 0.05f;
        public float URegularization { get; set; } = 0.002f;
        public int NumFeatures { get; set; }

        private int columnID;
        private int columnChoice;
        private Dictionary<int, List<NDArray>> itemsDict; // each entry has a list with the cars the user has choosen from, one of them is the selected one
        private Dictionary<int, int> selectionDict; // for each selection ID, the index of the selected car

        public BPRModelBuilder(List<float[]> trainData) {
            TrainData = trainData;
            NumFeatures = TrainData[0].Length - 2; // last 2 columns are ID and choose
            columnID = NumFeatures;
            columnChoice = NumFeatures + 1;
            Debug.WriteLine("start training... ");
            if (!ReadModel()) {
                UFactors = new NDArray(typeof(float), NumFeatures);                
                Random rnd = new Random();
                for (int i = 0; i < NumFeatures; i++) UFactors[i] = rnd.NextDouble();
            }
        }

        public void Train() {
            Initialize(TrainData, true);

            foreach (var sample in Draw()) {
                Step(sample);
            }

            Debug.WriteLine(UFactors.ToString());

            SaveModel();
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
                foreach(var car in item.Value) {
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
            //int iIdx;
            //int jIdx;

            //Random rand = new Random();
            //selectionDict = selectionDict.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value);

            //foreach (var sel in selectionDict) { // We sample the train data choosing a selected item (I) and a not-selected item (J) 
            //    ret.Clear();
            //    var items = itemsDict[sel.Key];

            //    iIdx = sel.Value;
            //    do jIdx = rnd.Next(0, items.Count); 
            //    while (jIdx == iIdx); // item j is selected randomly between the ones that weren't choose

            //    ret.Add(items[iIdx]);
            //    ret.Add(items[jIdx]);

            //    yield return ret;
            //}

            var selectionSamples = CreateSelectionSamples();

            Random rand = new Random();
            selectionSamples = selectionSamples.OrderBy(x => rand.Next()).ToList(); 

            foreach(var choice in selectionSamples) {
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
            itemsDict = new Dictionary<int, List<NDArray>>();
            selectionDict = new Dictionary<int, int>();
            int idx = 0;
            foreach (var item in data) {

                var featureValues = new float[NumFeatures];
                Array.Copy(item, 0, featureValues, 0, NumFeatures);

                if (!itemsDict.ContainsKey((int)item[columnID])) {
                    itemsDict.Add((int)item[columnID], new List<NDArray>());
                    idx = 0;
                }
                itemsDict[(int)item[columnID]].Add(new NDArray(featureValues));

                if (item[columnChoice] > 0) selectionDict[(int)item[columnID]] = idx;

                idx++;
            }
        }

        public void SaveModel() {
            BinaryWriter writer = new BinaryWriter(File.Open("Models/carmodelnew.dat", FileMode.Create));
            writer.Write(NumFeatures);
            for (int i = 0; i < NumFeatures; i++) writer.Write((float)UFactors[i]);
            writer.Close();
        }

        public bool ReadModel() {
            if (File.Exists("Models/carmodelnew.dat")) {
                BinaryReader reader = new BinaryReader(File.Open("Models/carmodelnew.dat", FileMode.Open));
                NumFeatures = reader.ReadInt32();
                UFactors = new NDArray(typeof(float), NumFeatures);
                for (int i = 0; i < NumFeatures; i++) UFactors[i] = reader.ReadSingle();
                reader.Close();
                Debug.WriteLine("updating model: " + UFactors.ToString());
                return true;
            }
            return false;
        }

        //public void ReadModel(string path) {
        //    if (File.Exists(path)) {
        //        BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
        //        NumFeatures = reader.ReadInt32();
        //        UFactors = new NDArray(typeof(float), NumFeatures);
        //        for (int i = 0; i < NumFeatures; i++) UFactors[i] = reader.ReadSingle();
        //        reader.Close();
        //    }
        //}
    }
}
