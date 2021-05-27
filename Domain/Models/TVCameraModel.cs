using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Domain.Models {
    public class TVCameraModel : CameraModel {
        public float SplinePosStart { get; set; }
        public float SplinePosEnd { get; set; }
        public int EntrySpeed { get; set; }
        public int ExitSpeed { get; set; }

        [JsonIgnore]
        public TVCameraModel PrevCam { get; set; }

        private TVCameraModel() : base() { }

        public TVCameraModel(string set, string name) : base(set, name) {
            SplinePosStart = -1;
            SplinePosEnd = -1;
        }

        public void SetEntry(float splinePosition, int kmh) {
            SplinePosStart = splinePosition;
            EntrySpeed = kmh;
        }

        public void SetExit(float splinePosition, int kmh) {
            SplinePosEnd = splinePosition;
            ExitSpeed = kmh;
        }

        public void UpdateModel(TVCameraModel model) {
            if (string.Equals(CameraSetName, model.CameraSetName) && string.Equals(CameraName, model.CameraName)) {
                if (SplinePosStart < 0) SplinePosStart = model.SplinePosStart;
                if (SplinePosEnd < 0) SplinePosEnd = model.SplinePosEnd;
                if (EntrySpeed <= 0) EntrySpeed = model.EntrySpeed;
                if (ExitSpeed <= 0) ExitSpeed = model.ExitSpeed;
                //Debug.WriteLine("updated " + CameraSetName + " " + CameraName);
            }
        }
    }
}
