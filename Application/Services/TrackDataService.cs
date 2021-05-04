using ACCAssistedDirector.Core.MessageHandling;
using Domain.ACCUpdatesStructs;
using Domain.Models;

namespace ACCAssistedDirector.Core.Services {
    public class TrackDataService : UpdateReceiver, ITrackDataService {

        public TrackDataModel TrackDataModel { get; set; }

        public TrackDataService(IClientService clientService) : base(clientService) { }

        protected override void OnTrackDataUpdate(string sender, TrackData trackData) {
            if (TrackDataModel == null) {
                TrackDataModel = new TrackDataModel();
            }
            TrackDataModel.Update(trackData);
        }
    }
}
