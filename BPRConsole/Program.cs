using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ACCAssistedDirector.Core.Assistant;
using BPR;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace BPRConsole {
    class Program {
        static void Main(string[] args) {

            List<float[]> trainData = new List<float[]>();
            List<float[]> evalData = new List<float[]>();

            //var focusChangeReader = new StreamReader("C:/Dev/ACCAssistedDirector/ACCAssistedDirector.Wpf/bin/Debug/netcoreapp3.1/Dataset/CarsOnlySelection.csv");
            //var focusChangeCsv = new CsvReader(focusChangeReader, CultureInfo.CurrentCulture);
            //Console.WriteLine("reading timings...");
            //var records = focusChangeCsv.GetRecords<CarFeatures>();
            //int c = 0;
            //float t = 0;
            //foreach(var r in records) {
            //    t += r.SinceLastFocusSlow;
            //    c++;
            //}
            //var avg = t / c;
            //Console.WriteLine("avg switch time: " + avg);

            //var reader = new StreamReader("C:/Dev/ACCAssistedDirector/ACCAssistedDirector.Wpf/bin/Debug/netcoreapp3.1/Dataset/CarsAll.csv");
            //var csv = new CsvReader(reader, CultureInfo.CurrentCulture);
            //Console.WriteLine("Reading training data...");
            //var trainingRecords = csv.GetRecords<CarFeatures>();
            //var timeScale = avg * 2;
            //foreach(var r in trainingRecords) {
            //    //if(r.Selected == 1) {
            //    //    r.SinceLastFocusSlow = 1 - Math.Min(r.SinceLastFocusSlow, timeScale) / timeScale;
            //    //} else {
            //    //    r.SinceLastFocusSlow = Math.Min(r.SinceLastFocusSlow, timeScale) / timeScale;
            //    //}
            //    trainData.Add(r.ToArrayLabeled());
            //}

            //Console.WriteLine("Training...");
            //var bpr = new BPR.BPRModelBuilder(trainData);
            //bpr.Train();

            //evalData = trainData;
            //Console.WriteLine("Evaluating...");
            //var evaluation = bpr.Evaluate(evalData);
            //Console.WriteLine("Evaluation: " + evaluation);

            //for (int i = 0; i < 100; i++) {
            //    Console.WriteLine("Training...");
            //    bpr.Train();
            //    Console.WriteLine("Evaluating...");
            //    evaluation = bpr.Evaluate(evalData);
            //    Console.WriteLine("Evaluation: " + evaluation);
            //}




            var reader = new StreamReader("C:/Dev/ACCAssistedDirector/ACCAssistedDirector.Wpf/bin/Debug/netcoreapp3.1/Dataset/CarsAll.csv");
            var csv = new CsvReader(reader, CultureInfo.CurrentCulture);
            Console.WriteLine("Reading training data...");
            csv.Read();
            int numColumn = csv.Parser.Count;
            csv.ReadHeader();
            Console.WriteLine(numColumn);
            while (csv.Read()) {
                var record = new float[numColumn];
                for (int i = 0; i < numColumn; i++) record[i] = csv.GetField<float>(i);
                trainData.Add(record);
            }

            Console.WriteLine("Training...");
            var bpr = new BPR.BPRModelBuilder(trainData);
            bpr.Train();

            reader = new StreamReader("C:/Dev/ACCAssistedDirector/ACCAssistedDirector.Wpf/bin/Debug/netcoreapp3.1/Dataset/CarsOnlySelection.csv");
            csv = new CsvReader(reader, CultureInfo.CurrentCulture);
            Console.WriteLine("Reading evaluation data...");
            csv.Read();
            numColumn = csv.Parser.Count;
            csv.ReadHeader();
            Console.WriteLine(numColumn);
            while (csv.Read()) {
                var record = new float[numColumn];
                for (int i = 0; i < numColumn; i++) record[i] = csv.GetField<float>(i);
                evalData.Add(record);
            }
            Console.WriteLine("Evaluating...");
            var evaluation = bpr.Evaluate(evalData);
            Console.WriteLine("Evaluation: " + evaluation);

            for (int i = 0; i < 100; i++) {
                Console.WriteLine("Training...");
                bpr.Train();
                Console.WriteLine("Evaluating...");
                evaluation = bpr.Evaluate(evalData);
                Console.WriteLine("Evaluation: " + evaluation);
            }
        }
    }
}
