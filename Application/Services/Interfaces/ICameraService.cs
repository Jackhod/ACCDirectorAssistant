using Application.Services.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Services {

    public delegate void CamsReceivedDelegate();
    public delegate void ActiveCamtypeUpdatedDelegate(CameraModel selectedCam, bool isAutoDirector);
    public delegate void ActiveCamUpdatedDelegate(CameraModel selectedCam, bool isAutoDirector);

    public interface ICameraService : Service {
        public Dictionary<string, List<TVCameraModel>> TVCameraSets { get; }
        public Dictionary<string, List<CameraModel>> CameraSets { get; }
        public Dictionary<CamTypeEnum, DateTime> CamTypeLastActive { get; }
        public float TVCamLearningProgress { get; }
        public DateTime LastCameraSetChange { get; }
        public DateTime LastCameraChange { get; }
        public CameraModel CurrentCam { get; }

        public event CamsReceivedDelegate OnCamsReceived;
        public event ActiveCamtypeUpdatedDelegate OnActiveCamTypeUpdated;
        public event ActiveCamUpdatedDelegate OnActiveCamUpdated;
    }
}
