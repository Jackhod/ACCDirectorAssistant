using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domain.Models {
    public class CameraSetModel {
        public string CameraSetName { get; }
        public ObservableCollection<CameraModel> Cameras { get; }
        public bool IsActive { get; set; }

        public CameraSetModel(string cameraSetName) {

            CameraSetName = cameraSetName;
        }

        public CameraSetModel(string cameraSetName, List<string> cameraNames) {

            CameraSetName = cameraSetName;

            foreach (string cameraName in cameraNames) {
                Cameras.Add(new CameraModel(CameraSetName, cameraName));
            }
        }

        public void Update(List<string> cameraNames) {

            foreach (var cameraName in cameraNames) {
                CameraModel cam = Cameras.SingleOrDefault(x => x.CameraName == cameraName);
                if (cam == null) {
                    cam = new CameraModel(CameraSetName, cameraName);
                    Cameras.Add(cam);
                }
            }

            // cameras to remove
            var toRemove = new List<CameraModel>();
            foreach (var camVM in Cameras) {
                if (!cameraNames.Contains(camVM.CameraName))
                    toRemove.Add(camVM);
            }

            foreach (var item in toRemove)
                Cameras.Remove(item);
        }
    }
}
