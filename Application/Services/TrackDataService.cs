using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using Domain.Models;

namespace ACCAssistedDirector.Core.Services {
    public class TrackDataService : GameUpdatesReceiver, ITrackDataService {

        public TrackDataModel TrackDataModel { get; set; }

        public TrackDataService(IClientService clientService) : base(clientService) { }

        protected override void OnTrackDataUpdate(string sender, TrackData trackData) {

            System.Diagnostics.Debug.WriteLine("ontrackdataupdate trackservice");

            if (TrackDataModel == null) {
                TrackDataModel = new TrackDataModel();
            }
            TrackDataModel.Update(trackData);
        }

        public void CancelService() {
            UnsubscribeFromGameUpdates();
        }
    }
}
