using ACCAssistedDirector.Core.Assistant.Interfaces;
using Domain.Models;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Infrastructure.Analytics {
    class CamTracker {

        private TelemetryClient _telemetryClient;
        private IDirectorAssistant _directorAssistant;

        private int _camTrainingCycles;
        private int suggestedCount;
        private int totalCount;

        public CamTracker(TelemetryClient telemetryClient, IDirectorAssistant directorAssistant) {
            _telemetryClient = telemetryClient;
            _directorAssistant = directorAssistant;
            _camTrainingCycles = ReadTrainingCycles();
        }

        public void OnSetCamera(CameraModel cam, int carIdx, bool autoDirectorChangedCamera) {
            if (!autoDirectorChangedCamera) {

                bool suggested = false;
                if(carIdx > 0)
                    suggested = IsSuggested(cam, carIdx);

                var properties = new Dictionary<string, string>
                    {{"Autopilot", _directorAssistant.IsAutoPilotActive.ToString()}};
                var metrics = new Dictionary<string, double>
                    {{"Suggested", suggested ? 1 : 0}, {"TrainingCycles", _camTrainingCycles} };

                // Send the event
                _telemetryClient.TrackEvent("SetCamera", properties, metrics);

                totalCount += 1;
                if (suggested) suggestedCount += 1;
                //System.Diagnostics.Trace.WriteLine("cams: " + suggestedCount + "/" + totalCount);
            }
        }

        private bool IsSuggested(CameraModel cam, int carIdx) {                    
            try {
                if (_directorAssistant == null) return false;

                var tips = _directorAssistant.DirectorTips;

                if (tips == null) return false;

                foreach (var t in tips) {
                    var tippedCar = t.CarTip.Tip;
                    if (tippedCar.CarInfo.CarIndex == carIdx) {
                        var camTip = t.CamTips[0].Tip;

                        if (camTip == null) continue;

                        if (camTip.CamType == cam.CamType) return true;
                    }
                }
                return false;
            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                return false;
            }
        }

        private int ReadTrainingCycles() {
            try {

                if (File.Exists("Models/CamModelUpdateData.dat")) {
                    BinaryReader reader = new BinaryReader(File.Open("Models/CamModelUpdateData.dat", FileMode.Open));

                    int numCamtypes = reader.ReadInt32();
                    int numFeatures = reader.ReadInt32();

                    //reader.BaseStream.Position += numCamtypes + (numCamtypes * numFeatures * 3);

                    reader.BaseStream.Position += 12*numCamtypes + 3*(numCamtypes * (4 + numFeatures*8));

                    var trainingCycles = reader.ReadInt32();
                    reader.Close();
                    return trainingCycles;
                }

            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }

            return 0;
        }
    }
}
