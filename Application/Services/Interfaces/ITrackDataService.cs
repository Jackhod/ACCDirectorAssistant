using Application.Services.Interfaces;
using Domain.Models;

namespace ACCAssistedDirector.Core.Services {
    public interface ITrackDataService : Service{
        public TrackDataModel TrackDataModel { get; set; }
    }
}
