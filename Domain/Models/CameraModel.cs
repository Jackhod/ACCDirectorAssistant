
namespace Domain.Models {
    public enum CamTypeEnum { Tv1, Tv2, RearWing, Onboard, Helicam, Pitlane, Unknown }

    public class CameraModel {
        public string CameraSetName { get; set; }
        public string CameraName { get; set; }
        public CamTypeEnum CamType { get; set; }
        public bool IsActive { get; set; }

        protected CameraModel() { }

        public CameraModel(string cameraSetName, string cameraName) {
            CameraSetName = cameraSetName;
            CameraName = cameraName;
            IsActive = false;
            CamType = EvalCamType();
        }

        private CamTypeEnum EvalCamType() {
            if (CameraSetName.Contains("set1"))
                return CamTypeEnum.Tv1;
            if (CameraSetName.Contains("set2"))
                return CamTypeEnum.Tv2;
            if (CameraSetName.Contains("heli") || CameraSetName.Contains("Heli"))
                return CamTypeEnum.Helicam;
            if (CameraSetName == "pitlane")
                return CamTypeEnum.Pitlane;

            // the rest should be some kind of onboard, we only look for the rear wing one precisely
            if (CameraName == "Onboard3")
                return CamTypeEnum.RearWing;

            // aeh nobody wants to see chasecams in br
            if (CameraName.Contains("Chase") || CameraName.Contains("chase"))
                return CamTypeEnum.Unknown;

            return CamTypeEnum.Onboard;
        }
    }
}
