using ACCAssistedDirector.Core.Assistant;
using ACCAssistedDirector.Core.Assistant.Interfaces;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Infrastructure.Analytics {
    class CarFocusTracker {
        
        private TelemetryClient _telemetryClient;
        private IDirectorAssistant _directorAssistant;

        private int _carTrainingCycles;
        private int suggestedCount;
        private int totalCount;

        public CarFocusTracker(TelemetryClient telemetryClient, IDirectorAssistant directorAssistant) {
            _telemetryClient = telemetryClient;
            _directorAssistant = directorAssistant;
            _carTrainingCycles = ReadTrainingCycles();
        }

        public void OnSetFocus(int carIdx, bool autoDirectorChangedFocus) {

            if (!autoDirectorChangedFocus) {
                bool suggested = IsSuggested(carIdx);

                var properties = new Dictionary<string, string>
                    {{"Autopilot", _directorAssistant.IsAutoPilotActive.ToString()}};
                var metrics = new Dictionary<string, double>
                    {{"Suggested", suggested ? 1 : 0}, {"TrainingCycles", _carTrainingCycles} };

                // Send the event
                _telemetryClient.TrackEvent("SetFocus", properties, metrics);

                totalCount += 1;
                if (suggested) suggestedCount += 1;
                //System.Diagnostics.Trace.WriteLine("focus: " + suggestedCount + "/" + totalCount);
            }
        }

        public bool IsSuggested(int carIdx) {           
            try {
                if (_directorAssistant == null) return false;

                var tips = _directorAssistant.DirectorTips;

                if (tips == null) return false;

                foreach (var t in tips) {
                    if (t.CarTip.Tip.CarInfo.CarIndex == carIdx) return true;
                }
                return false;
            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                return false;
            }
        }

        private int ReadTrainingCycles() {
            if (File.Exists("Models/CarModel.dat")) {
                try {

                    BinaryReader reader = new BinaryReader(File.Open("Models/CarModel.dat", FileMode.Open));

                    int n = reader.ReadInt32();
                    reader.BaseStream.Position += 4 * n;
                    var trainingCycles = reader.ReadInt32();

                    reader.Close();

                    return trainingCycles;

                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                }
            }
            return 0;
        }
    }
}
